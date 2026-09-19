using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WhatToEatApp.Entities;

namespace WhatToEatApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Dish> Dishes => Set<Dish>();
        public DbSet<DishImage> DishImages => Set<DishImage>();
        public DbSet<AppUser> Users => Set<AppUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var ingredientsComparer = new ValueComparer<IList<string>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => new List<string>(v));

            modelBuilder.Entity<Dish>(dish =>
            {
                dish.HasKey(x => x.Id);
                dish.Property(x => x.Ingredients)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Metadata.SetValueComparer(ingredientsComparer);
            });

            modelBuilder.Entity<DishImage>(image =>
            {
                image.HasKey(x => x.Id);
            });

            modelBuilder.Entity<AppUser>(user =>
            {
                user.HasKey(x => x.Id);
                user.HasIndex(x => x.Email).IsUnique();
                user.Property(x => x.Status).HasConversion<int>();
            });
        }
    }
}
