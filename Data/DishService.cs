using LiteDB;

namespace WhatToEatApp.Data
{
    public interface IDishService
    {
        Task AddDish(Dish dish);
        Task UpdateDish(Dish dish);
        Task<Dish?> GetTodaysDish(Days day);
    }

    public class DishService : IDishService
    {
        // private readonly List<Dish> _dishes = new List<Dish>();
        private readonly ILiteDbService _liteDbService;

        public DishService(ILiteDbService liteDbService)
        {
            _liteDbService = liteDbService;
        }

        public async Task AddDish(Dish dish)
        {
            using var instance = _liteDbService.CreateInstance();
            var col = instance.GetCollection<Dish>("dishes");
            col.Insert(dish);

            await Task.CompletedTask;
        }

        public async Task UpdateDish(Dish dish)
        {
            using var instance = _liteDbService.CreateInstance();
            var col = instance.GetCollection<Dish>("dishes");
            col.Update(dish);

            await Task.CompletedTask;
        }

        public async Task<Dish?> GetTodaysDish(Days day)
        {
            using var instance = _liteDbService.CreateInstance();
            var col = instance.GetCollection<Dish>("dishes");
            var dish = col.FindAll().LastOrDefault(x => x.When.Date == Dish.ResolveDayOfWeek(day).Date);
            // var dish = _dishes.LastOrDefault(x => x.When.Date == Dish.ResolveDayOfWeek(day).Date);
            return await Task.FromResult<Dish?>(dish);
        }

    }

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

    public enum Days
    {
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
        Sunday = 7
    }
}