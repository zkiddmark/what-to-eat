namespace WhatToEatApp.Enums
{
    public enum Days
    {
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
        Sunday = 7
    }

    public static class DaysExtensions
    {
        /// <summary>
        /// Visningsnamn. Medlemsnamnen måste förbli engelska: ResolveDayOfWeek matchar dem
        /// mot DateTimeOffset.DayOfWeek.ToString(), som alltid ger engelska namn oavsett
        /// kultur. Döps de om misslyckas TryParse tyst och varje dag räknas fel.
        /// </summary>
        public static string ToSwedish(this Days day) => day switch
        {
            Days.Monday => "Måndag",
            Days.Tuesday => "Tisdag",
            Days.Wednesday => "Onsdag",
            Days.Thursday => "Torsdag",
            Days.Friday => "Fredag",
            Days.Saturday => "Lördag",
            Days.Sunday => "Söndag",
            _ => day.ToString(),
        };

        public static DateTimeOffset ResolveDayOfWeek(this Days dayOfWeek)
        {
            var dtNow = DateTimeOffset.UtcNow;
            var parsed = Enum.TryParse<Days>(dtNow.DayOfWeek.ToString(), out Days result);
            var dtDiff = (int)result - (int)dayOfWeek;
            return dtDiff > 0 ? dtNow.AddDays(7 - dtDiff) : dtNow.AddDays(-(dtDiff));
        }
    }
}