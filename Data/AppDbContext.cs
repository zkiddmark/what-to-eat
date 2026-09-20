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
        public DbSet<DishVote> DishVotes => Set<DishVote>();
        public DbSet<MealPlan> MealPlans => Set<MealPlan>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var ingredientsComparer = new ValueComparer<IList<string>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                v => new List<string>(v));

            modelBuilder.Entity<Dish>(dish =>
            {
                dish.HasKey(x => x.Id);
                dish.HasIndex(x => x.OwnerId);
                // RESTRICT: att avslå eller ta bort en användare ska inte tyst radera recepten.
                dish.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(x => x.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<DishVote>(vote =>
            {
                vote.HasKey(x => x.Id);
                // Garantin för "en röst per användare och rätt".
                vote.HasIndex(x => new { x.DishId, x.UserId }).IsUnique();
                // Kaskad, till skillnad från ägarskapet: en röst utan sin rätt eller sin
                // användare är meningslös, medan ett recept utan ägare är data man vill behålla.
                vote.HasOne<Dish>().WithMany().HasForeignKey(x => x.DishId).OnDelete(DeleteBehavior.Cascade);
                vote.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MealPlan>(plan =>
            {
                plan.HasKey(x => x.Id);
                // Garantin för "en rätt per dag och användare".
                plan.HasIndex(x => new { x.UserId, x.Date }).IsUnique();
                // Kaskad som för rösterna: en planeringspost utan sin rätt eller sin
                // användare är meningslös. Det är också det som gör att en raderad rätt
                // inte lämnar en trasig rad i någons vecka.
                plan.HasOne<Dish>().WithMany().HasForeignKey(x => x.DishId).OnDelete(DeleteBehavior.Cascade);
                plan.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
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
