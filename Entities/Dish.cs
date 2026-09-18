namespace WhatToEatApp.Entities
{
    public class Dish
    {
        public Dish()
        {
            Id = Guid.NewGuid();
            Ingredients = new List<string>();
        }
        public Dish(
            Guid id,
            string title,
            string notes,
            string? imgUrl,
            string? recipeUrl,
            IList<string> ingredients,
            int rating,
            DateTimeOffset when,
            string? imageId)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            Title = title;
            Notes = notes;
            ImgUrl = imgUrl;
            RecipeUrl = recipeUrl;
            Ingredients = ingredients;
            Rating = rating;
            When = when;
            ImageId = imageId;
        }

        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? ImgUrl { get; set; }
        public string? RecipeUrl { get; set; }
        public IList<string> Ingredients { get; set; }
        public int Rating { get; set; }
        public DateTimeOffset When { get; set; }
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
            When = updatedDish.When;
        }
    }
}
