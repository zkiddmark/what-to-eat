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
            Guid ownerId,
            string title,
            string notes,
            string? imgUrl,
            string? recipeUrl,
            IList<string> ingredients,
            string? imageId)
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            OwnerId = ownerId;
            Title = title;
            Notes = notes;
            ImgUrl = imgUrl;
            RecipeUrl = recipeUrl;
            Ingredients = ingredients;
            ImageId = imageId;
        }

        public Guid Id { get; set; }

        /// <summary>Sätts av servern utifrån inloggad användare, aldrig av något klienten skickar.</summary>
        public Guid OwnerId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? ImgUrl { get; set; }
        public string? RecipeUrl { get; set; }
        public IList<string> Ingredients { get; set; }
        public string? ImageId { get; set; }

        /// <summary>Ägaren byts aldrig via en uppdatering och kopieras därför inte här.</summary>
        public void UpdateDish(Dish updatedDish)
        {
            Title = updatedDish.Title;
            Notes = updatedDish.Notes;
            ImgUrl = updatedDish.ImgUrl;
            RecipeUrl = updatedDish.RecipeUrl;
            Ingredients = updatedDish.Ingredients;
            ImageId = updatedDish.ImageId;
        }
    }
}
