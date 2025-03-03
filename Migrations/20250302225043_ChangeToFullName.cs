using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationProjectBackendAPI.Migrations
{
    public partial class ChangeToFullName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "UsersT");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "UsersT");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "UsersT",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullName",
                table: "UsersT");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "UsersT",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "UsersT",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
