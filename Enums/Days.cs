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
        public static DateTimeOffset ResolveDayOfWeek(this Days dayOfWeek)
        {
            var dtNow = DateTimeOffset.UtcNow;
            var parsed = Enum.TryParse<Days>(dtNow.DayOfWeek.ToString(), out Days result);
            var dtDiff = (int)result - (int)dayOfWeek;
            return dtDiff > 0 ? dtNow.AddDays(7 - dtDiff) : dtNow.AddDays(-(dtDiff));
        }
    }
}