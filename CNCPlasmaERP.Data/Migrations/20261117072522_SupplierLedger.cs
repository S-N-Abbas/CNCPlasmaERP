using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CNCPlasmaERP.Data.Migrations
{
    /// <inheritdoc />
    public partial class SupplierLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBalance",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "Suppliers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "SupplierPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaidByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPayments_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPayments_Users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<int>(type: "int", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RunningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockPurchaseId = table.Column<int>(type: "int", nullable: true),
                    SupplierPaymentId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierLedgerEntries_StockPurchases_StockPurchaseId",
                        column: x => x.StockPurchaseId,
                        principalTable: "StockPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierLedgerEntries_SupplierPayments_SupplierPaymentId",
                        column: x => x.SupplierPaymentId,
                        principalTable: "SupplierPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierLedgerEntries_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLedgerEntries_StockPurchaseId",
                table: "SupplierLedgerEntries",
                column: "StockPurchaseId",
                unique: true,
                filter: "[StockPurchaseId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLedgerEntries_SupplierId_EntryDate",
                table: "SupplierLedgerEntries",
                columns: new[] { "SupplierId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierLedgerEntries_SupplierPaymentId",
                table: "SupplierLedgerEntries",
                column: "SupplierPaymentId",
                unique: true,
                filter: "[SupplierPaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PaidByUserId",
                table: "SupplierPayments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_SupplierId",
                table: "SupplierPayments",
                column: "SupplierId");

            // Existing purchases predate supplier accounts. Backfill them so the
            // first statement and current payable balance include historical debt.
            migrationBuilder.Sql(@"
                ;WITH PurchaseRows AS
                (
                    SELECT p.Id, p.SupplierId, p.PurchaseDate, p.MaterialCost + p.CarriageCost AS Amount,
                           SUM(p.MaterialCost + p.CarriageCost) OVER
                               (PARTITION BY p.SupplierId ORDER BY p.PurchaseDate, p.Id ROWS UNBOUNDED PRECEDING) AS RunningTotal
                    FROM StockPurchases p
                    WHERE p.IsDeleted = 0
                )
                INSERT INTO SupplierLedgerEntries
                    (SupplierId, EntryDate, Type, Debit, Credit, RunningBalance, StockPurchaseId, SupplierPaymentId, Notes, CreatedAt, UpdatedAt, IsDeleted)
                SELECT p.SupplierId, p.PurchaseDate, 1, 0, p.Amount,
                       s.OpeningBalance + p.RunningTotal, p.Id, NULL,
                       CASE WHEN NULLIF(LTRIM(RTRIM(sp.SupplierInvoiceNumber)), '') IS NULL
                            THEN CONCAT('Historical stock purchase: ', CONVERT(nvarchar(40), sp.WeightKg), ' kg')
                            ELSE CONCAT('Historical stock purchase invoice ', sp.SupplierInvoiceNumber) END,
                       SYSUTCDATETIME(), NULL, 0
                FROM PurchaseRows p
                JOIN StockPurchases sp ON sp.Id = p.Id
                JOIN Suppliers s ON s.Id = p.SupplierId;

                UPDATE s
                SET CurrentBalance = s.OpeningBalance + ISNULL(p.TotalAmount, 0)
                FROM Suppliers s
                OUTER APPLY
                (
                    SELECT SUM(MaterialCost + CarriageCost) AS TotalAmount
                    FROM StockPurchases
                    WHERE SupplierId = s.Id AND IsDeleted = 0
                ) p;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierLedgerEntries");

            migrationBuilder.DropTable(
                name: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "CurrentBalance",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "Suppliers");
        }
    }
}
