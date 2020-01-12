using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OC.DiscordBotServer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Chanel2Servers",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false),
                    IP = table.Column<string>(nullable: false),
                    Port = table.Column<int>(nullable: false),
                    LastOnlineTime = table.Column<DateTime>(nullable: false),
                    LastCheckTime = table.Column<DateTime>(nullable: false),
                    LinkCreator = table.Column<ulong>(nullable: false),
                    Token = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chanel2Servers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OCUsers",
                columns: table => new
                {
                    DiscordIdChanel = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    OCLogin = table.Column<string>(nullable: true),
                    LastActiveTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OCUsers", x => new { x.DiscordIdChanel, x.UserId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Chanel2Servers");

            migrationBuilder.DropTable(
                name: "OCUsers");
        }
    }
}