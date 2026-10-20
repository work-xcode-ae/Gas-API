using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class allcontract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "invoice_id",
                table: "amc_payment_schedule",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_amc_payment_schedule_invoice_id",
                table: "amc_payment_schedule",
                column: "invoice_id");

            migrationBuilder.AddForeignKey(
                name: "FK_amc_payment_schedule_invoice_invoice_id",
                table: "amc_payment_schedule",
                column: "invoice_id",
                principalTable: "invoice",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_amc_payment_schedule_invoice_invoice_id",
                table: "amc_payment_schedule");

            migrationBuilder.DropIndex(
                name: "IX_amc_payment_schedule_invoice_id",
                table: "amc_payment_schedule");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "amc_payment_schedule");
        }
    }
}
