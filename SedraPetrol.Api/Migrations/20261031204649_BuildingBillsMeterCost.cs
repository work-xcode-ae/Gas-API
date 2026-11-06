using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class BuildingBillsMeterCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "building_id",
                table: "vendor_bill",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cost_location",
                table: "asset",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "purchase_bill_id",
                table: "asset",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_building_id",
                table: "vendor_bill",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_purchase_bill_id",
                table: "asset",
                column: "purchase_bill_id");

            migrationBuilder.AddForeignKey(
                name: "FK_asset_vendor_bill_purchase_bill_id",
                table: "asset",
                column: "purchase_bill_id",
                principalTable: "vendor_bill",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vendor_bill_building_building_id",
                table: "vendor_bill",
                column: "building_id",
                principalTable: "building",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_asset_vendor_bill_purchase_bill_id",
                table: "asset");

            migrationBuilder.DropForeignKey(
                name: "FK_vendor_bill_building_building_id",
                table: "vendor_bill");

            migrationBuilder.DropIndex(
                name: "IX_vendor_bill_building_id",
                table: "vendor_bill");

            migrationBuilder.DropIndex(
                name: "IX_asset_purchase_bill_id",
                table: "asset");

            migrationBuilder.DropColumn(
                name: "building_id",
                table: "vendor_bill");

            migrationBuilder.DropColumn(
                name: "cost_location",
                table: "asset");

            migrationBuilder.DropColumn(
                name: "purchase_bill_id",
                table: "asset");
        }
    }
}
