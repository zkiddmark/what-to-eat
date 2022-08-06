using LiteDB;
using WhatToEatApp.Enums;

namespace WhatToEatApp.Entities
{
    public class Dish
    {
        public Dish()
        {
            DishId = new ObjectId();
            Ingredients = new List<string>();
        }
        public Dish(
            ObjectId? dishId,
            string title,
            string notes,
            string? imgUrl,
            string? recipeUrl,
            IList<string> ingredients,
            int rating,
            DateTimeOffset when,
            string? imageId)
        {
            DishId = dishId ?? new ObjectId();
            Title = title;
            Notes = notes;
            ImgUrl = imgUrl;
            RecipeUrl = recipeUrl;
            Ingredients = ingredients;
            Rating = rating;
            When = when;
            ImageId = imageId;
        }

        public ObjectId DishId { get; set; }
        public string Title { get; private set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? ImgUrl { get; set; }
        public string? RecipeUrl { get; set; }
        public IList<string> Ingredients { get; set; }
        public int Rating { get; set; }
        public DateTimeOffset When { get; private set; }
        public string? ImageId { get; set; }

        public void UpdateDish(Dish updatedDish)
        {
            Title = updatedDish.Title;
            Notes = updatedDish.Notes;
            ImgUrl = updatedDish.ImgUrl;
            RecipeUrl = updatedDish.RecipeUrl;
            Ingredients = updatedDish.Ingredients;
            Rating = updatedDish.Rating;
            ImageId = updatedDish.ImageId;
        }
    }
}