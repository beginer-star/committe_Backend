using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinayakaApp.API.Migrations
{
    public partial class CalendarAvailability : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type", table: "CalendarEvents", type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Available");
            migrationBuilder.AddColumn<int>(
                name: "MemberId", table: "CalendarEvents", type: "integer", nullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_MemberId", table: "CalendarEvents", column: "MemberId");
            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_Users_MemberId", table: "CalendarEvents", column: "MemberId", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_CalendarEvents_Users_MemberId", table: "CalendarEvents");
            migrationBuilder.DropIndex(name: "IX_CalendarEvents_MemberId", table: "CalendarEvents");
            migrationBuilder.DropColumn(name: "MemberId", table: "CalendarEvents");
            migrationBuilder.DropColumn(name: "Type", table: "CalendarEvents");
        }
    }
}
