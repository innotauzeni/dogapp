using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKaAppSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ka_vaccinationrecords_ka_dogs_DogId",
                table: "ka_vaccinationrecords");

            migrationBuilder.AlterColumn<int>(
                name: "DogId",
                table: "ka_vaccinationrecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PuppyId",
                table: "ka_vaccinationrecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KaAppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SettingKey = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SettingValue = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KaAppSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ka_vaccinationrecords_PuppyId",
                table: "ka_vaccinationrecords",
                column: "PuppyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ka_vaccinationrecords_ka_dogs_DogId",
                table: "ka_vaccinationrecords",
                column: "DogId",
                principalTable: "ka_dogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ka_vaccinationrecords_ka_puppies_PuppyId",
                table: "ka_vaccinationrecords",
                column: "PuppyId",
                principalTable: "ka_puppies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ka_vaccinationrecords_ka_dogs_DogId",
                table: "ka_vaccinationrecords");

            migrationBuilder.DropForeignKey(
                name: "FK_ka_vaccinationrecords_ka_puppies_PuppyId",
                table: "ka_vaccinationrecords");

            migrationBuilder.DropTable(
                name: "KaAppSettings");

            migrationBuilder.DropIndex(
                name: "IX_ka_vaccinationrecords_PuppyId",
                table: "ka_vaccinationrecords");

            migrationBuilder.DropColumn(
                name: "PuppyId",
                table: "ka_vaccinationrecords");

            migrationBuilder.AlterColumn<int>(
                name: "DogId",
                table: "ka_vaccinationrecords",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ka_vaccinationrecords_ka_dogs_DogId",
                table: "ka_vaccinationrecords",
                column: "DogId",
                principalTable: "ka_dogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
