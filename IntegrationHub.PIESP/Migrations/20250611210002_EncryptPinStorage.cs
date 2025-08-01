using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationHub.PIESP.Migrations
{
    /// <inheritdoc />
    public partial class EncryptPinStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pin",
                table: "Users",
                newName: "PinHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PinHash",
                table: "Users",
                newName: "Pin");
        }
    }
}
