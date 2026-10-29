using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class PremisesDeleteCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_premises_refill_invoice_refill_request_refill_request_id",
                table: "premises_refill_invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_premises_tank_asset_tank_tank_id",
                table: "premises_tank");

            migrationBuilder.AddForeignKey(
                name: "FK_premises_refill_invoice_refill_request_refill_request_id",
                table: "premises_refill_invoice",
                column: "refill_request_id",
                principalTable: "refill_request",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premises_tank_asset_tank_tank_id",
                table: "premises_tank",
                column: "tank_id",
                principalTable: "asset_tank",
                principalColumn: "asset_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_premises_refill_invoice_refill_request_refill_request_id",
                table: "premises_refill_invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_premises_tank_asset_tank_tank_id",
                table: "premises_tank");

            migrationBuilder.AddForeignKey(
                name: "FK_premises_refill_invoice_refill_request_refill_request_id",
                table: "premises_refill_invoice",
                column: "refill_request_id",
                principalTable: "refill_request",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_premises_tank_asset_tank_tank_id",
                table: "premises_tank",
                column: "tank_id",
                principalTable: "asset_tank",
                principalColumn: "asset_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
