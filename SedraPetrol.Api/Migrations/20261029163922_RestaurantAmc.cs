using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class RestaurantAmc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "premises_id",
                table: "contract_amc_spec",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_amc_spec_premises",
                table: "contract_amc_spec",
                column: "premises_id");

            migrationBuilder.AddForeignKey(
                name: "FK_contract_amc_spec_premises_premises_id",
                table: "contract_amc_spec",
                column: "premises_id",
                principalTable: "premises",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contract_amc_spec_premises_premises_id",
                table: "contract_amc_spec");

            migrationBuilder.DropIndex(
                name: "ix_contract_amc_spec_premises",
                table: "contract_amc_spec");

            migrationBuilder.DropColumn(
                name: "premises_id",
                table: "contract_amc_spec");
        }
    }
}
