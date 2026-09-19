using Microsoft.AspNetCore.Components.Forms;

namespace WhatToEatApp.Services.Dish
{
    public class DishDto
    {
        public Guid DishId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? ImgUrl { get; set; }
        public string? RecipeUrl { get; set; }
        public IList<string> Ingredients { get; set; } = new List<string>();
        public int Rating { get; set; }
        public DateTimeOffset When { get; set; }
        public string? ImageId { get; set; }
        public IBrowserFile? Image { get; set; }

        /// <summary>Endast för visning. Det finns medvetet inget skrivbart OwnerId på DTO:n —
        /// ägaren sätts av servern och kan därför inte styras av klienten.</summary>
        public string OwnerAlias { get; set; } = string.Empty;

        /// <summary>Beräknas av servern: får den inloggade användaren ändra den här rätten?</summary>
        public bool CanEdit { get; set; }
    }

    public static class DishDtoExtensions
    {
        public static Entities.Dish MapToNewDish(this DishDto dishDto, Guid ownerId)
        {
            return new Entities.Dish(dishDto.DishId, ownerId, dishDto.Title, dishDto.Notes, dishDto.ImgUrl,
            dishDto.RecipeUrl, dishDto.Ingredients, dishDto.Rating, dishDto.When, dishDto.ImageId);
        }

        public static DishDto MapToDishDto(this Entities.Dish dish)
        {
            return new DishDto
            {
                DishId = dish.Id,
                Title = dish.Title,
                ImageId = dish.ImageId,
                ImgUrl = dish.ImgUrl,
                Ingredients = dish.Ingredients,
                Notes = dish.Notes,
                RecipeUrl = dish.RecipeUrl,
                Rating = dish.Rating,
                When = dish.When
            };
        }
    }
}