using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeSubTypeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "MediaEntries");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "MediaEntries");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "MediaEntries",
                newName: "SubType");

            migrationBuilder.AlterColumn<int>(
                name: "Rating",
                table: "MediaEntries",
                type: "int",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubType",
                table: "MediaEntries",
                newName: "Total");

            migrationBuilder.AlterColumn<byte>(
                name: "Rating",
                table: "MediaEntries",
                type: "tinyint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "MediaEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "MediaEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "MediaEntries",
                type: "datetime2",
                nullable: true);
        }
    }
}
