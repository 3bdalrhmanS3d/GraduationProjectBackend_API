using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationProjectBackendAPI.Migrations
{
    public partial class addTracks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseTracks",
                columns: table => new
                {
                    TrackId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrackDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTracks", x => x.TrackId);
                });

            migrationBuilder.CreateTable(
                name: "CourseTrackCourses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTrackCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseTrackCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_CourseTrackCourses_CourseTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "CourseTracks",
                        principalColumn: "TrackId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseTrackCourses_CourseId",
                table: "CourseTrackCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseTrackCourses_TrackId",
                table: "CourseTrackCourses",
                column: "TrackId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseTrackCourses");

            migrationBuilder.DropTable(
                name: "CourseTracks");
        }
    }
}
