using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatToEatApp.Entities;
using WhatToEatApp.Enums;

namespace WhatToEatApp.Data
{
    /// <summary>
    /// Skapar det första administratörskontot så appen aldrig är utelåst. Lösenordet läses ur
    /// konfigurationen — saknas det seedas ingenting och appen startar ändå, hellre det än ett
    /// standardlösenord i koden.
    /// </summary>
    public static class UserSeeder
    {
        public const string AdminEmail = "peter@stjern.se";
        public const string PasswordSetting = "ADMIN_INITIAL_PASSWORD";

        public static async Task SeedAsync(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(UserSeeder));
            var factory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();

            using var db = await factory.CreateDbContextAsync();
            if (await db.Users.AnyAsync())
            {
                return;
            }

            var password = configuration[PasswordSetting];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "Ingen användare finns och {Setting} är inte satt — inget administratörskonto skapades. " +
                    "Sätt variabeln och starta om för att kunna logga in.", PasswordSetting);
                return;
            }

            var hasher = services.GetRequiredService<IPasswordHasher<AppUser>>();
            var admin = new AppUser
            {
                Id = Guid.NewGuid(),
                Alias = "Peter",
                Email = AdminEmail,
                Status = AccountStatus.Approved,
                Role = "admin",
                SecurityStamp = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            admin.PasswordHash = hasher.HashPassword(admin, password);

            db.Users.Add(admin);
            await db.SaveChangesAsync();
            logger.LogInformation("Administratörskonto {Email} skapat.", AdminEmail);
        }
    }
}
