using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPuppyRegisteredDogId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegisteredDogId",
                table: "ka_puppies",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegisteredDogId",
                table: "ka_puppies");
        }
    }
}
