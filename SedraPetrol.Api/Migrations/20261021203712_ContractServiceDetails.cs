using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class ContractServiceDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "bill_by_email",
                table: "contract_supply_spec",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "bill_by_post",
                table: "contract_supply_spec",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "billing_cycle_months",
                table: "contract_supply_spec",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "service_type_id",
                table: "contract_supply_spec",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "usage_type_id",
                table: "contract_supply_spec",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_contract_supply_spec_service_type_id",
                table: "contract_supply_spec",
                column: "service_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_supply_spec_usage_type_id",
                table: "contract_supply_spec",
                column: "usage_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_contract_supply_spec_lookup_value_service_type_id",
                table: "contract_supply_spec",
                column: "service_type_id",
                principalTable: "lookup_value",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_contract_supply_spec_lookup_value_usage_type_id",
                table: "contract_supply_spec",
                column: "usage_type_id",
                principalTable: "lookup_value",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contract_supply_spec_lookup_value_service_type_id",
                table: "contract_supply_spec");

            migrationBuilder.DropForeignKey(
                name: "FK_contract_supply_spec_lookup_value_usage_type_id",
                table: "contract_supply_spec");

            migrationBuilder.DropIndex(
                name: "IX_contract_supply_spec_service_type_id",
                table: "contract_supply_spec");

            migrationBuilder.DropIndex(
                name: "IX_contract_supply_spec_usage_type_id",
                table: "contract_supply_spec");

            migrationBuilder.DropColumn(
                name: "bill_by_email",
                table: "contract_supply_spec");

            migrationBuilder.DropColumn(
                name: "bill_by_post",
                table: "contract_supply_spec");

            migrationBuilder.DropColumn(
                name: "billing_cycle_months",
                table: "contract_supply_spec");

            migrationBuilder.DropColumn(
                name: "service_type_id",
                table: "contract_supply_spec");

            migrationBuilder.DropColumn(
                name: "usage_type_id",
                table: "contract_supply_spec");
        }
    }
}
