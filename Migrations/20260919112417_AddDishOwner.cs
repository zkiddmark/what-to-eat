using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatToEatApp.Migrations
{
    /// <inheritdoc />
    public partial class AddDishOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Dishes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Befintliga recept får peter@stjern.se som ägare. Kolumnen skapas med Guid.Empty
            // som default, så uppdateringen måste ske innan främmande nyckeln läggs på.
            //
            // Tom databas: inga rätter att uppdatera, ingenting händer.
            // Databas som redan kört story 006: admin-kontot finns och slås upp här.
            // Har appen aldrig startat med ADMIN_INITIAL_PASSWORD satt finns ingen admin,
            // och då fäller främmande nyckeln nedan migreringen i stället för att lämna
            // recept med en ägare som inte existerar.
            migrationBuilder.Sql(@"
                UPDATE Dishes
                SET OwnerId = (SELECT Id FROM Users WHERE Email = 'peter@stjern.se')
                WHERE EXISTS (SELECT 1 FROM Users WHERE Email = 'peter@stjern.se');");

            migrationBuilder.CreateIndex(
                name: "IX_Dishes_OwnerId",
                table: "Dishes",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dishes_Users_OwnerId",
                table: "Dishes",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dishes_Users_OwnerId",
                table: "Dishes");

            migrationBuilder.DropIndex(
                name: "IX_Dishes_OwnerId",
                table: "Dishes");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Dishes");
        }
    }
}
