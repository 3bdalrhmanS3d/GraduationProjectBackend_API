using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationProjectBackendAPI.Migrations
{
    public partial class editContentCourse2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_CurrentLevelId",
                table: "UserProgresses",
                column: "CurrentLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProgresses_CurrentSectionId",
                table: "UserProgresses",
                column: "CurrentSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Levels_CurrentLevelId",
                table: "UserProgresses",
                column: "CurrentLevelId",
                principalTable: "Levels",
                principalColumn: "LevelId",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Sections_CurrentSectionId",
                table: "UserProgresses",
                column: "CurrentSectionId",
                principalTable: "Sections",
                principalColumn: "SectionId",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Levels_CurrentLevelId",
                table: "UserProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Sections_CurrentSectionId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_CurrentLevelId",
                table: "UserProgresses");

            migrationBuilder.DropIndex(
                name: "IX_UserProgresses_CurrentSectionId",
                table: "UserProgresses");
        }
    }
}
