using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Olwe.Data.Migrations
{
    public partial class addsomemodconfigs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AntiPhishingEnabled",
                table: "ModConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PhishingInfractionType",
                table: "ModConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Prefix",
                table: "Guilds",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AntiPhishingEnabled",
                table: "ModConfigs");

            migrationBuilder.DropColumn(
                name: "PhishingInfractionType",
                table: "ModConfigs");

            migrationBuilder.AlterColumn<string>(
                name: "Prefix",
                table: "Guilds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
