using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowExternalMatingPartner : Migration
    {
        // NOTE: intentionally scoped to ka_matingrecords only. EF wanted to bundle a lot of
        // unrelated model drift (columns that already exist in the database) into this migration,
        // which would fail on apply. This migration only makes the mating partner optional and
        // adds free-text partner names for unregistered dogs.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SireId",
                table: "ka_matingrecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "DamId",
                table: "ka_matingrecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "SireName",
                table: "ka_matingrecords",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DamName",
                table: "ka_matingrecords",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SireName",
                table: "ka_matingrecords");

            migrationBuilder.DropColumn(
                name: "DamName",
                table: "ka_matingrecords");

            migrationBuilder.AlterColumn<int>(
                name: "SireId",
                table: "ka_matingrecords",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DamId",
                table: "ka_matingrecords",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
