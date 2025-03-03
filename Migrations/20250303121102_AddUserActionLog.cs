using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GraduationProjectBackendAPI.Migrations
{
    public partial class AddUserActionLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adminActionLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    TargetUserId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionDetails = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adminActionLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_adminActionLogs_UsersT_AdminId",
                        column: x => x.AdminId,
                        principalTable: "UsersT",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_adminActionLogs_UsersT_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "UsersT",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_adminActionLogs_AdminId",
                table: "adminActionLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_adminActionLogs_TargetUserId",
                table: "adminActionLogs",
                column: "TargetUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adminActionLogs");
        }
    }
}
