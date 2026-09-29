using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CNCPlasmaERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class SequenceYearRollOver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceSequenceYear",
                table: "AppSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "JobSequenceYear",
                table: "AppSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QuotationSequenceYear",
                table: "AppSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceSequenceYear",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "JobSequenceYear",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "QuotationSequenceYear",
                table: "AppSettings");
        }
    }
}
