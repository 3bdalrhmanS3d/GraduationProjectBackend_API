using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationProjectBackendAPI.Migrations
{
    public partial class editCourseTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstructorId",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "aboutCourses",
                columns: table => new
                {
                    AboutCourseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    AboutCourseText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aboutCourses", x => x.AboutCourseId);
                    table.ForeignKey(
                        name: "FK_aboutCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "courseSkills",
                columns: table => new
                {
                    CourseSkillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    CourseSkillText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courseSkills", x => x.CourseSkillId);
                    table.ForeignKey(
                        name: "FK_courseSkills_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_InstructorId",
                table: "Courses",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_aboutCourses_CourseId",
                table: "aboutCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_courseSkills_CourseId",
                table: "courseSkills",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_UsersT_InstructorId",
                table: "Courses",
                column: "InstructorId",
                principalTable: "UsersT",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_UsersT_InstructorId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "aboutCourses");

            migrationBuilder.DropTable(
                name: "courseSkills");

            migrationBuilder.DropIndex(
                name: "IX_Courses_InstructorId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "InstructorId",
                table: "Courses");
        }
    }
}
