using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.Authorization;
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

        /// <summary>Rätt lösenord, men det är tillfälligt: logga in och tvinga fram ett byte.</summary>
        MustChangePassword,

        /// <summary>Det tillfälliga lösenordet har passerat sin utgångstid.</summary>
        TemporaryPasswordExpired,
    }

    public enum ForcedChangeResult
    {
        Success,
        NoTemporaryPassword,
        Expired,
        PasswordLengthInvalid,
        UserNotFound,
    }

    public enum RegisterResult
    {
        Success,
        EmailAlreadyRegistered,
        PasswordLengthInvalid,
    }

    public enum ChangePasswordResult
    {
        Success,
        InvalidCurrentPassword,
        PasswordLengthInvalid,
        UserNotFound,
    }

    public enum AdminActionResult
    {
        Success,
        NotAuthorized,
        UserNotFound,
        CannotActOnSelf,
        CannotRemoveLastAdmin,
    }

    public interface IUserService
    {
        Task<RegisterResult> RegisterAsync(string alias, string email, string password);
        Task<IReadOnlyList<AppUser>> GetPendingAsync();
        Task<IReadOnlyList<AppUser>> GetUsersAsync();
        Task<AdminActionResult> ApproveAsync(Guid userId);
        Task<AdminActionResult> RejectAsync(Guid userId);
        Task<(LoginResult Result, AppUser? User)> ValidateCredentialsAsync(string email, string password);
        Task<AppUser?> FindBySecurityStampAsync(Guid userId, Guid securityStamp);
        Task<AppUser?> GetByIdAsync(Guid userId);
        Task RotateSecurityStampAsync(Guid userId);
        Task<ChangePasswordResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        Task<(AdminActionResult Result, string? Password)> SetTemporaryPasswordAsync(Guid userId);
        Task<ForcedChangeResult> CompleteForcedChangeAsync(Guid userId, string newPassword);
    }

    public class UserService : IUserService
    {
        /// <summary>Minsta lösenordslängd. Ingen övre gräns under 256, inga teckenklasskrav.</summary>
        public const int MinimumPasswordLength = 12;
        public const int MaximumPasswordLength = 256;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public const string AdminRole = "admin";
        public const string UserRole = "user";

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        /// <summary>
        /// Hash att verifiera mot när e-posten inte finns, så att svarstiden blir densamma
        /// som för ett existerande konto. Utan den avslöjar inloggningssidan vilka adresser
        /// som är registrerade.
        /// </summary>
        private readonly string _dummyHash;

        public UserService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IPasswordHasher<AppUser> passwordHasher,
            AuthenticationStateProvider authenticationStateProvider)
        {
            _dbContextFactory = dbContextFactory;
            _passwordHasher = passwordHasher;
            _authenticationStateProvider = authenticationStateProvider;
            _dummyHash = _passwordHasher.HashPassword(new AppUser(), "no user with this address exists");
        }

        public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        public async Task<RegisterResult> RegisterAsync(string alias, string email, string password)
        {
            if (password.Length < MinimumPasswordLength || password.Length > MaximumPasswordLength)
            {
                return RegisterResult.PasswordLengthInvalid;
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
                Role = UserRole,
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

            // En spärrad inloggning ger samma generiska svar som fel lösenord — och måste ta
            // lika lång tid. Utan verifieringen nedan svarar ett spärrat konto dubbelt så
            // snabbt, vilket i sig avslöjar att adressen finns.
            if (user.LockedUntil is not null && user.LockedUntil > DateTimeOffset.UtcNow)
            {
                _passwordHasher.VerifyHashedPassword(new AppUser(), _dummyHash, password);
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
            if (user.Status != AccountStatus.Approved)
            {
                return user.Status == AccountStatus.Rejected
                    ? (LoginResult.Rejected, null)
                    : (LoginResult.PendingApproval, null);
            }

            // Ett tillfälligt lösenord prövas mot sin utgångstid när det används — inget
            // bakgrundsjobb städar. Går det ut förblir kontot spärrat tills admin sätter ett nytt.
            if (user.TemporaryPasswordExpiresAt is { } expiresAt)
            {
                return expiresAt <= DateTimeOffset.UtcNow
                    ? (LoginResult.TemporaryPasswordExpired, null)
                    // Användaren loggas in — annars går hen inte att identifiera på bytessidan.
                    : (LoginResult.MustChangePassword, user);
            }

            return (LoginResult.Success, user);
        }

        /// <summary>
        /// Den inloggade användaren hämtas alltid härifrån, aldrig ur en parameter. Rollen
        /// skickas därmed aldrig med från anroparen och kan inte hittas på.
        /// </summary>
        private async Task<AppUser?> GetCurrentAdminAsync()
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var userId = AuthClaims.GetUserId(state.User);
            if (userId is null)
            {
                return null;
            }

            using var db = _dbContextFactory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId.Value);
            if (user is null || user.Status != AccountStatus.Approved || user.Role != AdminRole)
            {
                return null;
            }
            return user;
        }

        public async Task<IReadOnlyList<AppUser>> GetPendingAsync()
        {
            if (await GetCurrentAdminAsync() is null)
            {
                return Array.Empty<AppUser>();
            }

            // SQLite kan inte sortera på DateTimeOffset i ORDER BY — samma begränsning som
            // GetAllDishes stötte på i story 004. Sorteringen görs därför i minnet.
            using var db = _dbContextFactory.CreateDbContext();
            var pending = await db.Users
                .Where(x => x.Status == AccountStatus.Pending)
                .ToListAsync();
            return pending.OrderBy(x => x.CreatedAt).ToList();
        }

        public async Task<IReadOnlyList<AppUser>> GetUsersAsync()
        {
            if (await GetCurrentAdminAsync() is null)
            {
                return Array.Empty<AppUser>();
            }

            using var db = _dbContextFactory.CreateDbContext();
            return await db.Users
                .Where(x => x.Status == AccountStatus.Approved)
                .OrderBy(x => x.Alias)
                .ToListAsync();
        }

        public async Task<AdminActionResult> ApproveAsync(Guid userId)
            => await SetStatusAsync(userId, AccountStatus.Approved);

        public async Task<AdminActionResult> RejectAsync(Guid userId)
            => await SetStatusAsync(userId, AccountStatus.Rejected);

        private async Task<AdminActionResult> SetStatusAsync(Guid userId, AccountStatus status)
        {
            var admin = await GetCurrentAdminAsync();
            if (admin is null)
            {
                return AdminActionResult.NotAuthorized;
            }

            using var db = _dbContextFactory.CreateDbContext();
            var target = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (target is null)
            {
                return AdminActionResult.UserNotFound;
            }

            if (status == AccountStatus.Rejected)
            {
                // Ingen ska kunna låsa ut sig själv, och sista adminen måste bli kvar.
                if (target.Id == admin.Id)
                {
                    return AdminActionResult.CannotActOnSelf;
                }

                var remainingAdmins = await db.Users.CountAsync(x =>
                    x.Role == AdminRole && x.Status == AccountStatus.Approved && x.Id != target.Id);
                if (target.Role == AdminRole && remainingAdmins == 0)
                {
                    return AdminActionResult.CannotRemoveLastAdmin;
                }

                // Ett avslag ska slå igenom direkt, även om kontot har en giltig cookie.
                target.SecurityStamp = Guid.NewGuid();
            }

            target.Status = status;
            await db.SaveChangesAsync();
            return AdminActionResult.Success;
        }

        public async Task<AppUser?> FindBySecurityStampAsync(Guid userId, Guid securityStamp)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return await db.Users.FirstOrDefaultAsync(x =>
                x.Id == userId && x.SecurityStamp == securityStamp && x.Status == AccountStatus.Approved);
        }

        /// <summary>
        /// Byter lösenord på en användare som redan är inloggad. Id:t kommer från cookien,
        /// aldrig från formuläret. Det nuvarande lösenordet verifieras med
        /// <see cref="ValidateCredentialsAsync"/> så att inloggningens spärrlogik gäller även
        /// här — annars vore kontosidan en väg runt låset.
        /// </summary>
        public async Task<ChangePasswordResult> ChangePasswordAsync(
            Guid userId, string currentPassword, string newPassword)
        {
            if (newPassword.Length < MinimumPasswordLength || newPassword.Length > MaximumPasswordLength)
            {
                return ChangePasswordResult.PasswordLengthInvalid;
            }

            using var db = _dbContextFactory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                return ChangePasswordResult.UserNotFound;
            }

            // Spärrat konto, fel lösenord eller ett konto som inte längre är godkänt ger
            // samma svar: att det var fel. Inget annat avslöjas.
            var (result, validated) = await ValidateCredentialsAsync(user.Email, currentPassword);
            if (result != LoginResult.Success || validated is null || validated.Id != userId)
            {
                return ChangePasswordResult.InvalidCurrentPassword;
            }

            // Hash och stämpel skrivs i samma SaveChanges: ett halvt byte får inte bli kvar.
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.SecurityStamp = Guid.NewGuid();
            await db.SaveChangesAsync();
            return ChangePasswordResult.Success;
        }

        /// <summary>
        /// Tecken som inte går att förväxla när lösenordet läses upp i telefon: inga 0/O,
        /// 1/l/I. Grupperingen i fyror är av samma skäl.
        /// </summary>
        private const string TemporaryPasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        private const int TemporaryPasswordGroups = 4;
        private const int TemporaryPasswordGroupLength = 4;
        private static readonly TimeSpan TemporaryPasswordLifetime = TimeSpan.FromHours(24);

        private static string GenerateTemporaryPassword()
        {
            var groups = Enumerable
                .Range(0, TemporaryPasswordGroups)
                .Select(_ => RandomNumberGenerator.GetString(
                    TemporaryPasswordAlphabet, TemporaryPasswordGroupLength));
            return string.Join('-', groups);
        }

        /// <summary>
        /// Sätter ett tillfälligt lösenord på en annan användare och returnerar klartexten.
        /// Den skrivs aldrig till databas eller logg — anroparen visar den en gång och
        /// slänger den.
        /// </summary>
        public async Task<(AdminActionResult Result, string? Password)> SetTemporaryPasswordAsync(Guid userId)
        {
            var admin = await GetCurrentAdminAsync();
            if (admin is null)
            {
                return (AdminActionResult.NotAuthorized, null);
            }

            if (userId == admin.Id)
            {
                // Admin byter sitt eget lösenord via /konto.
                return (AdminActionResult.CannotActOnSelf, null);
            }

            using var db = _dbContextFactory.CreateDbContext();
            var target = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (target is null || target.Status != AccountStatus.Approved)
            {
                return (AdminActionResult.UserNotFound, null);
            }

            var password = GenerateTemporaryPassword();
            target.PasswordHash = _passwordHasher.HashPassword(target, password);
            target.TemporaryPasswordExpiresAt = DateTimeOffset.UtcNow.Add(TemporaryPasswordLifetime);
            // Ny stämpel: användarens pågående sessioner slutar gälla omedelbart. Räknaren
            // nollas — annars kunde ett gammalt lås hindra hen från att använda det nya.
            target.SecurityStamp = Guid.NewGuid();
            target.FailedAttempts = 0;
            target.LockedUntil = null;
            await db.SaveChangesAsync();

            return (AdminActionResult.Success, password);
        }

        /// <summary>
        /// Avslutar det tvingade bytet. Tillståndet rensas i samma SaveChanges som det nya
        /// lösenordet sparas — ett halvt byte får inte lämna kontot utan väg in.
        /// </summary>
        public async Task<ForcedChangeResult> CompleteForcedChangeAsync(Guid userId, string newPassword)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                return ForcedChangeResult.UserNotFound;
            }

            if (user.TemporaryPasswordExpiresAt is not { } expiresAt)
            {
                return ForcedChangeResult.NoTemporaryPassword;
            }

            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                return ForcedChangeResult.Expired;
            }

            if (newPassword.Length < MinimumPasswordLength || newPassword.Length > MaximumPasswordLength)
            {
                return ForcedChangeResult.PasswordLengthInvalid;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            user.TemporaryPasswordExpiresAt = null;
            user.SecurityStamp = Guid.NewGuid();
            await db.SaveChangesAsync();
            return ForcedChangeResult.Success;
        }

        public async Task<AppUser?> GetByIdAsync(Guid userId)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
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
