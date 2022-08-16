using LiteDB;
using Microsoft.AspNetCore.Components.Forms;

namespace WhatToEatApp.Services.Dish
{
    public class DishDto
    {
        public ObjectId? DishId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? ImgUrl { get; set; }
        public string? RecipeUrl { get; set; }
        public IList<string> Ingredients { get; set; } = new List<string>();
        public int Rating { get; set; }
        public DateTimeOffset When { get; set; }
        public string? ImageId { get; set; }
        public IBrowserFile? Image { get; set; }
    }

    public static class DishDtoExtensions
    {
        public static Entities.Dish MapToNewDish(this DishDto dishDto)
        {
            return new Entities.Dish(dishDto.DishId, dishDto.Title, dishDto.Notes, dishDto.ImgUrl,
            dishDto.RecipeUrl, dishDto.Ingredients, dishDto.Rating, dishDto.When, dishDto.ImageId);
        }

        public static DishDto MapToDishDto(this Entities.Dish dish)
        {
            return new DishDto
            {
                DishId = dish.DishId,
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