using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatToEatApp.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DishId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlans_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MealPlans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_DishId",
                table: "MealPlans",
                column: "DishId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_UserId_Date",
                table: "MealPlans",
                columns: new[] { "UserId", "Date" },
                unique: true);

            // Ordningen är inte förhandlingsbar: tabellen skapas, datan kopieras, och
            // FÖRST därefter släpps kolumnen. Släpps den tidigare är planeringen borta.
            //
            // Befintlig planering tillfaller rättens ägare — fältet har aldrig burit en
            // användare, och ägarmigreringen i story 008 gav rätterna till admin.
            // Villkoret date("When") >= date('now') gör två saker på en gång: förflutna
            // datum är historik och migreras inte, och DateTimeOffset.MinValue (år 1)
            // faller bort av samma skäl.
            //
            // Id:t byggs om till EF:s Guid-format (8-4-4-4-12, versaler). hex(randomblob(16))
            // ger 32 tecken utan bindestreck och blir oläsbart för EF utan den här
            // ombyggnaden. Subfrågan gör att randomblob utvärderas en gång per rad.
            migrationBuilder.Sql("""
                INSERT INTO MealPlans (Id, UserId, DishId, Date)
                SELECT
                    upper(substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4)
                          || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12)),
                    OwnerId,
                    Id,
                    date("When")
                FROM (
                    SELECT hex(randomblob(16)) AS h, OwnerId, Id, "When"
                    FROM Dishes
                    WHERE date("When") >= date('now')
                );
                """);

            migrationBuilder.DropColumn(
                name: "When",
                table: "Dishes");
        }

        /// <summary>
        /// Down() återskapar kolumnen men INTE datan. Vem som planerade vad går inte att
        /// härleda ur en tabell som tagits bort, och en rätt kan ha legat i flera veckor.
        /// Att rulla tillbaka den här migreringen innebär att planeringen är borta.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealPlans");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "When",
                table: "Dishes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
