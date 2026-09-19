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

const string ForcedPasswordChangePath = "/byt-losenord";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthorizationCore();
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));
// Scoped: tjänsten bär nu ett användarberoende och ska leva lika länge som kretsen.
builder.Services.AddScoped<IDishService, DishService>();
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
// Rätterna räknas med rå SQL: OwnerId-kolumnen finns inte förrän AddDishOwner körts, så en
// EF-fråga mot Dishes skulle falla på en kolumn som ännu inte är till.
static async Task<long> CountDishesAsync(AppDbContext db)
{
    await db.Database.OpenConnectionAsync();
    try
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Dishes";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
    finally
    {
        await db.Database.CloseConnectionAsync();
    }
}

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = await factory.CreateDbContextAsync();

    // Ägarskapsmigreringen i story 008 pekar rätternas OwnerId mot admin-kontot, som i sin
    // tur behöver användartabellen. Körs allt i ett svep på en databas med befintliga rätter
    // finns ingen admin att peka på och främmande nyckeln fäller migreringen. Därför:
    // migrera fram till användartabellen, seeda admin, och kör sedan resten.
    //
    // Den delade migreringen görs bara när den behövs. Seedningen skriver med den aktuella
    // modellen, och halvvägs genom kedjan saknar tabellen de kolumner senare migreringar
    // lägger till — en tom databas ska därför migreras hela vägen först och seedas sedan.
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();

    // Dishes finns först efter Initial; dessförinnan finns inga rätter att äga.
    var dishCount = applied.Any(m => m.EndsWith("Initial", StringComparison.Ordinal))
        ? await CountDishesAsync(db)
        : 0;

    if (pending.Any(m => m.EndsWith("AddDishOwner", StringComparison.Ordinal)) && dishCount > 0)
    {
        var usersMigration = pending.FirstOrDefault(m => m.EndsWith("AddAppUser", StringComparison.Ordinal));
        if (usersMigration is not null)
        {
            await db.Database.MigrateAsync(usersMigration);
        }

        // Seedningen hänger på att användartabellen finns — inte på att migreringen råkade
        // vara pending just den här starten. Skedde första starten utan
        // ADMIN_INITIAL_PASSWORD ligger AddAppUser redan applicerad men tabellen är tom.
        await UserSeeder.SeedAsync(app.Services);

        // Grind: utan admin fäller ägarmigreringen främmande nyckeln på befintliga rätter.
        // Stanna på en läsbar rad i stället för en SQLite-krasch ur EF — databasen är orörd.
        if (!await db.Users.AnyAsync())
        {
            app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup").LogError(
                "Inget administratörskonto finns och det behövs innan rätternas ägare kan sättas. " +
                "Sätt {Setting} ({Min}-{Max} tecken) i docker-compose-wte.yml och starta om containern. " +
                "Databasen är orörd.",
                UserSeeder.PasswordSetting,
                UserService.MinimumPasswordLength,
                UserService.MaximumPasswordLength);
            return 1;
        }
    }

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

// Ett konto på tillfälligt lösenord får inte nå något annat än sitt eget byte. Kontrollen
// ligger här, inte i vyerna: /_blazor ingår i det som spärras, så ingen krets kan startas
// och inget dataändrande anrop nå fram.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isExempt = path.StartsWithSegments(ForcedPasswordChangePath)
        || path.StartsWithSegments("/logout");

    if (!isExempt
        && context.User.Identity?.IsAuthenticated == true
        && AuthClaims.RequiresPasswordChange(context.User))
    {
        context.Response.Redirect(ForcedPasswordChangePath);
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
// Kontrollen sitter i pipelinen, inte i vyn: utan detta serveras _Host till vem som helst
// och först Blazor-komponenten avgör vad som visas.
app.MapFallbackToPage("/_Host").RequireAuthorization();

app.Run();

return 0;
