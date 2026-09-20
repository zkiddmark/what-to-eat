namespace WhatToEatApp.Entities
{
    /// <summary>
    /// En rätt på en dag för en användare. Det unika indexet på (UserId, Date) är det som
    /// gör "en rätt per dag och användare" sant även vid samtidiga anrop — en kontroll i
    /// koden ensam tappar där.
    /// </summary>
    public class MealPlan
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid DishId { get; set; }

        /// <summary>
        /// DateOnly, inte DateTimeOffset: planering är en dag, inte ett ögonblick. Typen
        /// lagras som TEXT och kan sorteras i databasen, till skillnad från DateTimeOffset
        /// som SQLite inte klarar i ORDER BY.
        /// </summary>
        public DateOnly Date { get; set; }
    }
}
