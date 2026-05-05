using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Asn.Diplomski.Rdbm.Migrations
{
    /// <inheritdoc />
    public partial class Step002_SensorReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "soil_moisture_readings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    SensorId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_soil_moisture_readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_soil_moisture_readings_sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "temperature_readings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    SensorId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_temperature_readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_temperature_readings_sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_soil_moisture_readings_SensorId_RecordedAt",
                table: "soil_moisture_readings",
                columns: new[] { "SensorId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_temperature_readings_SensorId_RecordedAt",
                table: "temperature_readings",
                columns: new[] { "SensorId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "soil_moisture_readings");

            migrationBuilder.DropTable(
                name: "temperature_readings");
        }
    }
}
