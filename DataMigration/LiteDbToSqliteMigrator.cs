using LiteDB;
using Microsoft.EntityFrameworkCore;
using WhatToEatApp.Data;
using WhatToEatApp.Entities;

namespace WhatToEatApp.DataMigration
{
    /// <summary>
    /// Engångskörning: läser en LiteDB-fil och skriver över allt till SQLite.
    /// Källfilen öppnas read-only och rörs aldrig.
    /// </summary>
    public static class LiteDbToSqliteMigrator
    {
        public static int Run(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Användning: dotnet WhatToEatApp.dll --migrate-litedb <källa.db> <mål.sqlite>");
                return 1;
            }

            var source = args[1];
            var target = args[2];

            if (!File.Exists(source))
            {
                Console.Error.WriteLine($"Källfilen '{source}' finns inte. Inget har skrivits.");
                return 1;
            }

            Console.WriteLine($"Migrerar {Path.GetFileName(source)} -> {Path.GetFileName(target)}");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={target}")
                .Options;

            using var db = new AppDbContext(options);
            db.Database.Migrate();

            using var lite = new LiteDatabase($"Filename={source};Connection=shared;ReadOnly=true");
            var dishes = lite.GetCollection<BsonDocument>("dishes").FindAll().ToList();
            var storage = lite.GetStorage<string>("wteImages", "wteChunks");
            var files = storage.FindAll().ToList();

            using var transaction = db.Database.BeginTransaction();
            var dishesWritten = 0;
            var imagesWritten = 0;
            try
            {
                foreach (var doc in dishes)
                {
                    db.Dishes.Add(MapDish(doc));
                    dishesWritten++;
                }

                foreach (var file in files)
                {
                    using var ms = new MemoryStream();
                    storage.OpenRead(file.Id).CopyTo(ms);
                    db.DishImages.Add(new DishImage
                    {
                        Id = Guid.Parse(file.Id),
                        FileName = file.Filename,
                        Content = ms.ToArray()
                    });
                    imagesWritten++;
                }

                db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.Error.WriteLine($"Fel efter {dishesWritten} lästa rätter och {imagesWritten} lästa bilder: {ex.InnerException?.Message ?? ex.Message}");
                Console.Error.WriteLine("Inget har skrivits — allt rullades tillbaka.");
                return 1;
            }

            Console.WriteLine($"  Rätter:  {dishes.Count} lästa, {dishesWritten} skrivna");
            Console.WriteLine($"  Bilder:  {files.Count} lästa, {imagesWritten} skrivna");
            Console.WriteLine("  Klart. Originalfilen är orörd.");
            return 0;
        }

        private static Dish MapDish(BsonDocument doc)
        {
            var ingredients = doc["Ingredients"].IsArray
                ? doc["Ingredients"].AsArray.Select(x => x.AsString).ToList()
                : new List<string>();

            return new Dish(
                Guid.NewGuid(),
                doc["Title"].AsString,
                doc["Notes"].AsString,
                doc["ImgUrl"].IsNull ? null : doc["ImgUrl"].AsString,
                doc["RecipeUrl"].IsNull ? null : doc["RecipeUrl"].AsString,
                ingredients,
                doc["Rating"].AsInt32,
                doc["When"].AsDateTime,
                doc["ImageId"].IsNull ? null : doc["ImageId"].AsString);
        }
    }
}
