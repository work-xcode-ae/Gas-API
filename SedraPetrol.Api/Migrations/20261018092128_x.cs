using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class x : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_supply_contract_meter_meter_id",
                table: "supply_contract");

            migrationBuilder.DropIndex(
                name: "IX_supply_contract_meter_id",
                table: "supply_contract");

            migrationBuilder.DropColumn(
                name: "meter_id",
                table: "supply_contract");

            migrationBuilder.AddColumn<long>(
                name: "contract_id",
                table: "meter_reading",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "reading_type_id",
                table: "meter_reading",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "unit_id",
                table: "meter_reading",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_meter_reading_contract",
                table: "meter_reading",
                columns: new[] { "contract_id", "reading_date" });

            migrationBuilder.CreateIndex(
                name: "IX_meter_reading_reading_type_id",
                table: "meter_reading",
                column: "reading_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_meter_reading_unit",
                table: "meter_reading",
                columns: new[] { "unit_id", "reading_date" });

            migrationBuilder.AddForeignKey(
                name: "FK_meter_reading_lookup_value_reading_type_id",
                table: "meter_reading",
                column: "reading_type_id",
                principalTable: "lookup_value",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_meter_reading_supply_contract_contract_id",
                table: "meter_reading",
                column: "contract_id",
                principalTable: "supply_contract",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_meter_reading_unit_unit_id",
                table: "meter_reading",
                column: "unit_id",
                principalTable: "unit",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_meter_reading_lookup_value_reading_type_id",
                table: "meter_reading");

            migrationBuilder.DropForeignKey(
                name: "FK_meter_reading_supply_contract_contract_id",
                table: "meter_reading");

            migrationBuilder.DropForeignKey(
                name: "FK_meter_reading_unit_unit_id",
                table: "meter_reading");

            migrationBuilder.DropIndex(
                name: "ix_meter_reading_contract",
                table: "meter_reading");

            migrationBuilder.DropIndex(
                name: "IX_meter_reading_reading_type_id",
                table: "meter_reading");

            migrationBuilder.DropIndex(
                name: "ix_meter_reading_unit",
                table: "meter_reading");

            migrationBuilder.DropColumn(
                name: "contract_id",
                table: "meter_reading");

            migrationBuilder.DropColumn(
                name: "reading_type_id",
                table: "meter_reading");

            migrationBuilder.DropColumn(
                name: "unit_id",
                table: "meter_reading");

            migrationBuilder.AddColumn<long>(
                name: "meter_id",
                table: "supply_contract",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_supply_contract_meter_id",
                table: "supply_contract",
                column: "meter_id");

            migrationBuilder.AddForeignKey(
                name: "FK_supply_contract_meter_meter_id",
                table: "supply_contract",
                column: "meter_id",
                principalTable: "meter",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
