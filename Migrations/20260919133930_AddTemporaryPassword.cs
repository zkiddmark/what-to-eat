using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhatToEatApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporaryPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TemporaryPasswordExpiresAt",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemporaryPasswordExpiresAt",
                table: "Users");
        }
    }
}
