using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatToEatApp.Data;
using WhatToEatApp.DataMigration;
using WhatToEatApp.Entities;
using WhatToEatApp.Services.Auth;
using WhatToEatApp.Services.Dish;

if (args.Length > 0 && args[0] == "--migrate-litedb")
{
    return LiteDbToSqliteMigrator.Run(args);
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthorizationCore();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));
builder.Services.AddTransient<IDishService, DishService>();
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        // Lax, inte Strict: annars följer cookien inte med vid redirect tillbaka från
        // inloggningssidan och användaren landar utloggad.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = CookieSecurityStampValidator.ValidateAsync;
    });

var app = builder.Build();

// Schemat måste följa med appversionen: story 006 lägger till Users-tabellen, och den
// befintliga databasen skapades innan den fanns. Detta är schemamigrering, inte
// LiteDB-importen — den är fortsatt ett manuellt, medvetet kommando.
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

await UserSeeder.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
// Kontrollen sitter i pipelinen, inte i vyn: utan detta serveras _Host till vem som helst
// och först Blazor-komponenten avgör vad som visas.
app.MapFallbackToPage("/_Host").RequireAuthorization();

app.Run();

return 0;
