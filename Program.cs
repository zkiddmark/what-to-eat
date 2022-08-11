using WhatToEatApp.Services.Persistance;
using WhatToEatApp.Services.Dish;
using WhatToEatApp.Auth;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("./ExcludedSecrets/firebaseConfig.json");

// Add services to the container.
builder.Services.Configure<FirebaseOptions>(
    builder.Configuration.GetSection("firebaseOptions")
);
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<ILiteDbService, LiteDbService>();
builder.Services.AddTransient<IDishService, DishService>();
builder.Services.AddScoped<IFirebaseService, FirebaseService>();
builder.Services.AddScoped<FirebaseAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>((provider) => provider.GetRequiredService<FirebaseAuthStateProvider>());

var app = builder.Build();


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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
