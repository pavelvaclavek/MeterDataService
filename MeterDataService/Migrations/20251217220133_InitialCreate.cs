using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MeterDataService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeterReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Network = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    System = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Data_1_8_0 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Data_1_8_1 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Data_1_8_2 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Data_2_8_0 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeterReadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_SerialNumber",
                table: "MeterReadings",
                column: "SerialNumber");

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_SerialNumber_Timestamp",
                table: "MeterReadings",
                columns: new[] { "SerialNumber", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MeterReadings_Timestamp",
                table: "MeterReadings",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeterReadings");
        }
    }
}
