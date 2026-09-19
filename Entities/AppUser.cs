using WhatToEatApp.Enums;

namespace WhatToEatApp.Entities
{
    public class AppUser
    {
        public Guid Id { get; set; }
        public string Alias { get; set; } = string.Empty;

        /// <summary>Inloggningsidentitet. Lagras normaliserad till gemener och har unikt index.</summary>
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public AccountStatus Status { get; set; } = AccountStatus.Pending;

        /// <summary>Sätts i story 007. Kolumnen finns redan för att slippa en extra migration.</summary>
        public string Role { get; set; } = "user";

        /// <summary>Roteras vid utloggning och lösenordsbyte så gamla cookies slutar gälla.</summary>
        public Guid SecurityStamp { get; set; } = Guid.NewGuid();
        public DateTimeOffset CreatedAt { get; set; }
        /// <summary>
        /// Satt = lösenordet är tillfälligt och måste bytas. Värdet är när det slutar gälla.
        /// Ett nullbart datum bär båda kraven; en separat flagga vore en andra sanningskälla.
        /// </summary>
        public DateTimeOffset? TemporaryPasswordExpiresAt { get; set; }

        public int FailedAttempts { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
    }
}
