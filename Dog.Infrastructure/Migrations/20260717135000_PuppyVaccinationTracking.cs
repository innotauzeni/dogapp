using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PuppyVaccinationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make DogId nullable on VaccinationRecord so puppies can have vaccines before promotion.
            migrationBuilder.AlterColumn<int>(
                name: "DogId",
                table: "ka_vaccinationrecords",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            // Add PuppyId FK to link vaccines to unregistered puppies.
            migrationBuilder.AddColumn<int>(
                name: "PuppyId",
                table: "ka_vaccinationrecords",
                type: "int",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // FK constraint: VaccinationRecord.PuppyId -> Puppy.Id
            migrationBuilder.CreateIndex(
                name: "IX_ka_vaccinationrecords_PuppyId",
                table: "ka_vaccinationrecords",
                column: "PuppyId");

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
                name: "FK_ka_vaccinationrecords_ka_puppies_PuppyId",
                table: "ka_vaccinationrecords");

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
        }
    }
}
