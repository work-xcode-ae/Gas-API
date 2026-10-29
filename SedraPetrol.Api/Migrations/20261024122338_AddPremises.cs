using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPremises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "premises",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    premises_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_id = table.Column<long>(type: "bigint", nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    owner_party_id = table.Column<long>(type: "bigint", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premises", x => x.id);
                    table.ForeignKey(
                        name: "FK_premises_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_premises_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_premises_party_owner_party_id",
                        column: x => x.owner_party_id,
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_premises_unit_unit_id",
                        column: x => x.unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "premises_restaurant_spec",
                columns: table => new
                {
                    premises_id = table.Column<long>(type: "bigint", nullable: false),
                    trade_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    trade_license_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    trade_license_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    seating_capacity = table.Column<int>(type: "int", nullable: true),
                    burners_count = table.Column<int>(type: "int", nullable: true),
                    kitchen_type_id = table.Column<long>(type: "bigint", nullable: true),
                    cuisine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    operating_hours = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premises_restaurant_spec", x => x.premises_id);
                    table.ForeignKey(
                        name: "FK_premises_restaurant_spec_lookup_value_kitchen_type_id",
                        column: x => x.kitchen_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_premises_restaurant_spec_premises_premises_id",
                        column: x => x.premises_id,
                        principalTable: "premises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "premises_signatory",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    premises_id = table.Column<long>(type: "bigint", nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    emirates_id = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    can_sign_contracts = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premises_signatory", x => x.id);
                    table.ForeignKey(
                        name: "FK_premises_signatory_premises_premises_id",
                        column: x => x.premises_id,
                        principalTable: "premises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "premises_tank",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    premises_id = table.Column<long>(type: "bigint", nullable: false),
                    tank_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premises_tank", x => x.id);
                    table.ForeignKey(
                        name: "FK_premises_tank_asset_tank_tank_id",
                        column: x => x.tank_id,
                        principalTable: "asset_tank",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_premises_tank_premises_premises_id",
                        column: x => x.premises_id,
                        principalTable: "premises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "premises_villa_spec",
                columns: table => new
                {
                    premises_id = table.Column<long>(type: "bigint", nullable: false),
                    community = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    plot_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    villa_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    makani_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    bedrooms = table.Column<short>(type: "smallint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premises_villa_spec", x => x.premises_id);
                    table.ForeignKey(
                        name: "FK_premises_villa_spec_premises_premises_id",
                        column: x => x.premises_id,
                        principalTable: "premises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_delivery_spec",
                columns: table => new
                {
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    premises_id = table.Column<long>(type: "bigint", nullable: false),
                    customer_party_id = table.Column<long>(type: "bigint", nullable: false),
                    price_per_litre = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    tax_rate_id = table.Column<long>(type: "bigint", nullable: true),
                    payment_terms_days = table.Column<short>(type: "smallint", nullable: true),
                    signatory_id = table.Column<long>(type: "bigint", nullable: true),
                    signed_by_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    signed_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_delivery_spec", x => x.contract_id);
                    table.ForeignKey(
                        name: "FK_contract_delivery_spec_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contract_delivery_spec_party_customer_party_id",
                        column: x => x.customer_party_id,
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_delivery_spec_premises_premises_id",
                        column: x => x.premises_id,
                        principalTable: "premises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_delivery_spec_premises_signatory_signatory_id",
                        column: x => x.signatory_id,
                        principalTable: "premises_signatory",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_delivery_spec_tax_rate_tax_rate_id",
                        column: x => x.tax_rate_id,
                        principalTable: "tax_rate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "premises_refill_invoice",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    premises_id = table.Column<long>(type: "bigint", nullable: false),
                    refill_request_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    price_per_litre = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premises_refill_invoice", x => x.id);
                    table.ForeignKey(
                        name: "FK_premises_refill_invoice_contract_delivery_spec_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract_delivery_spec",
                        principalColumn: "contract_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_premises_refill_invoice_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_premises_refill_invoice_premises_premises_id",
                        column: x => x.premises_id,
                        principalTable: "premises",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_premises_refill_invoice_refill_request_refill_request_id",
                        column: x => x.refill_request_id,
                        principalTable: "refill_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_delivery_spec_customer",
                table: "contract_delivery_spec",
                column: "customer_party_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_delivery_spec_premises",
                table: "contract_delivery_spec",
                column: "premises_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_delivery_spec_signatory_id",
                table: "contract_delivery_spec",
                column: "signatory_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_delivery_spec_tax_rate_id",
                table: "contract_delivery_spec",
                column: "tax_rate_id");

            migrationBuilder.CreateIndex(
                name: "IX_premises_branch_id",
                table: "premises",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_premises_building_type",
                table: "premises",
                columns: new[] { "building_id", "premises_type" });

            migrationBuilder.CreateIndex(
                name: "ix_premises_owner",
                table: "premises",
                column: "owner_party_id");

            migrationBuilder.CreateIndex(
                name: "ux_premises_code",
                table: "premises",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_premises_unit",
                table: "premises",
                column: "unit_id",
                unique: true,
                filter: "[unit_id] IS NOT NULL AND [is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_premises_refill_invoice_contract_id",
                table: "premises_refill_invoice",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_premises_refill_invoice_invoice",
                table: "premises_refill_invoice",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_premises_refill_invoice_premises",
                table: "premises_refill_invoice",
                column: "premises_id");

            migrationBuilder.CreateIndex(
                name: "ux_premises_refill_invoice_refill",
                table: "premises_refill_invoice",
                column: "refill_request_id",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_premises_restaurant_spec_kitchen_type_id",
                table: "premises_restaurant_spec",
                column: "kitchen_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_premises_signatory_premises",
                table: "premises_signatory",
                column: "premises_id");

            migrationBuilder.CreateIndex(
                name: "ix_premises_tank_premises",
                table: "premises_tank",
                column: "premises_id");

            migrationBuilder.CreateIndex(
                name: "ux_premises_tank_tank",
                table: "premises_tank",
                column: "tank_id",
                unique: true,
                filter: "[is_deleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "premises_refill_invoice");

            migrationBuilder.DropTable(
                name: "premises_restaurant_spec");

            migrationBuilder.DropTable(
                name: "premises_tank");

            migrationBuilder.DropTable(
                name: "premises_villa_spec");

            migrationBuilder.DropTable(
                name: "contract_delivery_spec");

            migrationBuilder.DropTable(
                name: "premises_signatory");

            migrationBuilder.DropTable(
                name: "premises");
        }
    }
}
