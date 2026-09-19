using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatToEatApp.Entities;
using WhatToEatApp.Enums;
using WhatToEatApp.Services.Auth;

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
                    "Sätt variabeln ({Min}-{Max} tecken) i docker-compose-wte.yml och starta om containern " +
                    "för att kunna logga in.",
                    PasswordSetting, UserService.MinimumPasswordLength, UserService.MaximumPasswordLength);
                return;
            }

            if (password.Length < UserService.MinimumPasswordLength
                || password.Length > UserService.MaximumPasswordLength)
            {
                logger.LogWarning(
                    "{Setting} bryter mot appens egen lösenordspolicy ({Min}-{Max} tecken) — " +
                    "inget administratörskonto skapades. Välj ett lösenord inom längdgränserna i " +
                    "docker-compose-wte.yml och starta om containern.",
                    PasswordSetting, UserService.MinimumPasswordLength, UserService.MaximumPasswordLength);
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

            // Raden skrivs med rå SQL och en uttrycklig kolumnlista, inte via modellen.
            // Seedningen kör mitt i migreringskedjan — direkt efter AddAppUser och före
            // ägarmigreringen — och tabellen saknar då de kolumner senare migreringar lägger
            // till. En EF-insert skulle nämna dem och falla. Listan nedan är AddAppUser:s
            // egna kolumner; senare kolumner är nullbara och lämnas åt sitt förval.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO Users (Id, Alias, Email, PasswordHash, Status, Role, SecurityStamp, CreatedAt, FailedAttempts, LockedUntil)
                VALUES ({admin.Id}, {admin.Alias}, {admin.Email}, {admin.PasswordHash}, {(int)admin.Status}, {admin.Role}, {admin.SecurityStamp}, {admin.CreatedAt}, {admin.FailedAttempts}, NULL)
                """);
            logger.LogInformation("Administratörskonto {Email} skapat.", AdminEmail);
        }
    }
}
