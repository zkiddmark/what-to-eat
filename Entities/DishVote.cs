namespace WhatToEatApp.Entities
{
    /// <summary>
    /// En röst per användare och rätt. Det unika indexet på (DishId, UserId) är det som
    /// faktiskt garanterar det — en kontroll i koden ensam tappar vid samtidiga anrop.
    /// </summary>
    public class DishVote
    {
        public Guid Id { get; set; }
        public Guid DishId { get; set; }
        public Guid UserId { get; set; }
        public int Score { get; set; }
    }
}
