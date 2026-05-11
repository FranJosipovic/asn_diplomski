using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asn.Diplomski.Rdbm.Migrations
{
    /// <inheritdoc />
    public partial class Step007_DeviceStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProvisionStatus",
                table: "devices",
                newName: "Status");

            // Old Provisioned=2 maps to new Ready=3; all other existing values (0,1) are compatible.
            migrationBuilder.Sql("UPDATE devices SET \"Status\" = 3 WHERE \"Status\" = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE devices SET \"ProvisionStatus\" = 2 WHERE \"ProvisionStatus\" = 3");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "devices",
                newName: "ProvisionStatus");
        }
    }
}
