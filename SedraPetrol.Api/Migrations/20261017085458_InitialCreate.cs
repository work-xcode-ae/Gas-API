using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "country",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currency",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    decimal_places = table.Column<short>(type: "smallint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currency", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "gas_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    litre_to_m3 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gas_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lookup_type",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "emirate",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    country_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emirate", x => x.id);
                    table.ForeignKey(
                        name: "FK_emirate_country_country_id",
                        column: x => x.country_id,
                        principalTable: "country",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    trn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    logo_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    logo_dark_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    stamp_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    website = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    base_currency_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_currency_base_currency_id",
                        column: x => x.base_currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lookup_value",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    lookup_type_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lookup_value", x => x.id);
                    table.ForeignKey(
                        name: "FK_lookup_value_lookup_type_lookup_type_id",
                        column: x => x.lookup_type_id,
                        principalTable: "lookup_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lookup_value_lookup_value_parent_id",
                        column: x => x.parent_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permission", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permission_permission_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permission_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "city",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    emirate_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_city", x => x.id);
                    table.ForeignKey(
                        name: "FK_city_emirate_emirate_id",
                        column: x => x.emirate_id,
                        principalTable: "emirate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fiscal_period",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    period_no = table.Column<short>(type: "smallint", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_closed = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fiscal_period", x => x.id);
                    table.ForeignKey(
                        name: "FK_fiscal_period_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    parent_account_id = table.Column<long>(type: "bigint", nullable: true),
                    account_type_id = table.Column<long>(type: "bigint", nullable: true),
                    account_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    level = table.Column<short>(type: "smallint", nullable: true),
                    is_group = table.Column<bool>(type: "bit", nullable: false),
                    is_postable = table.Column<bool>(type: "bit", nullable: false),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_account_parent_account_id",
                        column: x => x.parent_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_lookup_value_account_type_id",
                        column: x => x.account_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_template",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    visit_type_id = table.Column<long>(type: "bigint", nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_template", x => x.id);
                    table.ForeignKey(
                        name: "FK_checklist_template_lookup_value_visit_type_id",
                        column: x => x.visit_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "branch",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    city_id = table.Column<long>(type: "bigint", nullable: true),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    logo_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_city_city_id",
                        column: x => x.city_id,
                        principalTable: "city",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_branch_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "checklist_template_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    template_id = table.Column<long>(type: "bigint", nullable: false),
                    item_text_en = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    item_text_ar = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_checklist_template_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_checklist_template_item_checklist_template_template_id",
                        column: x => x.template_id,
                        principalTable: "checklist_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_mapping",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    source_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    source_key_id = table.Column<long>(type: "bigint", nullable: true),
                    debit_account_id = table.Column<long>(type: "bigint", nullable: true),
                    credit_account_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_mapping", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_mapping_account_credit_account_id",
                        column: x => x.credit_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_mapping_account_debit_account_id",
                        column: x => x.debit_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_mapping_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_mapping_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_mapping_lookup_value_source_key_id",
                        column: x => x.source_key_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user", x => x.id);
                    table.ForeignKey(
                        name: "FK_app_user_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_app_user_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "building",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    building_type_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    city_id = table.Column<long>(type: "bigint", nullable: true),
                    zone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    street = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    po_box = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    total_floors = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_building", x => x.id);
                    table.ForeignKey(
                        name: "FK_building_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_building_city_city_id",
                        column: x => x.city_id,
                        principalTable: "city",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_building_lookup_value_building_type_id",
                        column: x => x.building_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_building_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "document",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    entity_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type_id = table.Column<long>(type: "bigint", nullable: true),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    file_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    mime_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_document_lookup_value_document_type_id",
                        column: x => x.document_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meter",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    meter_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    serial_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    meter_type_id = table.Column<long>(type: "bigint", nullable: true),
                    brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    sim_card_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    calibration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    install_date = table.Column<DateOnly>(type: "date", nullable: true),
                    initial_reading = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    location_note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meter", x => x.id);
                    table.ForeignKey(
                        name: "FK_meter_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_lookup_value_meter_type_id",
                        column: x => x.meter_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "number_sequence",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    prefix = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: true),
                    last_value = table.Column<int>(type: "int", nullable: true),
                    padding = table.Column<short>(type: "smallint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_number_sequence", x => x.id);
                    table.ForeignKey(
                        name: "FK_number_sequence_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_number_sequence_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "party",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    party_type_id = table.Column<long>(type: "bigint", nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    emirates_id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    nationality_id = table.Column<long>(type: "bigint", nullable: true),
                    trade_license_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    tax_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    city_id = table.Column<long>(type: "bigint", nullable: true),
                    po_box = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party", x => x.id);
                    table.ForeignKey(
                        name: "FK_party_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_party_city_city_id",
                        column: x => x.city_id,
                        principalTable: "city",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_party_lookup_value_nationality_id",
                        column: x => x.nationality_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_party_lookup_value_party_type_id",
                        column: x => x.party_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    tenant_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    emirates_id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    nationality_id = table.Column<long>(type: "bigint", nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    city_id = table.Column<long>(type: "bigint", nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    receivable_account_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_account_receivable_account_id",
                        column: x => x.receivable_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_city_city_id",
                        column: x => x.city_id,
                        principalTable: "city",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_lookup_value_nationality_id",
                        column: x => x.nationality_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    vendor_type_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    trade_license_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    trn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    payable_account_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor", x => x.id);
                    table.ForeignKey(
                        name: "FK_vendor_account_payable_account_id",
                        column: x => x.payable_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_lookup_value_vendor_type_id",
                        column: x => x.vendor_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_entry",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    journal_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fiscal_period_id = table.Column<long>(type: "bigint", nullable: false),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    exchange_rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    source_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    source_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    posted_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    posted_by = table.Column<long>(type: "bigint", nullable: true),
                    reversed_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_entry", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_entry_app_user_posted_by",
                        column: x => x.posted_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_entry_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_entry_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_entry_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_entry_fiscal_period_fiscal_period_id",
                        column: x => x.fiscal_period_id,
                        principalTable: "fiscal_period",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_entry_journal_entry_reversed_entry_id",
                        column: x => x.reversed_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_entry_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_branch",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_branch", x => new { x.user_id, x.branch_id });
                    table.ForeignKey(
                        name: "FK_user_branch_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_branch_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_role", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_role_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_role_role_role_id",
                        column: x => x.role_id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "activity_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    building_id = table.Column<long>(type: "bigint", nullable: true),
                    entity_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    entity_id = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    severity_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    old_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    new_values = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_activity_log_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activity_log_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activity_log_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activity_log_lookup_value_severity_id",
                        column: x => x.severity_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cd_certificate",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: false),
                    certificate_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    cert_type_id = table.Column<long>(type: "bigint", nullable: true),
                    license_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    issued_by = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    scope = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    inspector_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    inspection_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_certificate", x => x.id);
                    table.ForeignKey(
                        name: "FK_cd_certificate_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cd_certificate_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cd_certificate_lookup_value_cert_type_id",
                        column: x => x.cert_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cd_certificate_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tank",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: false),
                    tank_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    gas_type_id = table.Column<long>(type: "bigint", nullable: false),
                    capacity_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    current_level_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    serial_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    install_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_inspection_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_inspection_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tank", x => x.id);
                    table.ForeignKey(
                        name: "FK_tank_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tank_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tank_gas_type_gas_type_id",
                        column: x => x.gas_type_id,
                        principalTable: "gas_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tank_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    building_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    floor_no = table.Column<int>(type: "int", nullable: true),
                    unit_type_id = table.Column<long>(type: "bigint", nullable: true),
                    area_sqm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    bedrooms = table.Column<short>(type: "smallint", nullable: true),
                    bathrooms = table.Column<short>(type: "smallint", nullable: true),
                    rent_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    last_inspection_date = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_unit", x => x.id);
                    table.ForeignKey(
                        name: "FK_unit_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_unit_lookup_value_unit_type_id",
                        column: x => x.unit_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "building_owner",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    building_id = table.Column<long>(type: "bigint", nullable: false),
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    ownership_percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    is_primary = table.Column<bool>(type: "bit", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: true),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_building_owner", x => x.id);
                    table.ForeignKey(
                        name: "FK_building_owner_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_building_owner_party_party_id",
                        column: x => x.party_id,
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "party_bank_account",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    account_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    iban = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_bank_account", x => x.id);
                    table.ForeignKey(
                        name: "FK_party_bank_account_party_party_id",
                        column: x => x.party_id,
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "party_contact",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    contact_type_id = table.Column<long>(type: "bigint", nullable: false),
                    value = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_contact", x => x.id);
                    table.ForeignKey(
                        name: "FK_party_contact_lookup_value_contact_type_id",
                        column: x => x.contact_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_party_contact_party_party_id",
                        column: x => x.party_id,
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "amc_contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: false),
                    owner_party_id = table.Column<long>(type: "bigint", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    annual_value = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    visit_frequency_id = table.Column<long>(type: "bigint", nullable: true),
                    visits_total = table.Column<short>(type: "smallint", nullable: true),
                    response_time_hours = table.Column<short>(type: "smallint", nullable: true),
                    emergency_included = table.Column<bool>(type: "bit", nullable: false),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_amc_contract", x => x.id);
                    table.ForeignKey(
                        name: "FK_amc_contract_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_contract_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_contract_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_contract_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_contract_lookup_value_visit_frequency_id",
                        column: x => x.visit_frequency_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_contract_party_owner_party_id",
                        column: x => x.owner_party_id,
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_contract_vendor_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "driver",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendor_id = table.Column<long>(type: "bigint", nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    license_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    license_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    hazmat_permit_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver", x => x.id);
                    table.ForeignKey(
                        name: "FK_driver_vendor_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_contact",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_contact", x => x.id);
                    table.ForeignKey(
                        name: "FK_vendor_contact_vendor_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    receipt_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    payment_method_id = table.Column<long>(type: "bigint", nullable: true),
                    reference_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    received_by = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_app_user_received_by",
                        column: x => x.received_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_lookup_value_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bill",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    bill_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    bill_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    source_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    source_id = table.Column<long>(type: "bigint", nullable: true),
                    subtotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    vat_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    exchange_rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bill", x => x.id);
                    table.ForeignKey(
                        name: "FK_vendor_bill_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_vendor_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    payment_method_id = table.Column<long>(type: "bigint", nullable: true),
                    reference_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    paid_by = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_vendor_payment_app_user_paid_by",
                        column: x => x.paid_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_payment_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_payment_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_payment_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_payment_lookup_value_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_payment_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_payment_vendor_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cd_renewal",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    certificate_id = table.Column<long>(type: "bigint", nullable: false),
                    started_at = table.Column<DateOnly>(type: "date", nullable: true),
                    current_step = table.Column<short>(type: "smallint", nullable: true),
                    inspector_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    inspector_phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    inspector_dept = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    renewal_date = table.Column<DateOnly>(type: "date", nullable: true),
                    new_expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    new_certificate_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cd_renewal", x => x.id);
                    table.ForeignKey(
                        name: "FK_cd_renewal_cd_certificate_certificate_id",
                        column: x => x.certificate_id,
                        principalTable: "cd_certificate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cd_renewal_cd_certificate_new_certificate_id",
                        column: x => x.new_certificate_id,
                        principalTable: "cd_certificate",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cd_renewal_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refill_request",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    request_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tank_id = table.Column<long>(type: "bigint", nullable: false),
                    request_date = table.Column<DateOnly>(type: "date", nullable: false),
                    requested_by = table.Column<long>(type: "bigint", nullable: true),
                    requested_qty_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    level_at_request_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    priority_id = table.Column<long>(type: "bigint", nullable: true),
                    current_stage_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_by = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    approval_notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refill_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_refill_request_app_user_approved_by",
                        column: x => x.approved_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_request_app_user_requested_by",
                        column: x => x.requested_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_request_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_request_lookup_value_current_stage_id",
                        column: x => x.current_stage_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_request_lookup_value_priority_id",
                        column: x => x.priority_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_request_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_request_tank_tank_id",
                        column: x => x.tank_id,
                        principalTable: "tank",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meter_assignment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    meter_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meter_assignment", x => x.id);
                    table.ForeignKey(
                        name: "FK_meter_assignment_meter_meter_id",
                        column: x => x.meter_id,
                        principalTable: "meter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_assignment_unit_unit_id",
                        column: x => x.unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_request",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    request_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: true),
                    meter_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    priority_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    assigned_to = table.Column<long>(type: "bigint", nullable: true),
                    assigned_vendor_id = table.Column<long>(type: "bigint", nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_request", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_request_app_user_assigned_to",
                        column: x => x.assigned_to,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_lookup_value_priority_id",
                        column: x => x.priority_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_meter_meter_id",
                        column: x => x.meter_id,
                        principalTable: "meter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_unit_unit_id",
                        column: x => x.unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_vendor_assigned_vendor_id",
                        column: x => x.assigned_vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supply_contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
                    meter_id = table.Column<long>(type: "bigint", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    auto_renew = table.Column<bool>(type: "bit", nullable: false),
                    renewal_period_months = table.Column<short>(type: "smallint", nullable: true),
                    deposit_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    deposit_paid = table.Column<bool>(type: "bit", nullable: false),
                    deposit_date = table.Column<DateOnly>(type: "date", nullable: true),
                    terms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    terminated_at = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_contract", x => x.id);
                    table.ForeignKey(
                        name: "FK_supply_contract_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supply_contract_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supply_contract_meter_meter_id",
                        column: x => x.meter_id,
                        principalTable: "meter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supply_contract_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supply_contract_unit_unit_id",
                        column: x => x.unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "amc_scope_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    item_type_id = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amc_scope_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_amc_scope_item_amc_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "amc_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_scope_item_lookup_value_item_type_id",
                        column: x => x.item_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "amc_term",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    term_type_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_custom = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amc_term", x => x.id);
                    table.ForeignKey(
                        name: "FK_amc_term_amc_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "amc_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_term_lookup_value_term_type_id",
                        column: x => x.term_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendor_id = table.Column<long>(type: "bigint", nullable: true),
                    default_driver_id = table.Column<long>(type: "bigint", nullable: true),
                    plate_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    capacity_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    registration_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle", x => x.id);
                    table.ForeignKey(
                        name: "FK_vehicle_driver_default_driver_id",
                        column: x => x.default_driver_id,
                        principalTable: "driver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicle_vendor_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_visit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    visit_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    amc_contract_id = table.Column<long>(type: "bigint", nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: false),
                    visit_type_id = table.Column<long>(type: "bigint", nullable: true),
                    period_label = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                    time_slot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    visit_date = table.Column<DateOnly>(type: "date", nullable: true),
                    vendor_contact_id = table.Column<long>(type: "bigint", nullable: true),
                    technician_phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    report_summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_visit", x => x.id);
                    table.ForeignKey(
                        name: "FK_maintenance_visit_amc_contract_amc_contract_id",
                        column: x => x.amc_contract_id,
                        principalTable: "amc_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_visit_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_visit_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_visit_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_visit_lookup_value_visit_type_id",
                        column: x => x.visit_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_visit_vendor_contact_vendor_contact_id",
                        column: x => x.vendor_contact_id,
                        principalTable: "vendor_contact",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meter_topup",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    meter_id = table.Column<long>(type: "bigint", nullable: false),
                    topup_date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    quantity_m3 = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    payment_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meter_topup", x => x.id);
                    table.ForeignKey(
                        name: "FK_meter_topup_meter_meter_id",
                        column: x => x.meter_id,
                        principalTable: "meter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_topup_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "amc_payment_schedule",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    vendor_bill_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amc_payment_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_amc_payment_schedule_amc_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "amc_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_payment_schedule_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_amc_payment_schedule_vendor_bill_vendor_bill_id",
                        column: x => x.vendor_bill_id,
                        principalTable: "vendor_bill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_bill_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    bill_id = table.Column<long>(type: "bigint", nullable: false),
                    item_type_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    uom = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    vat_rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    expense_account_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_bill_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_vendor_bill_item_account_expense_account_id",
                        column: x => x.expense_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_item_lookup_value_item_type_id",
                        column: x => x.item_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_bill_item_vendor_bill_bill_id",
                        column: x => x.bill_id,
                        principalTable: "vendor_bill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_payment_allocation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    vendor_payment_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_bill_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_payment_allocation", x => x.id);
                    table.CheckConstraint("ck_vendor_payment_allocation_amount", "[amount] > 0");
                    table.ForeignKey(
                        name: "FK_vendor_payment_allocation_vendor_bill_vendor_bill_id",
                        column: x => x.vendor_bill_id,
                        principalTable: "vendor_bill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_payment_allocation_vendor_payment_vendor_payment_id",
                        column: x => x.vendor_payment_id,
                        principalTable: "vendor_payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refill_po",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refill_request_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    po_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    po_date = table.Column<DateOnly>(type: "date", nullable: false),
                    qty_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refill_po", x => x.id);
                    table.ForeignKey(
                        name: "FK_refill_po_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_po_refill_request_refill_request_id",
                        column: x => x.refill_request_id,
                        principalTable: "refill_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_po_vendor_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refill_purchase_invoice",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refill_request_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_bill_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refill_purchase_invoice", x => x.id);
                    table.ForeignKey(
                        name: "FK_refill_purchase_invoice_refill_request_refill_request_id",
                        column: x => x.refill_request_id,
                        principalTable: "refill_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_purchase_invoice_vendor_bill_vendor_bill_id",
                        column: x => x.vendor_bill_id,
                        principalTable: "vendor_bill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refill_schedule",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refill_request_id = table.Column<long>(type: "bigint", nullable: false),
                    delivery_date = table.Column<DateOnly>(type: "date", nullable: false),
                    time_slot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    civil_defense_confirmed = table.Column<bool>(type: "bit", nullable: false),
                    civil_defense_ref = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refill_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_refill_schedule_refill_request_refill_request_id",
                        column: x => x.refill_request_id,
                        principalTable: "refill_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refill_stage_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refill_request_id = table.Column<long>(type: "bigint", nullable: false),
                    from_stage_id = table.Column<long>(type: "bigint", nullable: true),
                    to_stage_id = table.Column<long>(type: "bigint", nullable: false),
                    changed_by = table.Column<long>(type: "bigint", nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refill_stage_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_refill_stage_history_app_user_changed_by",
                        column: x => x.changed_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_stage_history_lookup_value_from_stage_id",
                        column: x => x.from_stage_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_stage_history_lookup_value_to_stage_id",
                        column: x => x.to_stage_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_stage_history_refill_request_refill_request_id",
                        column: x => x.refill_request_id,
                        principalTable: "refill_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refill_supplier_payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refill_request_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_payment_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refill_supplier_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_refill_supplier_payment_refill_request_refill_request_id",
                        column: x => x.refill_request_id,
                        principalTable: "refill_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_supplier_payment_vendor_payment_vendor_payment_id",
                        column: x => x.vendor_payment_id,
                        principalTable: "vendor_payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_fee",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    fee_type_id = table.Column<long>(type: "bigint", nullable: false),
                    cycle_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: false),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_contract_fee", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_fee_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_fee_lookup_value_cycle_id",
                        column: x => x.cycle_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_fee_lookup_value_fee_type_id",
                        column: x => x.fee_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contract_fee_supply_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "supply_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: true),
                    contract_id = table.Column<long>(type: "bigint", nullable: true),
                    invoice_type_id = table.Column<long>(type: "bigint", nullable: true),
                    billing_period_start = table.Column<DateOnly>(type: "date", nullable: true),
                    billing_period_end = table.Column<DateOnly>(type: "date", nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    vat_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    total_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    exchange_rate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_lookup_value_invoice_type_id",
                        column: x => x.invoice_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_supply_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "supply_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_line",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    account_id = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    debit = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    credit = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    base_debit = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    base_credit = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    building_id = table.Column<long>(type: "bigint", nullable: true),
                    unit_id = table.Column<long>(type: "bigint", nullable: true),
                    tenant_id = table.Column<long>(type: "bigint", nullable: true),
                    vendor_id = table.Column<long>(type: "bigint", nullable: true),
                    contract_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_line", x => x.id);
                    table.CheckConstraint("ck_journal_line_dr_cr", "([debit] = 0 AND [credit] > 0) OR ([credit] = 0 AND [debit] > 0)");
                    table.ForeignKey(
                        name: "FK_journal_line_account_account_id",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_line_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_line_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_line_supply_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "supply_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_line_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_line_unit_unit_id",
                        column: x => x.unit_id,
                        principalTable: "unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journal_line_vendor_vendor_id",
                        column: x => x.vendor_id,
                        principalTable: "vendor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refill_collection",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refill_request_id = table.Column<long>(type: "bigint", nullable: false),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: true),
                    driver_id = table.Column<long>(type: "bigint", nullable: true),
                    collection_date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    meter_before = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    meter_after = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    collected_qty_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refill_collection", x => x.id);
                    table.ForeignKey(
                        name: "FK_refill_collection_driver_driver_id",
                        column: x => x.driver_id,
                        principalTable: "driver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_collection_refill_request_refill_request_id",
                        column: x => x.refill_request_id,
                        principalTable: "refill_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refill_collection_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tank_refill",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refill_request_id = table.Column<long>(type: "bigint", nullable: false),
                    tank_id = table.Column<long>(type: "bigint", nullable: false),
                    refill_datetime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    vehicle_id = table.Column<long>(type: "bigint", nullable: true),
                    driver_id = table.Column<long>(type: "bigint", nullable: true),
                    level_before_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    level_after_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    delivered_qty_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    safety_check_passed = table.Column<bool>(type: "bit", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tank_refill", x => x.id);
                    table.ForeignKey(
                        name: "FK_tank_refill_driver_driver_id",
                        column: x => x.driver_id,
                        principalTable: "driver",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tank_refill_refill_request_refill_request_id",
                        column: x => x.refill_request_id,
                        principalTable: "refill_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tank_refill_tank_tank_id",
                        column: x => x.tank_id,
                        principalTable: "tank",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tank_refill_vehicle_vehicle_id",
                        column: x => x.vehicle_id,
                        principalTable: "vehicle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visit_checklist_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    template_item_id = table.Column<long>(type: "bigint", nullable: true),
                    item_text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: true),
                    is_done = table.Column<bool>(type: "bit", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visit_checklist_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_visit_checklist_item_checklist_template_item_template_item_id",
                        column: x => x.template_item_id,
                        principalTable: "checklist_template_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visit_checklist_item_maintenance_visit_visit_id",
                        column: x => x.visit_id,
                        principalTable: "maintenance_visit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visit_finding",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    visit_id = table.Column<long>(type: "bigint", nullable: false),
                    severity_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    action_taken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    service_request_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visit_finding", x => x.id);
                    table.ForeignKey(
                        name: "FK_visit_finding_lookup_value_severity_id",
                        column: x => x.severity_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visit_finding_maintenance_visit_visit_id",
                        column: x => x.visit_id,
                        principalTable: "maintenance_visit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visit_finding_service_request_service_request_id",
                        column: x => x.service_request_id,
                        principalTable: "service_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meter_reading",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    meter_id = table.Column<long>(type: "bigint", nullable: false),
                    reading_date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    reading_value = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    consumption = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    method_id = table.Column<long>(type: "bigint", nullable: true),
                    read_by = table.Column<long>(type: "bigint", nullable: true),
                    invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meter_reading", x => x.id);
                    table.ForeignKey(
                        name: "FK_meter_reading_app_user_read_by",
                        column: x => x.read_by,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_reading_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_reading_lookup_value_method_id",
                        column: x => x.method_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_reading_meter_meter_id",
                        column: x => x.meter_id,
                        principalTable: "meter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_allocation", x => x.id);
                    table.CheckConstraint("ck_payment_allocation_amount", "[amount] > 0");
                    table.ForeignKey(
                        name: "FK_payment_allocation_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_allocation_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tank_level_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tank_id = table.Column<long>(type: "bigint", nullable: false),
                    event_date = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    event_type_id = table.Column<long>(type: "bigint", nullable: true),
                    level_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    tank_refill_id = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tank_level_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_tank_level_history_lookup_value_event_type_id",
                        column: x => x.event_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tank_level_history_tank_refill_tank_refill_id",
                        column: x => x.tank_refill_id,
                        principalTable: "tank_refill",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tank_level_history_tank_tank_id",
                        column: x => x.tank_id,
                        principalTable: "tank",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    item_type_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    uom = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(14,4)", precision: 14, scale: 4, nullable: true),
                    vat_rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    meter_reading_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_item_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_item_lookup_value_item_type_id",
                        column: x => x.item_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_item_meter_reading_meter_reading_id",
                        column: x => x.meter_reading_id,
                        principalTable: "meter_reading",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "meter_service",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    service_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    meter_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_id = table.Column<long>(type: "bigint", nullable: true),
                    service_type_id = table.Column<long>(type: "bigint", nullable: false),
                    issue_type_id = table.Column<long>(type: "bigint", nullable: true),
                    priority_id = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requested_date = table.Column<DateOnly>(type: "date", nullable: true),
                    completed_date = table.Column<DateOnly>(type: "date", nullable: true),
                    before_reading = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    after_reading = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    new_meter_id = table.Column<long>(type: "bigint", nullable: true),
                    safety_check_passed = table.Column<bool>(type: "bit", nullable: true),
                    tenant_notified = table.Column<bool>(type: "bit", nullable: true),
                    technician_id = table.Column<long>(type: "bigint", nullable: true),
                    fee_invoice_item_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meter_service", x => x.id);
                    table.ForeignKey(
                        name: "FK_meter_service_app_user_technician_id",
                        column: x => x.technician_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_invoice_item_fee_invoice_item_id",
                        column: x => x.fee_invoice_item_id,
                        principalTable: "invoice_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_lookup_value_issue_type_id",
                        column: x => x.issue_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_lookup_value_priority_id",
                        column: x => x.priority_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_lookup_value_service_type_id",
                        column: x => x.service_type_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_lookup_value_status_id",
                        column: x => x.status_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_meter_meter_id",
                        column: x => x.meter_id,
                        principalTable: "meter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_meter_new_meter_id",
                        column: x => x.new_meter_id,
                        principalTable: "meter",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_meter_service_supply_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "supply_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "account_index_6",
                table: "account",
                columns: new[] { "company_id", "account_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_account_type_id",
                table: "account",
                column: "account_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_currency_id",
                table: "account",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_parent_account_id",
                table: "account",
                column: "parent_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_mapping_branch_id",
                table: "account_mapping",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_mapping_company_id",
                table: "account_mapping",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_mapping_credit_account_id",
                table: "account_mapping",
                column: "credit_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_mapping_debit_account_id",
                table: "account_mapping",
                column: "debit_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_mapping_source_key_id",
                table: "account_mapping",
                column: "source_key_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_log_branch_id",
                table: "activity_log",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_log_building_id",
                table: "activity_log",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_log_severity_id",
                table: "activity_log",
                column: "severity_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_log_user_id",
                table: "activity_log",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_contract_branch_id",
                table: "amc_contract",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_contract_building_id",
                table: "amc_contract",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_contract_currency_id",
                table: "amc_contract",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_contract_owner_party_id",
                table: "amc_contract",
                column: "owner_party_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_contract_status_id",
                table: "amc_contract",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_contract_vendor_id",
                table: "amc_contract",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_contract_visit_frequency_id",
                table: "amc_contract",
                column: "visit_frequency_id");

            migrationBuilder.CreateIndex(
                name: "ux_amc_contract_contract_no",
                table: "amc_contract",
                column: "contract_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_amc_payment_schedule_contract_id",
                table: "amc_payment_schedule",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_payment_schedule_status_id",
                table: "amc_payment_schedule",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_payment_schedule_vendor_bill_id",
                table: "amc_payment_schedule",
                column: "vendor_bill_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_scope_item_contract_id",
                table: "amc_scope_item",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_scope_item_item_type_id",
                table: "amc_scope_item",
                column: "item_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_term_contract_id",
                table: "amc_term",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_amc_term_term_type_id",
                table: "amc_term",
                column: "term_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_branch_id",
                table: "app_user",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_company_id",
                table: "app_user",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ux_app_user_email",
                table: "app_user",
                column: "email",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_branch_city_id",
                table: "branch",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_company_id",
                table: "branch",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ux_branch_code",
                table: "branch",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_building_branch_id",
                table: "building",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_building_building_type_id",
                table: "building",
                column: "building_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_building_city_id",
                table: "building",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_building_status_id",
                table: "building",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ux_building_code",
                table: "building",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_building_owner_party_id",
                table: "building_owner",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ux_building_owner_primary",
                table: "building_owner",
                column: "building_id",
                unique: true,
                filter: "[is_primary] = 1 AND [is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_cd_certificate_branch_id",
                table: "cd_certificate",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_cd_certificate_building_id",
                table: "cd_certificate",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_cd_certificate_cert_type_id",
                table: "cd_certificate",
                column: "cert_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_cd_certificate_status_id",
                table: "cd_certificate",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ux_cd_certificate_certificate_no",
                table: "cd_certificate",
                column: "certificate_no",
                unique: true,
                filter: "[certificate_no] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cd_renewal_certificate_id",
                table: "cd_renewal",
                column: "certificate_id");

            migrationBuilder.CreateIndex(
                name: "IX_cd_renewal_new_certificate_id",
                table: "cd_renewal",
                column: "new_certificate_id");

            migrationBuilder.CreateIndex(
                name: "IX_cd_renewal_status_id",
                table: "cd_renewal",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_template_visit_type_id",
                table: "checklist_template",
                column: "visit_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_checklist_template_item_template_id",
                table: "checklist_template_item",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_city_emirate_id",
                table: "city",
                column: "emirate_id");

            migrationBuilder.CreateIndex(
                name: "ux_city_code",
                table: "city",
                column: "code",
                unique: true,
                filter: "[code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_company_base_currency_id",
                table: "company",
                column: "base_currency_id");

            migrationBuilder.CreateIndex(
                name: "ux_company_code",
                table: "company",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contract_fee_contract_id",
                table: "contract_fee",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_fee_currency_id",
                table: "contract_fee",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_fee_cycle_id",
                table: "contract_fee",
                column: "cycle_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_fee_fee_type_id",
                table: "contract_fee",
                column: "fee_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_country_code",
                table: "country",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_currency_code",
                table: "currency",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "document_index_7",
                table: "document",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_document_branch_id",
                table: "document",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_document_document_type_id",
                table: "document",
                column: "document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_driver_vendor_id",
                table: "driver",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_emirate_country_id",
                table: "emirate",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ux_emirate_code",
                table: "emirate",
                column: "code",
                unique: true,
                filter: "[code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "fiscal_period_index_5",
                table: "fiscal_period",
                columns: new[] { "company_id", "year", "period_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_gas_type_code",
                table: "gas_type",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_branch_id",
                table: "invoice",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_building_id",
                table: "invoice",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_contract_id",
                table: "invoice",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_currency_id",
                table: "invoice",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_invoice_type_id",
                table: "invoice",
                column: "invoice_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_journal_entry_id",
                table: "invoice",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_status_id",
                table: "invoice",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id",
                table: "invoice",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_invoice_invoice_no",
                table: "invoice",
                column: "invoice_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_invoice_id",
                table: "invoice_item",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_item_type_id",
                table: "invoice_item",
                column: "item_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_item_meter_reading_id",
                table: "invoice_item",
                column: "meter_reading_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_branch_id",
                table: "journal_entry",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_company_id",
                table: "journal_entry",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_currency_id",
                table: "journal_entry",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_fiscal_period_id",
                table: "journal_entry",
                column: "fiscal_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_posted_by",
                table: "journal_entry",
                column: "posted_by");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_reversed_entry_id",
                table: "journal_entry",
                column: "reversed_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entry_status_id",
                table: "journal_entry",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ux_journal_entry_journal_no",
                table: "journal_entry",
                column: "journal_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_line_account_id",
                table: "journal_line",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_line_building_id",
                table: "journal_line",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_line_contract_id",
                table: "journal_line",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_line_journal_entry_id",
                table: "journal_line",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_line_tenant_id",
                table: "journal_line",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_line_unit_id",
                table: "journal_line",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_journal_line_vendor_id",
                table: "journal_line",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ux_lookup_type_code",
                table: "lookup_type",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lookup_value_parent_id",
                table: "lookup_value",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "lookup_value_index_0",
                table: "lookup_value",
                columns: new[] { "lookup_type_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_visit_amc_contract_id",
                table: "maintenance_visit",
                column: "amc_contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_visit_branch_id",
                table: "maintenance_visit",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_visit_building_id",
                table: "maintenance_visit",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_visit_status_id",
                table: "maintenance_visit",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_visit_vendor_contact_id",
                table: "maintenance_visit",
                column: "vendor_contact_id");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_visit_visit_type_id",
                table: "maintenance_visit",
                column: "visit_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_maintenance_visit_visit_no",
                table: "maintenance_visit",
                column: "visit_no",
                unique: true,
                filter: "[visit_no] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_meter_branch_id",
                table: "meter",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_meter_type_id",
                table: "meter",
                column: "meter_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_status_id",
                table: "meter",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "ux_meter_meter_no",
                table: "meter",
                column: "meter_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_meter_serial_no",
                table: "meter",
                column: "serial_no",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_meter_assignment_open_meter",
                table: "meter_assignment",
                column: "meter_id",
                unique: true,
                filter: "[to_date] IS NULL AND [is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_meter_assignment_open_unit",
                table: "meter_assignment",
                column: "unit_id",
                unique: true,
                filter: "[to_date] IS NULL AND [is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_meter_reading_invoice_id",
                table: "meter_reading",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_reading_method_id",
                table: "meter_reading",
                column: "method_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_reading_read_by",
                table: "meter_reading",
                column: "read_by");

            migrationBuilder.CreateIndex(
                name: "meter_reading_index_2",
                table: "meter_reading",
                columns: new[] { "meter_id", "reading_date" });

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_branch_id",
                table: "meter_service",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_contract_id",
                table: "meter_service",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_fee_invoice_item_id",
                table: "meter_service",
                column: "fee_invoice_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_issue_type_id",
                table: "meter_service",
                column: "issue_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_meter_id",
                table: "meter_service",
                column: "meter_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_new_meter_id",
                table: "meter_service",
                column: "new_meter_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_priority_id",
                table: "meter_service",
                column: "priority_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_service_type_id",
                table: "meter_service",
                column: "service_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_status_id",
                table: "meter_service",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_service_technician_id",
                table: "meter_service",
                column: "technician_id");

            migrationBuilder.CreateIndex(
                name: "ux_meter_service_service_no",
                table: "meter_service",
                column: "service_no",
                unique: true,
                filter: "[service_no] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_meter_topup_meter_id",
                table: "meter_topup",
                column: "meter_id");

            migrationBuilder.CreateIndex(
                name: "IX_meter_topup_payment_id",
                table: "meter_topup",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_number_sequence_branch_id",
                table: "number_sequence",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "number_sequence_index_8",
                table: "number_sequence",
                columns: new[] { "company_id", "branch_id", "prefix", "year" },
                unique: true,
                filter: "[branch_id] IS NOT NULL AND [year] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_party_branch_id",
                table: "party",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_city_id",
                table: "party",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_nationality_id",
                table: "party",
                column: "nationality_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_party_type_id",
                table: "party",
                column: "party_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_bank_account_party_id",
                table: "party_bank_account",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_contact_contact_type_id",
                table: "party_contact",
                column: "contact_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_contact_party_id",
                table: "party_contact",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_branch_id",
                table: "payment",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_currency_id",
                table: "payment",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_journal_entry_id",
                table: "payment",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_payment_method_id",
                table: "payment",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_received_by",
                table: "payment",
                column: "received_by");

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_id",
                table: "payment",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_tenant_id",
                table: "payment",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_payment_receipt_no",
                table: "payment",
                column: "receipt_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocation_invoice_id",
                table: "payment_allocation",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocation_payment_id",
                table: "payment_allocation",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ux_permission_code",
                table: "permission",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refill_collection_driver_id",
                table: "refill_collection",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_collection_vehicle_id",
                table: "refill_collection",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ux_refill_collection_refill_request_id",
                table: "refill_collection",
                column: "refill_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refill_po_currency_id",
                table: "refill_po",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_po_vendor_id",
                table: "refill_po",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ux_refill_po_po_number",
                table: "refill_po",
                column: "po_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_refill_po_refill_request_id",
                table: "refill_po",
                column: "refill_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refill_purchase_invoice_vendor_bill_id",
                table: "refill_purchase_invoice",
                column: "vendor_bill_id");

            migrationBuilder.CreateIndex(
                name: "ux_refill_purchase_invoice_refill_request_id",
                table: "refill_purchase_invoice",
                column: "refill_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refill_request_approved_by",
                table: "refill_request",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_refill_request_branch_id",
                table: "refill_request",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_request_current_stage_id",
                table: "refill_request",
                column: "current_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_request_priority_id",
                table: "refill_request",
                column: "priority_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_request_requested_by",
                table: "refill_request",
                column: "requested_by");

            migrationBuilder.CreateIndex(
                name: "IX_refill_request_status_id",
                table: "refill_request",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_request_tank_id",
                table: "refill_request",
                column: "tank_id");

            migrationBuilder.CreateIndex(
                name: "ux_refill_request_request_no",
                table: "refill_request",
                column: "request_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_refill_schedule_refill_request_id",
                table: "refill_schedule",
                column: "refill_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refill_stage_history_changed_by",
                table: "refill_stage_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "IX_refill_stage_history_from_stage_id",
                table: "refill_stage_history",
                column: "from_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_stage_history_refill_request_id",
                table: "refill_stage_history",
                column: "refill_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_stage_history_to_stage_id",
                table: "refill_stage_history",
                column: "to_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_refill_supplier_payment_vendor_payment_id",
                table: "refill_supplier_payment",
                column: "vendor_payment_id");

            migrationBuilder.CreateIndex(
                name: "ux_refill_supplier_payment_refill_request_id",
                table: "refill_supplier_payment",
                column: "refill_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_role_code",
                table: "role",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_permission_id",
                table: "role_permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_assigned_to",
                table: "service_request",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_assigned_vendor_id",
                table: "service_request",
                column: "assigned_vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_branch_id",
                table: "service_request",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_building_id",
                table: "service_request",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_meter_id",
                table: "service_request",
                column: "meter_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_priority_id",
                table: "service_request",
                column: "priority_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_status_id",
                table: "service_request",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_tenant_id",
                table: "service_request",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_request_unit_id",
                table: "service_request",
                column: "unit_id");

            migrationBuilder.CreateIndex(
                name: "ux_service_request_request_no",
                table: "service_request",
                column: "request_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supply_contract_branch_id",
                table: "supply_contract",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_supply_contract_meter_id",
                table: "supply_contract",
                column: "meter_id");

            migrationBuilder.CreateIndex(
                name: "IX_supply_contract_status_id",
                table: "supply_contract",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_supply_contract_tenant_id",
                table: "supply_contract",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_supply_contract_active_unit",
                table: "supply_contract",
                column: "unit_id",
                unique: true,
                filter: "[is_active] = 1 AND [is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_supply_contract_contract_no",
                table: "supply_contract",
                column: "contract_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tank_branch_id",
                table: "tank",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_tank_gas_type_id",
                table: "tank",
                column: "gas_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_tank_status_id",
                table: "tank",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "tank_index_3",
                table: "tank",
                columns: new[] { "building_id", "tank_no" },
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_tank_level_history_event_type_id",
                table: "tank_level_history",
                column: "event_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_tank_level_history_tank_id",
                table: "tank_level_history",
                column: "tank_id");

            migrationBuilder.CreateIndex(
                name: "IX_tank_level_history_tank_refill_id",
                table: "tank_level_history",
                column: "tank_refill_id");

            migrationBuilder.CreateIndex(
                name: "IX_tank_refill_driver_id",
                table: "tank_refill",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "IX_tank_refill_tank_id",
                table: "tank_refill",
                column: "tank_id");

            migrationBuilder.CreateIndex(
                name: "IX_tank_refill_vehicle_id",
                table: "tank_refill",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "ux_tank_refill_refill_request_id",
                table: "tank_refill",
                column: "refill_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_branch_id",
                table: "tenant",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_city_id",
                table: "tenant",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_nationality_id",
                table: "tenant",
                column: "nationality_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_receivable_account_id",
                table: "tenant",
                column: "receivable_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_tenant_emirates_id",
                table: "tenant",
                column: "emirates_id",
                unique: true,
                filter: "[emirates_id] IS NOT NULL AND [is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "ux_tenant_tenant_code",
                table: "tenant",
                column: "tenant_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unit_status_id",
                table: "unit",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_unit_unit_type_id",
                table: "unit",
                column: "unit_type_id");

            migrationBuilder.CreateIndex(
                name: "unit_index_1",
                table: "unit",
                columns: new[] { "building_id", "unit_no" },
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_user_branch_branch_id",
                table: "user_branch",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_role_id",
                table: "user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_default_driver_id",
                table: "vehicle",
                column: "default_driver_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_vendor_id",
                table: "vehicle",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ux_vehicle_plate_no",
                table: "vehicle",
                column: "plate_no",
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_branch_id",
                table: "vendor",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payable_account_id",
                table: "vendor",
                column: "payable_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_vendor_type_id",
                table: "vendor",
                column: "vendor_type_id");

            migrationBuilder.CreateIndex(
                name: "ux_vendor_code",
                table: "vendor",
                column: "code",
                unique: true,
                filter: "[code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_branch_id",
                table: "vendor_bill",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_currency_id",
                table: "vendor_bill",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_journal_entry_id",
                table: "vendor_bill",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_status_id",
                table: "vendor_bill",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "vendor_bill_index_4",
                table: "vendor_bill",
                columns: new[] { "vendor_id", "bill_no" },
                unique: true,
                filter: "[is_deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_item_bill_id",
                table: "vendor_bill_item",
                column: "bill_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_item_expense_account_id",
                table: "vendor_bill_item",
                column: "expense_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_bill_item_item_type_id",
                table: "vendor_bill_item",
                column: "item_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_contact_vendor_id",
                table: "vendor_contact",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_branch_id",
                table: "vendor_payment",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_currency_id",
                table: "vendor_payment",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_journal_entry_id",
                table: "vendor_payment",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_paid_by",
                table: "vendor_payment",
                column: "paid_by");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_payment_method_id",
                table: "vendor_payment",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_status_id",
                table: "vendor_payment",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_vendor_id",
                table: "vendor_payment",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "ux_vendor_payment_payment_no",
                table: "vendor_payment",
                column: "payment_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_allocation_vendor_bill_id",
                table: "vendor_payment_allocation",
                column: "vendor_bill_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendor_payment_allocation_vendor_payment_id",
                table: "vendor_payment_allocation",
                column: "vendor_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_visit_checklist_item_template_item_id",
                table: "visit_checklist_item",
                column: "template_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_visit_checklist_item_visit_id",
                table: "visit_checklist_item",
                column: "visit_id");

            migrationBuilder.CreateIndex(
                name: "IX_visit_finding_service_request_id",
                table: "visit_finding",
                column: "service_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_visit_finding_severity_id",
                table: "visit_finding",
                column: "severity_id");

            migrationBuilder.CreateIndex(
                name: "IX_visit_finding_visit_id",
                table: "visit_finding",
                column: "visit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_mapping");

            migrationBuilder.DropTable(
                name: "activity_log");

            migrationBuilder.DropTable(
                name: "amc_payment_schedule");

            migrationBuilder.DropTable(
                name: "amc_scope_item");

            migrationBuilder.DropTable(
                name: "amc_term");

            migrationBuilder.DropTable(
                name: "building_owner");

            migrationBuilder.DropTable(
                name: "cd_renewal");

            migrationBuilder.DropTable(
                name: "contract_fee");

            migrationBuilder.DropTable(
                name: "document");

            migrationBuilder.DropTable(
                name: "journal_line");

            migrationBuilder.DropTable(
                name: "meter_assignment");

            migrationBuilder.DropTable(
                name: "meter_service");

            migrationBuilder.DropTable(
                name: "meter_topup");

            migrationBuilder.DropTable(
                name: "number_sequence");

            migrationBuilder.DropTable(
                name: "party_bank_account");

            migrationBuilder.DropTable(
                name: "party_contact");

            migrationBuilder.DropTable(
                name: "payment_allocation");

            migrationBuilder.DropTable(
                name: "refill_collection");

            migrationBuilder.DropTable(
                name: "refill_po");

            migrationBuilder.DropTable(
                name: "refill_purchase_invoice");

            migrationBuilder.DropTable(
                name: "refill_schedule");

            migrationBuilder.DropTable(
                name: "refill_stage_history");

            migrationBuilder.DropTable(
                name: "refill_supplier_payment");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "tank_level_history");

            migrationBuilder.DropTable(
                name: "user_branch");

            migrationBuilder.DropTable(
                name: "user_role");

            migrationBuilder.DropTable(
                name: "vendor_bill_item");

            migrationBuilder.DropTable(
                name: "vendor_payment_allocation");

            migrationBuilder.DropTable(
                name: "visit_checklist_item");

            migrationBuilder.DropTable(
                name: "visit_finding");

            migrationBuilder.DropTable(
                name: "cd_certificate");

            migrationBuilder.DropTable(
                name: "invoice_item");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "permission");

            migrationBuilder.DropTable(
                name: "tank_refill");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "vendor_bill");

            migrationBuilder.DropTable(
                name: "vendor_payment");

            migrationBuilder.DropTable(
                name: "checklist_template_item");

            migrationBuilder.DropTable(
                name: "maintenance_visit");

            migrationBuilder.DropTable(
                name: "service_request");

            migrationBuilder.DropTable(
                name: "meter_reading");

            migrationBuilder.DropTable(
                name: "refill_request");

            migrationBuilder.DropTable(
                name: "vehicle");

            migrationBuilder.DropTable(
                name: "checklist_template");

            migrationBuilder.DropTable(
                name: "amc_contract");

            migrationBuilder.DropTable(
                name: "vendor_contact");

            migrationBuilder.DropTable(
                name: "invoice");

            migrationBuilder.DropTable(
                name: "tank");

            migrationBuilder.DropTable(
                name: "driver");

            migrationBuilder.DropTable(
                name: "party");

            migrationBuilder.DropTable(
                name: "journal_entry");

            migrationBuilder.DropTable(
                name: "supply_contract");

            migrationBuilder.DropTable(
                name: "gas_type");

            migrationBuilder.DropTable(
                name: "vendor");

            migrationBuilder.DropTable(
                name: "app_user");

            migrationBuilder.DropTable(
                name: "fiscal_period");

            migrationBuilder.DropTable(
                name: "meter");

            migrationBuilder.DropTable(
                name: "tenant");

            migrationBuilder.DropTable(
                name: "unit");

            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "building");

            migrationBuilder.DropTable(
                name: "branch");

            migrationBuilder.DropTable(
                name: "lookup_value");

            migrationBuilder.DropTable(
                name: "city");

            migrationBuilder.DropTable(
                name: "company");

            migrationBuilder.DropTable(
                name: "lookup_type");

            migrationBuilder.DropTable(
                name: "emirate");

            migrationBuilder.DropTable(
                name: "currency");

            migrationBuilder.DropTable(
                name: "country");
        }
    }
}
