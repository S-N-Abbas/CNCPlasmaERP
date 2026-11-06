using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CNCPlasmaERP.Data.Migrations
{
    public partial class ReliabilityHardening : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Accounts created by the former hard-coded bootstrap credential
            // must replace it before entering the application.
            migrationBuilder.Sql("UPDATE [Users] SET [MustChangePassword] = 1 WHERE [Username] = N'admin' AND [IsDeleted] = 0");

            migrationBuilder.DropIndex(name: "IX_CustomerLedgerEntries_InvoiceId", table: "CustomerLedgerEntries");
            migrationBuilder.CreateIndex(
                name: "IX_CustomerLedgerEntries_InvoiceId",
                table: "CustomerLedgerEntries",
                column: "InvoiceId",
                unique: true,
                filter: "[InvoiceId] IS NOT NULL");
            migrationBuilder.DropIndex(name: "IX_CustomerLedgerEntries_PaymentId", table: "CustomerLedgerEntries");
            migrationBuilder.CreateIndex(
                name: "IX_CustomerLedgerEntries_PaymentId",
                table: "CustomerLedgerEntries",
                column: "PaymentId",
                unique: true,
                filter: "[PaymentId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_CustomerLedgerEntries_InvoiceId", table: "CustomerLedgerEntries");
            migrationBuilder.DropIndex(name: "IX_CustomerLedgerEntries_PaymentId", table: "CustomerLedgerEntries");
            migrationBuilder.CreateIndex(name: "IX_CustomerLedgerEntries_InvoiceId", table: "CustomerLedgerEntries", column: "InvoiceId");
            migrationBuilder.CreateIndex(name: "IX_CustomerLedgerEntries_PaymentId", table: "CustomerLedgerEntries", column: "PaymentId");
            migrationBuilder.DropColumn(name: "MustChangePassword", table: "Users");
        }
    }
}
