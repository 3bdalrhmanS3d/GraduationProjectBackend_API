using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationProjectBackendAPI.Migrations
{
    public partial class progress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentContentId",
                table: "UserProgresses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Sections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Levels",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_CurrentContentId",
                table: "UserProgresses",
                column: "CurrentContentId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Contents_CurrentContentId",
                table: "UserProgresses",
                column: "CurrentContentId",
                principalTable: "Contents",
                principalColumn: "ContentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Contents_CurrentContentId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_CurrentContentId",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "CurrentContentId",
                table: "UserProgresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Levels");
        }
    }
}
