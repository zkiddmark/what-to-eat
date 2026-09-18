using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using WhatToEatApp.Data;
using WhatToEatApp.Enums;

namespace WhatToEatApp.Services.Dish
{
    public interface IDishService
    {
        Task AddDishAsync(DishDto dish);
        Task UpdateDishAsync(DishDto dish);
        Task DeleteDishAsync(DishDto dish);
        Task<IEnumerable<DishDto>> GetAllDishes(int skip = 0, int take = 10);
        Task<int> DishesCount();
        Task<DishDto?> GetTodaysDish(Days day);
        Task<string> GetImageFromDbAsync(string imgId);
    }

    public class DishService : IDishService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DishService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task AddDishAsync(DishDto dishDto)
        {
            var newDish = dishDto.MapToNewDish();
            if (dishDto.Image is not null)
            {
                var imageId = await AddImageFromFileAsync(dishDto.Image, dishDto.Title);
                newDish.ImageId = imageId;
            }

            using var db = _dbContextFactory.CreateDbContext();
            db.Dishes.Add(dishDto.MapToNewDish());
            await db.SaveChangesAsync();
        }

        public async Task UpdateDishAsync(DishDto dishDto)
        {
            using var db = _dbContextFactory.CreateDbContext();
            // Get dish from db.
            var dishToUpdate = await db.Dishes.FirstAsync(x => x.Id == dishDto.DishId);

            // Image has changed, remove the old one.
            if (dishToUpdate.ImageId is not null && dishToUpdate.ImageId != dishDto.ImageId)
            {
                await DeleteImage(dishToUpdate.ImageId);
            }

            dishToUpdate.UpdateDish(dishDto.MapToNewDish());

            if (dishDto.Image is not null)
            {
                var imageId = await AddImageFromFileAsync(dishDto.Image, dishDto.Title);
                dishToUpdate.ImageId = imageId;
            }
            await db.SaveChangesAsync();
        }

        public async Task DeleteDishAsync(DishDto dishDto)
        {
            using var db = _dbContextFactory.CreateDbContext();
            await db.Dishes.Where(x => x.Id == dishDto.DishId).ExecuteDeleteAsync();

            // If the dish had an associated image, delete it.
            if (dishDto.ImageId is not null)
            {
                await DeleteImage(dishDto.ImageId);
            }
        }

        public async Task<IEnumerable<DishDto>> GetAllDishes(int skip = 0, int take = 10)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var dishes = await db.Dishes
                .OrderByDescending(x => x.Rating)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return dishes.Select(x => x.MapToDishDto()).ToList();
        }

        public async Task<int> DishesCount()
        {
            using var db = _dbContextFactory.CreateDbContext();
            return await db.Dishes.CountAsync();
        }

        public async Task<DishDto?> GetTodaysDish(Days day)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var dishes = await db.Dishes.ToListAsync();
            var dish = dishes.LastOrDefault(x => x.When.Date == day.ResolveDayOfWeek().Date);
            var dishDto = dish?.MapToDishDto();
            if (dishDto is null)
            {
                return null;
            }
            dishDto.When = day.ResolveDayOfWeek();
            return dishDto;
        }

        public async Task<string> GetImageFromDbAsync(string imgId)
        {
            if (imgId is null)
            {
                throw new InvalidOperationException("imgId must not be null!");
            }
            if (!Guid.TryParse(imgId, out var imageId))
            {
                return string.Empty;
            }

            using var db = _dbContextFactory.CreateDbContext();
            var content = await db.DishImages
                .Where(x => x.Id == imageId)
                .Select(x => x.Content)
                .FirstOrDefaultAsync();

            if (content is null)
            {
                return string.Empty;
            }
            return Convert.ToBase64String(content);
        }

        private async Task<string> AddImageFromFileAsync(IBrowserFile file, string title)
        {
            using var db = _dbContextFactory.CreateDbContext();
            using var ms = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(ms);
            var newId = Guid.NewGuid();
            db.DishImages.Add(new Entities.DishImage
            {
                Id = newId,
                FileName = CreateSafeImageTitle(title),
                Content = ms.ToArray()
            });
            await db.SaveChangesAsync();
            return newId.ToString();
        }

        private async Task DeleteImage(string imageId)
        {
            if (!Guid.TryParse(imageId, out var id))
            {
                return;
            }
            using var db = _dbContextFactory.CreateDbContext();
            await db.DishImages.Where(x => x.Id == id).ExecuteDeleteAsync();
        }

        private string CreateSafeImageTitle(string title)
        {
            var dtNow = DateTimeOffset.UtcNow.ToString("yyMMdd_HHmmss");
            return $"{title}_{dtNow}";
        }
    }
}
