using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Asn.Diplomski.Rdbm.Migrations
{
    /// <inheritdoc />
    public partial class Step005_Move_provisioning_to_device : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProvisioningToken",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "ProvisioningTokenExpiresAt",
                table: "tenants");

            migrationBuilder.AddColumn<string>(
                name: "ProvisioningToken",
                table: "devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProvisioningTokenExpiresAt",
                table: "devices",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProvisioningToken",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "ProvisioningTokenExpiresAt",
                table: "devices");

            migrationBuilder.AddColumn<string>(
                name: "ProvisioningToken",
                table: "tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProvisioningTokenExpiresAt",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
