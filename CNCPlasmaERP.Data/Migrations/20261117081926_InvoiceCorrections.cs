using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CNCPlasmaERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class InvoiceCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCharges",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InvoiceAdjustmentId",
                table: "CustomerLedgerEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoiceAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousOtherCharges = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewOtherCharges = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousGrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewGrandTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdjustedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceAdjustments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceAdjustments_Users_AdjustedByUserId",
                        column: x => x.AdjustedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLedgerEntries_InvoiceAdjustmentId",
                table: "CustomerLedgerEntries",
                column: "InvoiceAdjustmentId",
                unique: true,
                filter: "[InvoiceAdjustmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAdjustments_AdjustedByUserId",
                table: "InvoiceAdjustments",
                column: "AdjustedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAdjustments_InvoiceId_AdjustmentDate",
                table: "InvoiceAdjustments",
                columns: new[] { "InvoiceId", "AdjustmentDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerLedgerEntries_InvoiceAdjustments_InvoiceAdjustmentId",
                table: "CustomerLedgerEntries",
                column: "InvoiceAdjustmentId",
                principalTable: "InvoiceAdjustments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerLedgerEntries_InvoiceAdjustments_InvoiceAdjustmentId",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropTable(
                name: "InvoiceAdjustments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerLedgerEntries_InvoiceAdjustmentId",
                table: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "OtherCharges",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "InvoiceAdjustmentId",
                table: "CustomerLedgerEntries");
        }
    }
}
