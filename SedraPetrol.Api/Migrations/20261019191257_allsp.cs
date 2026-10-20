using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <inheritdoc />
    public partial class allsp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "tax_rate_id",
                table: "vendor_bill_item",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "tax_rate_id",
                table: "invoice_item",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "duration_years",
                table: "cd_renewal",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "invoiced_gov_fees",
                table: "cd_renewal",
                type: "decimal(14,2)",
                precision: 14,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "owner_invoice_id",
                table: "cd_renewal",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "accounting_setting",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    journal_approval_threshold = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    opening_equity_account_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounting_setting", x => x.id);
                    table.ForeignKey(
                        name: "FK_accounting_setting_account_opening_equity_account_id",
                        column: x => x.opening_equity_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accounting_setting_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_trail_link",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    audit_trail_id = table.Column<long>(type: "bigint", nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_self = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_trail_link", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_trail_link_audit_trail_audit_trail_id",
                        column: x => x.audit_trail_id,
                        principalTable: "audit_trail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bank_account",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    account_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    bank_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    account_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    iban = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    gl_account_id = table.Column<long>(type: "bigint", nullable: false),
                    opening_balance = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    opening_date = table.Column<DateOnly>(type: "date", nullable: true),
                    opening_journal_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_bank_account", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_account_account_gl_account_id",
                        column: x => x.gl_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_account_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_account_currency_currency_id",
                        column: x => x.currency_id,
                        principalTable: "currency",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_account_journal_entry_opening_journal_id",
                        column: x => x.opening_journal_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cashflow_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    direction = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    counterparty = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    building_id = table.Column<long>(type: "bigint", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    recurrence = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    confidence = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashflow_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_cashflow_item_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cashflow_item_building_building_id",
                        column: x => x.building_id,
                        principalTable: "building",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_note",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    credit_note_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: true),
                    contract_id = table.Column<long>(type: "bigint", nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    subtotal = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    cancel_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_note", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_note_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_note_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_note_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_note_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_refund",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    refund_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    refund_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    payment_method_id = table.Column<long>(type: "bigint", nullable: true),
                    reference_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    cancel_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_refund", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_refund_branch_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branch",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_refund_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_refund_lookup_value_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "lookup_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_refund_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_installment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    seq_no = table.Column<short>(type: "smallint", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_installment", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_installment_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journal_approval",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    journal_entry_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    threshold = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    requested_by = table.Column<long>(type: "bigint", nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    decided_by = table.Column<long>(type: "bigint", nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journal_approval", x => x.id);
                    table.ForeignKey(
                        name: "FK_journal_approval_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    family_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    absolute_expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    revoked_reason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    replaced_by_id = table.Column<long>(type: "bigint", nullable: true),
                    created_ip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    session_started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_token_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tax_rate",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    name_en = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    tax_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    applies_to = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_default = table.Column<bool>(type: "bit", nullable: false),
                    output_account_id = table.Column<long>(type: "bigint", nullable: true),
                    input_account_id = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_tax_rate", x => x.id);
                    table.ForeignKey(
                        name: "FK_tax_rate_account_input_account_id",
                        column: x => x.input_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tax_rate_account_output_account_id",
                        column: x => x.output_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bank_reconciliation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    bank_account_id = table.Column<long>(type: "bigint", nullable: false),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_to = table.Column<DateOnly>(type: "date", nullable: false),
                    statement_balance = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    book_balance = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    outstanding_book = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    unrecorded_bank = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    difference = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    completed_by = table.Column<long>(type: "bigint", nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_reconciliation", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_reconciliation_bank_account_bank_account_id",
                        column: x => x.bank_account_id,
                        principalTable: "bank_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bank_transfer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    transfer_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    transfer_date = table.Column<DateOnly>(type: "date", nullable: false),
                    from_account_id = table.Column<long>(type: "bigint", nullable: false),
                    to_account_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    fees = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    fees_account_id = table.Column<long>(type: "bigint", nullable: true),
                    reference_no = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_bank_transfer", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_transfer_account_fees_account_id",
                        column: x => x.fees_account_id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_transfer_bank_account_from_account_id",
                        column: x => x.from_account_id,
                        principalTable: "bank_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_transfer_bank_account_to_account_id",
                        column: x => x.to_account_id,
                        principalTable: "bank_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_transfer_journal_entry_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entry",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_note_allocation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    credit_note_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_credit_note_allocation", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_note_allocation_credit_note_credit_note_id",
                        column: x => x.credit_note_id,
                        principalTable: "credit_note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_note_allocation_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_note_line",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    credit_note_id = table.Column<long>(type: "bigint", nullable: false),
                    invoice_item_id = table.Column<long>(type: "bigint", nullable: true),
                    item_type_id = table.Column<long>(type: "bigint", nullable: true),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    vat_rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    vat_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_note_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_credit_note_line_credit_note_credit_note_id",
                        column: x => x.credit_note_id,
                        principalTable: "credit_note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_note_line_invoice_item_invoice_item_id",
                        column: x => x.invoice_item_id,
                        principalTable: "invoice_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_refund_source",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    refund_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: true),
                    credit_note_id = table.Column<long>(type: "bigint", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_refund_source", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_refund_source_credit_note_credit_note_id",
                        column: x => x.credit_note_id,
                        principalTable: "credit_note",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_refund_source_customer_refund_refund_id",
                        column: x => x.refund_id,
                        principalTable: "customer_refund",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_refund_source_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bank_statement_line",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    bank_account_id = table.Column<long>(type: "bigint", nullable: false),
                    line_date = table.Column<DateOnly>(type: "date", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    balance = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    journal_line_id = table.Column<long>(type: "bigint", nullable: true),
                    matched_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    matched_by = table.Column<long>(type: "bigint", nullable: true),
                    import_batch = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reconciliation_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_statement_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_statement_line_bank_account_bank_account_id",
                        column: x => x.bank_account_id,
                        principalTable: "bank_account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_statement_line_bank_reconciliation_reconciliation_id",
                        column: x => x.reconciliation_id,
                        principalTable: "bank_reconciliation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_bank_statement_line_journal_line_journal_line_id",
                        column: x => x.journal_line_id,
                        principalTable: "journal_line",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounting_setting_opening_equity_account_id",
                table: "accounting_setting",
                column: "opening_equity_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_accounting_setting_company",
                table: "accounting_setting",
                column: "company_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_trail_link_audit_trail_id",
                table: "audit_trail_link",
                column: "audit_trail_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_trail_link_entity",
                table: "audit_trail_link",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_branch_id",
                table: "bank_account",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_currency_id",
                table: "bank_account",
                column: "currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_account_opening_journal_id",
                table: "bank_account",
                column: "opening_journal_id");

            migrationBuilder.CreateIndex(
                name: "ux_bank_account_gl",
                table: "bank_account",
                column: "gl_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bank_reconciliation_account_period",
                table: "bank_reconciliation",
                columns: new[] { "bank_account_id", "period_to" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_line_account_date",
                table: "bank_statement_line",
                columns: new[] { "bank_account_id", "line_date" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_statement_line_journal_line",
                table: "bank_statement_line",
                column: "journal_line_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_statement_line_reconciliation_id",
                table: "bank_statement_line",
                column: "reconciliation_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_transfer_fees_account_id",
                table: "bank_transfer",
                column: "fees_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_transfer_from_account_id",
                table: "bank_transfer",
                column: "from_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_transfer_journal_entry_id",
                table: "bank_transfer",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_bank_transfer_to_account_id",
                table: "bank_transfer",
                column: "to_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_bank_transfer_no",
                table: "bank_transfer",
                column: "transfer_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cashflow_item_branch_id",
                table: "cashflow_item",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_cashflow_item_building_id",
                table: "cashflow_item",
                column: "building_id");

            migrationBuilder.CreateIndex(
                name: "ix_cashflow_item_start",
                table: "cashflow_item",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_branch_id",
                table: "credit_note",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_note_invoice",
                table: "credit_note",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_journal_entry_id",
                table: "credit_note",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_note_tenant",
                table: "credit_note",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_credit_note_no",
                table: "credit_note",
                column: "credit_note_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_allocation_credit_note_id",
                table: "credit_note_allocation",
                column: "credit_note_id");

            migrationBuilder.CreateIndex(
                name: "ix_credit_note_allocation_invoice",
                table: "credit_note_allocation",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_line_credit_note_id",
                table: "credit_note_line",
                column: "credit_note_id");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_line_invoice_item_id",
                table: "credit_note_line",
                column: "invoice_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refund_branch_id",
                table: "customer_refund",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refund_journal_entry_id",
                table: "customer_refund",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refund_payment_method_id",
                table: "customer_refund",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_refund_tenant",
                table: "customer_refund",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ux_customer_refund_no",
                table: "customer_refund",
                column: "refund_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customer_refund_source_credit_note_id",
                table: "customer_refund_source",
                column: "credit_note_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refund_source_payment_id",
                table: "customer_refund_source",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_refund_source_refund_id",
                table: "customer_refund_source",
                column: "refund_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_installment_due",
                table: "invoice_installment",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ux_invoice_installment_seq",
                table: "invoice_installment",
                columns: new[] { "invoice_id", "seq_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journal_approval_entry",
                table: "journal_approval",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_journal_approval_status",
                table: "journal_approval",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_family",
                table: "refresh_token",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_user",
                table: "refresh_token",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ux_refresh_token_hash",
                table: "refresh_token",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tax_rate_input_account_id",
                table: "tax_rate",
                column: "input_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rate_output_account_id",
                table: "tax_rate",
                column: "output_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_tax_rate_code_from",
                table: "tax_rate",
                columns: new[] { "code", "valid_from" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounting_setting");

            migrationBuilder.DropTable(
                name: "audit_trail_link");

            migrationBuilder.DropTable(
                name: "bank_statement_line");

            migrationBuilder.DropTable(
                name: "bank_transfer");

            migrationBuilder.DropTable(
                name: "cashflow_item");

            migrationBuilder.DropTable(
                name: "credit_note_allocation");

            migrationBuilder.DropTable(
                name: "credit_note_line");

            migrationBuilder.DropTable(
                name: "customer_refund_source");

            migrationBuilder.DropTable(
                name: "invoice_installment");

            migrationBuilder.DropTable(
                name: "journal_approval");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "tax_rate");

            migrationBuilder.DropTable(
                name: "bank_reconciliation");

            migrationBuilder.DropTable(
                name: "credit_note");

            migrationBuilder.DropTable(
                name: "customer_refund");

            migrationBuilder.DropTable(
                name: "bank_account");

            migrationBuilder.DropColumn(
                name: "tax_rate_id",
                table: "vendor_bill_item");

            migrationBuilder.DropColumn(
                name: "tax_rate_id",
                table: "invoice_item");

            migrationBuilder.DropColumn(
                name: "duration_years",
                table: "cd_renewal");

            migrationBuilder.DropColumn(
                name: "invoiced_gov_fees",
                table: "cd_renewal");

            migrationBuilder.DropColumn(
                name: "owner_invoice_id",
                table: "cd_renewal");
        }
    }
}
