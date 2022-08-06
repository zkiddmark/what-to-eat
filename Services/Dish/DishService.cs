using Microsoft.AspNetCore.Components.Forms;
using WhatToEatApp.Enums;
using WhatToEatApp.Services.Persistance;

namespace WhatToEatApp.Services.Dish
{
    public interface IDishService
    {
        Task AddDishAsync(DishDto dish);
        Task UpdateDishAsync(DishDto dish);
        Task DeleteDishAsync(DishDto dish);
        Task<DishDto?> GetTodaysDish(Days day);
        Task<string> GetImageFromDbAsync(string imgId);
    }

    public class DishService : IDishService
    {
        // private readonly List<Dish> _dishes = new List<Dish>();
        private readonly ILiteDbService _liteDbService;

        public DishService(ILiteDbService liteDbService)
        {
            _liteDbService = liteDbService;
        }

        public async Task AddDishAsync(DishDto dishDto)
        {
            var newDish = dishDto.MapToDish();
            if (dishDto.Image is not null)
            {
                var imageId = await AddImageFromFileAsync(dishDto.Image, dishDto.Title);
                newDish.ImageId = imageId;
            }

            using var instance = _liteDbService.CreateInstance();
            var col = instance.GetCollection<Entities.Dish>("dishes");
            var result = col.Insert(dishDto.MapToDish());
            await Task.CompletedTask;
        }

        public async Task UpdateDishAsync(DishDto dishDto)
        {
            using var instance = _liteDbService.CreateInstance();
            var col = instance.GetCollection<Entities.Dish>("dishes");
            // Get dish from db.
            var dishToUpdate = col.FindById(dishDto.DishId);

            // Image has changed, remove the old one.
            if (dishToUpdate.ImageId is not null && dishToUpdate.ImageId != dishDto.ImageId)
            {
                await DeleteImage(dishToUpdate.ImageId);
            }

            dishToUpdate.UpdateDish(dishDto.MapToDish());

            if (dishDto.Image is not null)
            {
                var imageId = await AddImageFromFileAsync(dishDto.Image, dishDto.Title);
                dishToUpdate.ImageId = imageId;
            }

            col.Update(dishToUpdate);
            await Task.CompletedTask;
        }

        public async Task DeleteDishAsync(DishDto dishDto)
        {
            using var instance = _liteDbService.CreateInstance();
            var col = instance.GetCollection<Entities.Dish>("dishes");
            col.Delete(dishDto.DishId);

            // If the dish had an associated image, delete it.
            if (dishDto.ImageId is not null)
            {
                await DeleteImage(dishDto.ImageId);
            }

            await Task.CompletedTask;
        }

        public async Task<DishDto?> GetTodaysDish(Days day)
        {
            using var instance = _liteDbService.CreateInstance();
            var col = instance.GetCollection<Entities.Dish>("dishes");
            var dish = col.FindAll().LastOrDefault(x => x.When.Date == day.ResolveDayOfWeek().Date);
            var dishDto = dish?.MapToDishDto();
            if (dishDto is null)
            {
                return null;
            }
            dishDto.When = day.ResolveDayOfWeek();
            return await Task.FromResult<DishDto?>(dishDto);
        }
        public async Task<string> GetImageFromDbAsync(string imgId)
        {
            if (imgId is null)
            {
                throw new InvalidOperationException("imgId must not be null!");
            }
            using var instance = _liteDbService.CreateInstance();
            using var ms = new MemoryStream();

            var fs = instance.GetStorage<string>("wteImages", "wteChunks");
            if (!fs.Exists(imgId))
            {
                return string.Empty;
            }

            var str = fs.OpenRead(imgId);

            await str.CopyToAsync(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        private async Task<string> AddImageFromFileAsync(IBrowserFile file, string title)
        {
            using var instance = _liteDbService.CreateInstance();
            var fs = instance.GetStorage<string>("wteImages", "wteChunks");
            var ms = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(ms);
            ms.Position = 0;
            var newId = Guid.NewGuid().ToString();
            fs.Upload(newId, CreateSafeImageTitle(title), ms);
            return newId;
        }

        private async Task DeleteImage(string imageId)
        {
            using var instance = _liteDbService.CreateInstance();
            var fs = instance.GetStorage<string>("wteImages", "wteChunks");
            fs.Delete(imageId);

            await Task.CompletedTask;
        }

        private string CreateSafeImageTitle(string title)
        {
            var dtNow = DateTimeOffset.UtcNow.ToString("yyMMdd_HHmmss");
            return $"{title}_{dtNow}";
        }



    }
}