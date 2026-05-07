using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asn.Diplomski.Rdbm.Migrations
{
    /// <inheritdoc />
    public partial class Step006_Add_provisioned_field_for_device : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProvisionStatus",
                table: "devices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProvisionStatus",
                table: "devices");
        }
    }
}
