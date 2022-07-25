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
            string title,
            string notes,
            string? imgUrl,
            string? recipeUrl,
            IList<string> ingredients,
            Days when)
        {
            DishId = new ObjectId();
            Title = title;
            Notes = notes;
            ImgUrl = imgUrl;
            RecipeUrl = recipeUrl;
            Ingredients = ingredients;
            Rating = 0;
            When = Dish.ResolveDayOfWeek(when);
        }

        public ObjectId DishId { get; set; }
        public string Title { get; private set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? ImgUrl { get; set; }
        public string? RecipeUrl { get; set; }
        public IList<string> Ingredients { get; set; }
        public int Rating { get; set; }
        public DateTimeOffset When { get; private set; }

        public void UpdateDish(string title, string notes, string? imgUrl, string? recipeUrl, IList<string> ingredients)
        {
            Title = title;
            Notes = notes;
            ImgUrl = imgUrl;
            RecipeUrl = recipeUrl;
            Ingredients = ingredients;
        }

        public static DateTimeOffset ResolveDayOfWeek(Days dayOfWeek)
        {
            var dtNow = DateTimeOffset.UtcNow;
            var parsed = Enum.TryParse<Days>(dtNow.DayOfWeek.ToString(), out Days result);
            var dtDiff = (int)result - (int)dayOfWeek;
            return dtDiff > 0 ? dtNow.AddDays(7 - dtDiff) : dtNow.AddDays(-(dtDiff));
        }
    }
}