namespace WhatToEatApp.Entities
{
    public class DishImage
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
