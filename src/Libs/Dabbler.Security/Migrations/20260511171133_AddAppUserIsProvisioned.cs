using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dabbler.Security.Migrations
{
    /// <inheritdoc />
    public partial class AddAppUserIsProvisioned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProvisioned",
                table: "AppUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsProvisioned",
                table: "AppUsers");
        }
    }
}
