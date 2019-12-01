using Microsoft.EntityFrameworkCore.Migrations;

namespace OC.DiscordBotServer.Migrations
{
    public partial class AddOCUserToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "OCUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Token",
                table: "OCUsers");
        }
    }
}
