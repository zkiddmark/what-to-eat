using WhatToEatApp.Entities;
using WhatToEatApp.Enums;

namespace WhatToEatApp.Services
{
    public interface IDishService
    {
        Task AddDish(Dish dish);
        Task UpdateDish(Dish dish);
        Task DeleteDish(Dish dish);
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

        public async Task DeleteDish(Dish dish)
        {
            using var instance = _liteDbService.CreateInstance();
            var col = instance.GetCollection<Dish>("dishes");
            col.Delete(dish.DishId);

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
}