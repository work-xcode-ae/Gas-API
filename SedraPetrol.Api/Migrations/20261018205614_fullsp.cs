using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class fullsp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    job_title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    emirates_id = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    hire_date = table.Column<DateOnly>(type: "date", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_lookup_value_department_id",
                        column: x => x.department_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "voucher",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    voucher_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    voucher_type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    voucher_date = table.Column<DateOnly>(type: "date", nullable: false),
                    account_id = table.Column<long>(type: "bigint", nullable: false),
                    party_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    payment_method_id = table.Column<long>(type: "bigint", nullable: true),
                    reference_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    cheque_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cheque_date = table.Column<DateOnly>(type: "date", nullable: true),
                    bank_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    building_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    cancel_reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher", x => x.id);
                    table.ForeignKey(
                        name: "FK_voucher_account_account_id",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_lookup_value_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    asset_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    category_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    building_id = table.Column<long>(type: "bigint", nullable: true),
                    serial_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: true),
                    purchase_cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    assigned_employee_id = table.Column<long>(type: "bigint", nullable: true),
                    warranty_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_employee_assigned_employee_id",
                        column: x => x.assigned_employee_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_lookup_value_category_id",
                        column: x => x.category_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_asset_assigned_employee_id",
                table: "asset",
                column: "assigned_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_branch_id",
                table: "asset",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_building_id",
                table: "asset",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_category_id",
                table: "asset",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_status_id",
                table: "asset",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_branch_id",
                table: "employee",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_id",
                table: "employee",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_employee_user_id",
                table: "employee",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_account_id",
                table: "voucher",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_branch_id",
                table: "voucher",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_building_id",
                table: "voucher",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_journal_entry_id",
                table: "voucher",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_payment_method_id",
                table: "voucher",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ux_voucher_no",
                table: "voucher",
                column: "voucher_no",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset");

            migrationBuilder.DropTable(
                name: "voucher");

            migrationBuilder.DropTable(
                name: "employee");
        }
    }
}
