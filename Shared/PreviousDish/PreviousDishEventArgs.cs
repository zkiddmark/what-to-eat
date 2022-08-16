using LiteDB;

namespace WhatToEatApp.Shared.PreviousDish
{
    public class PreviousDishEventArgs : EventArgs
    {
        public DateTimeOffset DtWhen { get; set; }
        public ObjectId DishId { get; set; } = null!;
    }
}