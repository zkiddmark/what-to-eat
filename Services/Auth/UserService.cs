using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatToEatApp.Data;
using WhatToEatApp.Entities;
using WhatToEatApp.Enums;

namespace WhatToEatApp.Services.Auth
{
    public enum LoginResult
    {
        Success,
        InvalidCredentials,
        PendingApproval,
        Rejected,
    }

    public enum RegisterResult
    {
        Success,
        EmailAlreadyRegistered,
        PasswordTooShort,
    }

    public interface IUserService
    {
        Task<RegisterResult> RegisterAsync(string alias, string email, string password);
        Task<(LoginResult Result, AppUser? User)> ValidateCredentialsAsync(string email, string password);
        Task<AppUser?> FindBySecurityStampAsync(Guid userId, Guid securityStamp);
        Task RotateSecurityStampAsync(Guid userId);
    }

    public class UserService : IUserService
    {
        /// <summary>Minsta lösenordslängd. Ingen övre gräns under 256, inga teckenklasskrav.</summary>
        public const int MinimumPasswordLength = 12;
        public const int MaximumPasswordLength = 256;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IPasswordHasher<AppUser> _passwordHasher;

        /// <summary>
        /// Hash att verifiera mot när e-posten inte finns, så att svarstiden blir densamma
        /// som för ett existerande konto. Utan den avslöjar inloggningssidan vilka adresser
        /// som är registrerade.
        /// </summary>
        private readonly string _dummyHash;

        public UserService(IDbContextFactory<AppDbContext> dbContextFactory, IPasswordHasher<AppUser> passwordHasher)
        {
            _dbContextFactory = dbContextFactory;
            _passwordHasher = passwordHasher;
            _dummyHash = _passwordHasher.HashPassword(new AppUser(), "no user with this address exists");
        }

        public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        public async Task<RegisterResult> RegisterAsync(string alias, string email, string password)
        {
            if (password.Length < MinimumPasswordLength)
            {
                return RegisterResult.PasswordTooShort;
            }

            using var db = _dbContextFactory.CreateDbContext();
            var normalized = NormalizeEmail(email);
            if (await db.Users.AnyAsync(x => x.Email == normalized))
            {
                return RegisterResult.EmailAlreadyRegistered;
            }

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                Alias = alias.Trim(),
                Email = normalized,
                Status = AccountStatus.Pending,
                Role = "user",
                SecurityStamp = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            db.Users.Add(user);
            await db.SaveChangesAsync();
            return RegisterResult.Success;
        }

        public async Task<(LoginResult Result, AppUser? User)> ValidateCredentialsAsync(string email, string password)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var normalized = NormalizeEmail(email);
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == normalized);

            if (user is null)
            {
                // Verifiera ändå, så att svaret tar ungefär lika lång tid som för ett konto
                // som finns. Resultatet slängs.
                _passwordHasher.VerifyHashedPassword(new AppUser(), _dummyHash, password);
                return (LoginResult.InvalidCredentials, null);
            }

            // En spärrad inloggning ger samma generiska svar som fel lösenord.
            if (user.LockedUntil is not null && user.LockedUntil > DateTimeOffset.UtcNow)
            {
                return (LoginResult.InvalidCredentials, null);
            }

            var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                user.FailedAttempts++;
                if (user.FailedAttempts >= MaxFailedAttempts)
                {
                    user.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
                    user.FailedAttempts = 0;
                }
                await db.SaveChangesAsync();
                return (LoginResult.InvalidCredentials, null);
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
            }

            user.FailedAttempts = 0;
            user.LockedUntil = null;
            await db.SaveChangesAsync();

            // Statusen kontrolleras efter lösenordet. Annars blir väntlägesbeskedet i sig
            // ett sätt att kartlägga vilka konton som finns.
            return user.Status switch
            {
                AccountStatus.Approved => (LoginResult.Success, user),
                AccountStatus.Rejected => (LoginResult.Rejected, null),
                _ => (LoginResult.PendingApproval, null),
            };
        }

        public async Task<AppUser?> FindBySecurityStampAsync(Guid userId, Guid securityStamp)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return await db.Users.FirstOrDefaultAsync(x =>
                x.Id == userId && x.SecurityStamp == securityStamp && x.Status == AccountStatus.Approved);
        }

        public async Task RotateSecurityStampAsync(Guid userId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                return;
            }
            user.SecurityStamp = Guid.NewGuid();
            await db.SaveChangesAsync();
        }
    }
}
