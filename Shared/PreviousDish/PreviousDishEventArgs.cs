namespace WhatToEatApp.Shared.PreviousDish
{
    public class PreviousDishEventArgs : EventArgs
    {
        public DateTimeOffset DtWhen { get; set; }
        public Guid DishId { get; set; }
    }
}