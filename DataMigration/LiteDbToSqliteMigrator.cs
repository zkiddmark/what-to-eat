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

            if (db.Dishes.Any() || db.DishImages.Any())
            {
                Console.Error.WriteLine($"Måldatabasen '{target}' innehåller redan data. Migreringen är en engångskörning — kör mot en tom fil. Inget har skrivits.");
                return 1;
            }

            using var lite = new LiteDatabase($"Filename={source};Connection=shared;ReadOnly=true");
            var dishes = lite.GetCollection<BsonDocument>("dishes").FindAll().ToList();
            var storage = lite.GetStorage<string>("wteImages", "wteChunks");
            var files = storage.FindAll().ToList();

            using var transaction = db.Database.BeginTransaction();
            var dishesRead = 0;
            var imagesRead = 0;
            var currentDish = "(ingen)";
            try
            {
                foreach (var doc in dishes)
                {
                    currentDish = doc.TryGetValue("Title", out var title) && title.IsString ? title.AsString : "(namnlös)";
                    db.Dishes.Add(MapDish(doc));
                    dishesRead++;
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
                    imagesRead++;
                }

                db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.Error.WriteLine($"Fel vid \"{currentDish}\" (efter {dishesRead} lästa rätter och {imagesRead} lästa bilder): {ex.InnerException?.Message ?? ex.Message}");
                Console.Error.WriteLine("Inget har skrivits — allt rullades tillbaka.");
                return 1;
            }

            Console.WriteLine($"  Rätter:  {dishes.Count} lästa, {db.Dishes.Count()} skrivna");
            Console.WriteLine($"  Bilder:  {files.Count} lästa, {db.DishImages.Count()} skrivna");
            Console.WriteLine("  Klart. Originalfilen är orörd.");
            return 0;
        }

        /// <summary>
        /// LiteDB lagrar DateTime i UTC. Vi normaliserar explicit till UTC i stället för att
        /// förlita oss på värdets Kind, och uttrycker det sedan i maskinens lokala offset —
        /// samma tid som LiteDB-versionen visade. Kör migreringen i samma tidszon som appen.
        /// </summary>
        private static DateTimeOffset ToLocalOffset(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
            return new DateTimeOffset(utc).ToLocalTime();
        }

        private static Dish MapDish(BsonDocument doc)
        {
            var ingredients = doc["Ingredients"].IsArray
                ? doc["Ingredients"].AsArray.Select(x => x.AsString).ToList()
                : new List<string>();

            return new Dish(
                Guid.NewGuid(),
                Required(doc, "Title").AsString,
                OptionalText(doc, "Notes") ?? string.Empty,
                OptionalText(doc, "ImgUrl"),
                OptionalText(doc, "RecipeUrl"),
                ingredients,
                Required(doc, "Rating").AsInt32,
                ToLocalOffset(Required(doc, "When").AsDateTime),
                OptionalText(doc, "ImageId"));
        }

        /// <summary>
        /// Gamla dokument saknar nycklar helt eller har dem satta till null — LiteDB skiljer
        /// inte på fallen vid indexering. Textfält utan värde blir null, och Notes blir
        /// string.Empty, samma tomma fält som användaren ser idag.
        /// </summary>
        private static string? OptionalText(BsonDocument doc, string field)
        {
            return doc.TryGetValue(field, out var value) && !value.IsNull ? value.AsString : null;
        }

        private static BsonValue Required(BsonDocument doc, string field)
        {
            if (!doc.TryGetValue(field, out var value) || value.IsNull)
            {
                throw new InvalidOperationException($"Fältet '{field}' saknas eller är null.");
            }
            return value;
        }

    }
}
