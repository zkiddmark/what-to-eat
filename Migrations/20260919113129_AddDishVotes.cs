using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatToEatApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDishVotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DishVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DishId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DishVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DishVotes_Dishes_DishId",
                        column: x => x.DishId,
                        principalTable: "Dishes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DishVotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DishVotes_DishId_UserId",
                table: "DishVotes",
                columns: new[] { "DishId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DishVotes_UserId",
                table: "DishVotes",
                column: "UserId");

            // Flytta betygen INNAN kolumnen försvinner. Ett satt betyg blir en röst från
            // rättens ägare — för de befintliga rätterna är det Peter, satt av story 008.
            // Betyg 0 betyder "aldrig satt", inte "underkänd", och blir därför ingen röst.
            migrationBuilder.Sql(@"
                INSERT INTO DishVotes (Id, DishId, UserId, Score)
                SELECT
                    substr(hex(randomblob(4)),1,8) || '-' ||
                    substr(hex(randomblob(2)),1,4) || '-' ||
                    substr(hex(randomblob(2)),1,4) || '-' ||
                    substr(hex(randomblob(2)),1,4) || '-' ||
                    substr(hex(randomblob(6)),1,12),
                    d.Id, d.OwnerId, d.Rating
                FROM Dishes d
                WHERE d.Rating > 0;");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Dishes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DishVotes");

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Dishes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
