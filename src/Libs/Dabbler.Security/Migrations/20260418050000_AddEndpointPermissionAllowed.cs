using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dabbler.Security.Migrations
{
    /// <inheritdoc />
    public partial class AddEndpointPermissionAllowed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows represent allowed endpoints under the legacy model,
            // so we backfill them to Allowed=true. New inserts from
            // EfCoreSecurityStore will set this column explicitly.
            migrationBuilder.AddColumn<bool>(
                name: "Allowed",
                table: "AppEndpointPermissions",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Allowed",
                table: "AppEndpointPermissions");
        }
    }
}
