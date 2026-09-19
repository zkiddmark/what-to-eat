using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using WhatToEatApp.Data;
using WhatToEatApp.Enums;
using WhatToEatApp.Services.Auth;

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

    /// <summary>Kastas när den inloggade varken äger rätten eller är admin.</summary>
    public class ForbiddenException : Exception
    {
        public ForbiddenException() : base("Du får inte ändra den här rätten.")
        {
        }
    }

    public class DishService : IDishService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly AuthenticationStateProvider _authenticationStateProvider;

        public DishService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            AuthenticationStateProvider authenticationStateProvider)
        {
            _dbContextFactory = dbContextFactory;
            _authenticationStateProvider = authenticationStateProvider;
        }

        /// <summary>
        /// Den inloggade användaren hämtas alltid härifrån. Varken id eller roll kommer
        /// någonsin från anroparen, så det finns inget för en klient att hitta på.
        /// </summary>
        private async Task<(Guid Id, bool IsAdmin)> GetCurrentUserAsync()
        {
            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var userId = AuthClaims.GetUserId(state.User);
            if (userId is null)
            {
                throw new ForbiddenException();
            }

            using var db = _dbContextFactory.CreateDbContext();
            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId.Value);
            if (user is null || user.Status != AccountStatus.Approved)
            {
                throw new ForbiddenException();
            }
            return (user.Id, user.Role == UserService.AdminRole);
        }

        private static void EnsureMayEdit(Entities.Dish dish, (Guid Id, bool IsAdmin) current)
        {
            if (!current.IsAdmin && dish.OwnerId != current.Id)
            {
                throw new ForbiddenException();
            }
        }

        public async Task AddDishAsync(DishDto dishDto)
        {
            var current = await GetCurrentUserAsync();

            using var db = _dbContextFactory.CreateDbContext();

            var newDish = dishDto.MapToNewDish(current.Id);
            if (dishDto.Image is not null)
            {
                newDish.ImageId = await AddImageFromFileAsync(db, dishDto.Image, dishDto.Title);
            }

            db.Dishes.Add(newDish);
            await db.SaveChangesAsync();
        }

        public async Task UpdateDishAsync(DishDto dishDto)
        {
            var current = await GetCurrentUserAsync();

            using var db = _dbContextFactory.CreateDbContext();
            // Get dish from db.
            var dishToUpdate = await db.Dishes.FirstAsync(x => x.Id == dishDto.DishId);
            EnsureMayEdit(dishToUpdate, current);

            // Image has changed, remove the old one. En ny uppladdad fil ersätter den gamla
            // bilden även när id:t är oförändrat — modalen nollställer bara ImageId när
            // användaren aktivt tar bort bilden, inte när hen väljer en ny fil.
            if (dishToUpdate.ImageId is not null
                && (dishDto.Image is not null || dishToUpdate.ImageId != dishDto.ImageId))
            {
                await DeleteImage(dishToUpdate.ImageId);
            }

            dishToUpdate.UpdateDish(dishDto.MapToNewDish(dishToUpdate.OwnerId));

            if (dishDto.Image is not null)
            {
                dishToUpdate.ImageId = await AddImageFromFileAsync(db, dishDto.Image, dishDto.Title);
            }
            await db.SaveChangesAsync();
        }

        public async Task DeleteDishAsync(DishDto dishDto)
        {
            var current = await GetCurrentUserAsync();

            using var db = _dbContextFactory.CreateDbContext();
            var dishToDelete = await db.Dishes.FirstOrDefaultAsync(x => x.Id == dishDto.DishId);
            if (dishToDelete is null)
            {
                return;
            }
            EnsureMayEdit(dishToDelete, current);

            // Bild-id:t tas från den lästa raden, inte ur DTO:n: annars kan en klient peka ut
            // någon annans bild för radering genom att skicka ett annat ImageId.
            var imageId = dishToDelete.ImageId;

            db.Dishes.Remove(dishToDelete);
            await db.SaveChangesAsync();

            // If the dish had an associated image, delete it.
            if (imageId is not null)
            {
                await DeleteImage(imageId);
            }
        }

        public async Task<IEnumerable<DishDto>> GetAllDishes(int skip = 0, int take = 10)
        {
            var current = await GetCurrentUserAsync();
            using var db = _dbContextFactory.CreateDbContext();
            // SQLite kan inte sortera på DateTimeOffset i ORDER BY, så ordningen görs i minnet.
            // Hela tabellen läses — samma sak som GetTodaysDish redan gör, och datamängden
            // är i storleksordningen tiotals rätter.
            var dishes = await db.Dishes.ToListAsync();
            var page = dishes
                .OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.When)
                .ThenBy(x => x.Id)
                .Skip(skip)
                .Take(take)
                .ToList();
            return await DecorateAsync(db, page, current);
        }

        public async Task<int> DishesCount()
        {
            await GetCurrentUserAsync();
            using var db = _dbContextFactory.CreateDbContext();
            return await db.Dishes.CountAsync();
        }

        public async Task<DishDto?> GetTodaysDish(Days day)
        {
            var current = await GetCurrentUserAsync();

            using var db = _dbContextFactory.CreateDbContext();
            var dishes = await db.Dishes.ToListAsync();
            var dish = dishes.LastOrDefault(x => x.When.Date == day.ResolveDayOfWeek().Date);
            if (dish is null)
            {
                return null;
            }
            var dishDto = (await DecorateAsync(db, new List<Entities.Dish> { dish }, current)).Single();
            dishDto.When = day.ResolveDayOfWeek();
            return dishDto;
        }

        /// <summary>
        /// Fyller i ägarens alias och om den inloggade får ändra rätten. Aliasen hämtas i en
        /// fråga för hela sidan, inte en per rad.
        /// </summary>
        private static async Task<List<DishDto>> DecorateAsync(
            AppDbContext db, List<Entities.Dish> dishes, (Guid Id, bool IsAdmin) current)
        {
            var ownerIds = dishes.Select(x => x.OwnerId).Distinct().ToList();
            var aliases = await db.Users
                .Where(u => ownerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Alias);

            return dishes.Select(dish =>
            {
                var dto = dish.MapToDishDto();
                dto.OwnerAlias = aliases.TryGetValue(dish.OwnerId, out var alias) ? alias : string.Empty;
                dto.CanEdit = current.IsAdmin || dish.OwnerId == current.Id;
                return dto;
            }).ToList();
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

        /// <summary>
        /// Lägger bilden i anroparens context utan att spara — bild och rätt skrivs i samma
        /// SaveChanges, så en misslyckad sparning inte lämnar en föräldralös bild efter sig.
        /// </summary>
        private async Task<string> AddImageFromFileAsync(AppDbContext db, IBrowserFile file, string title)
        {
            using var ms = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(ms);
            var newId = Guid.NewGuid();
            db.DishImages.Add(new Entities.DishImage
            {
                Id = newId,
                FileName = CreateSafeImageTitle(title),
                Content = ms.ToArray()
            });
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
