using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <summary>
    /// One table for every contract + one spec table per contract type:
    /// <c>supply_contract</c> + <c>amc_contract</c> → <c>contract</c> (header) + <c>contract_supply_spec</c> + <c>contract_amc_spec</c>.
    /// <c>amc_term</c> is merged into <c>contract_term</c>. contract_fee, contract_term, invoice and journal_line now point to <c>contract</c>.
    /// Data is moved: supply contracts keep their ids, AMC contracts get id + MAX(supply id) and every reference to them is updated
    /// (instalments, scope, visits, terms, activity log, documents, vendor bills, journals, audit trail).
    /// </summary>
    public partial class UnifiedContracts : Migration
    {
        /// <summary>Drops every foreign key that references one of the given tables (whatever its name).</summary>
        private static string DropFksTo(params string[] tables) => $@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.referenced_object_id) IN ({string.Join(", ", Array.ConvertAll(tables, t => $"N'{t}'"))});
EXEC sp_executesql @sql;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- 1. new tables ----------
            migrationBuilder.CreateTable(
                name: "contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    contract_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    building_id = table.Column<long>(type: "bigint", nullable: true),
                    unit_id = table.Column<long>(type: "bigint", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    auto_renew = table.Column<bool>(type: "bit", nullable: false),
                    renewal_period_months = table.Column<short>(type: "smallint", nullable: true),
                    currency_id = table.Column<long>(type: "bigint", nullable: true),
                    status_id = table.Column<long>(type: "bigint", nullable: true),
                    terminated_at = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_contract", x => x.id);
                    table.ForeignKey("FK_contract_branch_branch_id", x => x.branch_id, "branch", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_contract_building_building_id", x => x.building_id, "building", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_contract_currency_currency_id", x => x.currency_id, "currency", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_contract_lookup_value_status_id", x => x.status_id, "lookup_value", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_contract_unit_unit_id", x => x.unit_id, "unit", "id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_supply_spec",
                columns: table => new
                {
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    deposit_amount = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    deposit_paid = table.Column<bool>(type: "bit", nullable: false),
                    deposit_date = table.Column<DateOnly>(type: "date", nullable: true),
                    terms = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_supply_spec", x => x.contract_id);
                    table.ForeignKey("FK_contract_supply_spec_contract_contract_id", x => x.contract_id, "contract", "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_contract_supply_spec_tenant_tenant_id", x => x.tenant_id, "tenant", "id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_amc_spec",
                columns: table => new
                {
                    contract_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_id = table.Column<long>(type: "bigint", nullable: false),
                    owner_party_id = table.Column<long>(type: "bigint", nullable: true),
                    annual_value = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: true),
                    visit_frequency_id = table.Column<long>(type: "bigint", nullable: true),
                    visits_total = table.Column<short>(type: "smallint", nullable: true),
                    response_time_hours = table.Column<short>(type: "smallint", nullable: true),
                    emergency_included = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_amc_spec", x => x.contract_id);
                    table.ForeignKey("FK_contract_amc_spec_contract_contract_id", x => x.contract_id, "contract", "id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_contract_amc_spec_lookup_value_visit_frequency_id", x => x.visit_frequency_id, "lookup_value", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_contract_amc_spec_party_owner_party_id", x => x.owner_party_id, "party", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_contract_amc_spec_vendor_vendor_id", x => x.vendor_id, "vendor", "id", onDelete: ReferentialAction.Restrict);
                });

            // ---------- 2. release the old tables (every FK that points to them) ----------
            migrationBuilder.Sql(DropFksTo("supply_contract", "amc_contract", "amc_term"));

            // ---------- 3. move the data ----------
            migrationBuilder.Sql(@"
DECLARE @off bigint = ISNULL((SELECT MAX(id) FROM supply_contract), 0);

SET IDENTITY_INSERT contract ON;

INSERT INTO contract (id, contract_type, branch_id, contract_no, building_id, unit_id, start_date, end_date, auto_renew, renewal_period_months,
                      currency_id, status_id, terminated_at, notes, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
SELECT s.id, N'supply', s.branch_id, s.contract_no, u.building_id, s.unit_id, s.start_date, s.end_date, s.auto_renew, s.renewal_period_months,
       (SELECT TOP 1 id FROM currency WHERE code = N'AED'), s.status_id, s.terminated_at, NULL, s.is_active, s.created_at, s.created_by, s.updated_at, s.updated_by, s.is_deleted
FROM supply_contract s
LEFT JOIN unit u ON u.id = s.unit_id;

INSERT INTO contract (id, contract_type, branch_id, contract_no, building_id, unit_id, start_date, end_date, auto_renew, renewal_period_months,
                      currency_id, status_id, terminated_at, notes, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
SELECT a.id + @off, N'amc', a.branch_id, a.contract_no, a.building_id, NULL, a.start_date, a.end_date, 0, NULL,
       a.currency_id, a.status_id, NULL, a.notes, a.is_active, a.created_at, a.created_by, a.updated_at, a.updated_by, a.is_deleted
FROM amc_contract a;

SET IDENTITY_INSERT contract OFF;

INSERT INTO contract_supply_spec (contract_id, tenant_id, deposit_amount, deposit_paid, deposit_date, terms)
SELECT id, tenant_id, deposit_amount, deposit_paid, deposit_date, terms FROM supply_contract;

INSERT INTO contract_amc_spec (contract_id, vendor_id, owner_party_id, annual_value, visit_frequency_id, visits_total, response_time_hours, emergency_included)
SELECT id + @off, vendor_id, owner_party_id, annual_value, visit_frequency_id, visits_total, response_time_hours, emergency_included FROM amc_contract;

-- AMC contracts have new ids: update everything that points to them
UPDATE amc_payment_schedule SET contract_id = contract_id + @off;
UPDATE amc_scope_item       SET contract_id = contract_id + @off;
UPDATE maintenance_visit    SET amc_contract_id = amc_contract_id + @off;
UPDATE contract_term        SET contract_id = contract_id + @off WHERE contract_type = N'amc' AND contract_id IS NOT NULL;
UPDATE activity_log SET entity_id = entity_id + @off WHERE entity_type IN (N'amc_contract', N'AmcContract') AND entity_id IS NOT NULL;
UPDATE document     SET entity_id = entity_id + @off WHERE entity_type IN (N'amc_contract', N'AmcContract', N'amc');
UPDATE vendor_bill   SET source_id = source_id + @off WHERE source_type = N'amc_contract' AND source_id IS NOT NULL;
UPDATE journal_entry SET source_id = source_id + @off WHERE source_type = N'amc_contract' AND source_id IS NOT NULL;
UPDATE audit_trail      SET entity_id = CAST(TRY_CAST(entity_id AS bigint) + @off AS nvarchar(100)) WHERE entity_type = N'AmcContract' AND TRY_CAST(entity_id AS bigint) IS NOT NULL;
UPDATE audit_trail_link SET entity_id = CAST(TRY_CAST(entity_id AS bigint) + @off AS nvarchar(100)) WHERE entity_type = N'AmcContract' AND TRY_CAST(entity_id AS bigint) IS NOT NULL;

-- AMC instalment invoices now carry their contract (invoice.contract_id can point to any contract)
UPDATE i SET i.contract_id = s.contract_id
FROM invoice i JOIN amc_payment_schedule s ON s.invoice_id = i.id
WHERE i.contract_id IS NULL;

-- old AMC terms (amc_term) → contract_term, only for contracts that have no contract_term rows yet (same rule the API used)
INSERT INTO contract_term (contract_type, contract_id, source_id, category, text_ar, text_en, is_default, is_custom, is_modified, sort_order, is_active,
                           created_at, created_by, updated_at, updated_by, is_deleted)
SELECT N'amc', t.contract_id + @off, NULL, N'general', t.description, NULL, 0, t.is_custom, 0,
       10 * ROW_NUMBER() OVER (PARTITION BY t.contract_id ORDER BY t.id), 1, t.created_at, t.created_by, t.updated_at, t.updated_by, t.is_deleted
FROM amc_term t
WHERE NOT EXISTS (SELECT 1 FROM contract_term c WHERE c.contract_type = N'amc' AND c.contract_id = t.contract_id + @off);

-- contract terms whose contract no longer exists (contract_term had no foreign key before)
DELETE FROM contract_term WHERE contract_id IS NOT NULL AND contract_id NOT IN (SELECT id FROM contract);
");

            // ---------- 4. drop the old tables ----------
            migrationBuilder.DropTable(name: "amc_term");
            migrationBuilder.DropTable(name: "supply_contract");
            migrationBuilder.DropTable(name: "amc_contract");

            // ---------- 5. indexes ----------
            migrationBuilder.CreateIndex(name: "IX_contract_branch_id", table: "contract", column: "branch_id");
            migrationBuilder.CreateIndex(name: "ix_contract_building", table: "contract", column: "building_id");
            migrationBuilder.CreateIndex(name: "IX_contract_currency_id", table: "contract", column: "currency_id");
            migrationBuilder.CreateIndex(name: "IX_contract_status_id", table: "contract", column: "status_id");
            migrationBuilder.CreateIndex(name: "ix_contract_type_status", table: "contract", columns: new[] { "contract_type", "status_id" });
            migrationBuilder.CreateIndex(name: "ux_contract_active_unit", table: "contract", column: "unit_id", unique: true,
                filter: "[unit_id] IS NOT NULL AND [is_active] = 1 AND [is_deleted] = 0");
            migrationBuilder.CreateIndex(name: "ux_contract_contract_no", table: "contract", column: "contract_no", unique: true);
            migrationBuilder.CreateIndex(name: "ix_contract_supply_spec_tenant", table: "contract_supply_spec", column: "tenant_id");
            migrationBuilder.CreateIndex(name: "ix_contract_amc_spec_vendor", table: "contract_amc_spec", column: "vendor_id");
            migrationBuilder.CreateIndex(name: "IX_contract_amc_spec_owner_party_id", table: "contract_amc_spec", column: "owner_party_id");
            migrationBuilder.CreateIndex(name: "IX_contract_amc_spec_visit_frequency_id", table: "contract_amc_spec", column: "visit_frequency_id");
            migrationBuilder.CreateIndex(name: "IX_contract_term_contract_id", table: "contract_term", column: "contract_id");

            // ---------- 6. child tables → contract / spec tables ----------
            // shared by every contract type
            migrationBuilder.AddForeignKey("FK_contract_fee_contract_contract_id", "contract_fee", "contract_id", "contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_contract_term_contract_contract_id", "contract_term", "contract_id", "contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_invoice_contract_contract_id", "invoice", "contract_id", "contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_journal_line_contract_contract_id", "journal_line", "contract_id", "contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            // supply only
            migrationBuilder.AddForeignKey("FK_meter_reading_contract_supply_spec_contract_id", "meter_reading", "contract_id", "contract_supply_spec", principalColumn: "contract_id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_meter_service_contract_supply_spec_contract_id", "meter_service", "contract_id", "contract_supply_spec", principalColumn: "contract_id", onDelete: ReferentialAction.Restrict);
            // AMC only
            migrationBuilder.AddForeignKey("FK_amc_payment_schedule_contract_amc_spec_contract_id", "amc_payment_schedule", "contract_id", "contract_amc_spec", principalColumn: "contract_id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_amc_scope_item_contract_amc_spec_contract_id", "amc_scope_item", "contract_id", "contract_amc_spec", principalColumn: "contract_id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_maintenance_visit_contract_amc_spec_amc_contract_id", "maintenance_visit", "amc_contract_id", "contract_amc_spec", principalColumn: "contract_id", onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ---------- 1. old tables ----------
            migrationBuilder.CreateTable(
                name: "supply_contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    contract_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    tenant_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.ForeignKey("FK_supply_contract_branch_branch_id", x => x.branch_id, "branch", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_supply_contract_lookup_value_status_id", x => x.status_id, "lookup_value", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_supply_contract_tenant_tenant_id", x => x.tenant_id, "tenant", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_supply_contract_unit_unit_id", x => x.unit_id, "unit", "id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "amc_contract",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
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
                    table.ForeignKey("FK_amc_contract_branch_branch_id", x => x.branch_id, "branch", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_amc_contract_building_building_id", x => x.building_id, "building", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_amc_contract_currency_currency_id", x => x.currency_id, "currency", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_amc_contract_lookup_value_status_id", x => x.status_id, "lookup_value", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_amc_contract_lookup_value_visit_frequency_id", x => x.visit_frequency_id, "lookup_value", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_amc_contract_party_owner_party_id", x => x.owner_party_id, "party", "id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_amc_contract_vendor_vendor_id", x => x.vendor_id, "vendor", "id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "amc_term",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
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
                    table.ForeignKey("FK_amc_term_lookup_value_term_type_id", x => x.term_type_id, "lookup_value", "id", onDelete: ReferentialAction.Restrict);
                });

            // ---------- 2. release the new tables ----------
            migrationBuilder.Sql(DropFksTo("contract", "contract_supply_spec", "contract_amc_spec"));

            // ---------- 3. move the data back (supply keeps its id, AMC gets back id - MAX(supply id)) ----------
            migrationBuilder.Sql(@"
DECLARE @off bigint = ISNULL((SELECT MAX(c.id) FROM contract c JOIN contract_supply_spec s ON s.contract_id = c.id), 0);

SET IDENTITY_INSERT supply_contract ON;
INSERT INTO supply_contract (id, branch_id, contract_no, tenant_id, unit_id, start_date, end_date, auto_renew, renewal_period_months, deposit_amount, deposit_paid,
                             deposit_date, terms, status_id, terminated_at, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
SELECT c.id, c.branch_id, c.contract_no, s.tenant_id, c.unit_id, c.start_date, c.end_date, c.auto_renew, c.renewal_period_months, s.deposit_amount, s.deposit_paid,
       s.deposit_date, s.terms, c.status_id, c.terminated_at, c.is_active, c.created_at, c.created_by, c.updated_at, c.updated_by, c.is_deleted
FROM contract c JOIN contract_supply_spec s ON s.contract_id = c.id;
SET IDENTITY_INSERT supply_contract OFF;

SET IDENTITY_INSERT amc_contract ON;
INSERT INTO amc_contract (id, branch_id, contract_no, vendor_id, building_id, owner_party_id, start_date, end_date, annual_value, currency_id, visit_frequency_id,
                          visits_total, response_time_hours, emergency_included, status_id, notes, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
SELECT c.id - @off, c.branch_id, c.contract_no, a.vendor_id, c.building_id, a.owner_party_id, c.start_date, ISNULL(c.end_date, c.start_date), a.annual_value, c.currency_id,
       a.visit_frequency_id, a.visits_total, a.response_time_hours, a.emergency_included, c.status_id, c.notes, c.is_active, c.created_at, c.created_by, c.updated_at, c.updated_by, c.is_deleted
FROM contract c JOIN contract_amc_spec a ON a.contract_id = c.id;
SET IDENTITY_INSERT amc_contract OFF;

UPDATE amc_payment_schedule SET contract_id = contract_id - @off;
UPDATE amc_scope_item       SET contract_id = contract_id - @off;
UPDATE maintenance_visit    SET amc_contract_id = amc_contract_id - @off;
UPDATE contract_term        SET contract_id = contract_id - @off WHERE contract_type = N'amc' AND contract_id IS NOT NULL;
UPDATE activity_log SET entity_id = entity_id - @off WHERE entity_type IN (N'amc_contract', N'AmcContract') AND entity_id IS NOT NULL;
UPDATE document     SET entity_id = entity_id - @off WHERE entity_type IN (N'amc_contract', N'AmcContract', N'amc');
UPDATE vendor_bill   SET source_id = source_id - @off WHERE source_type = N'amc_contract' AND source_id IS NOT NULL;
UPDATE journal_entry SET source_id = source_id - @off WHERE source_type = N'amc_contract' AND source_id IS NOT NULL;
UPDATE audit_trail      SET entity_id = CAST(TRY_CAST(entity_id AS bigint) - @off AS nvarchar(100)) WHERE entity_type = N'AmcContract' AND TRY_CAST(entity_id AS bigint) IS NOT NULL;
UPDATE audit_trail_link SET entity_id = CAST(TRY_CAST(entity_id AS bigint) - @off AS nvarchar(100)) WHERE entity_type = N'AmcContract' AND TRY_CAST(entity_id AS bigint) IS NOT NULL;
-- AMC links on invoices / journal lines / fees did not exist before
UPDATE invoice      SET contract_id = NULL WHERE contract_id IS NOT NULL AND contract_id NOT IN (SELECT id FROM supply_contract);
UPDATE journal_line SET contract_id = NULL WHERE contract_id IS NOT NULL AND contract_id NOT IN (SELECT id FROM supply_contract);
DELETE FROM contract_fee WHERE contract_id NOT IN (SELECT id FROM supply_contract);
");

            // ---------- 4. drop the new tables ----------
            migrationBuilder.DropTable(name: "contract_supply_spec");
            migrationBuilder.DropTable(name: "contract_amc_spec");
            migrationBuilder.DropTable(name: "contract");

            // ---------- 5. indexes + foreign keys as before ----------
            migrationBuilder.DropIndex(name: "IX_contract_term_contract_id", table: "contract_term");
            migrationBuilder.CreateIndex(name: "IX_supply_contract_branch_id", table: "supply_contract", column: "branch_id");
            migrationBuilder.CreateIndex(name: "IX_supply_contract_status_id", table: "supply_contract", column: "status_id");
            migrationBuilder.CreateIndex(name: "IX_supply_contract_tenant_id", table: "supply_contract", column: "tenant_id");
            migrationBuilder.CreateIndex(name: "ux_supply_contract_active_unit", table: "supply_contract", column: "unit_id", unique: true, filter: "[is_active] = 1 AND [is_deleted] = 0");
            migrationBuilder.CreateIndex(name: "ux_supply_contract_contract_no", table: "supply_contract", column: "contract_no", unique: true);
            migrationBuilder.CreateIndex(name: "IX_amc_contract_branch_id", table: "amc_contract", column: "branch_id");
            migrationBuilder.CreateIndex(name: "IX_amc_contract_building_id", table: "amc_contract", column: "building_id");
            migrationBuilder.CreateIndex(name: "IX_amc_contract_currency_id", table: "amc_contract", column: "currency_id");
            migrationBuilder.CreateIndex(name: "IX_amc_contract_owner_party_id", table: "amc_contract", column: "owner_party_id");
            migrationBuilder.CreateIndex(name: "IX_amc_contract_status_id", table: "amc_contract", column: "status_id");
            migrationBuilder.CreateIndex(name: "IX_amc_contract_vendor_id", table: "amc_contract", column: "vendor_id");
            migrationBuilder.CreateIndex(name: "IX_amc_contract_visit_frequency_id", table: "amc_contract", column: "visit_frequency_id");
            migrationBuilder.CreateIndex(name: "ux_amc_contract_contract_no", table: "amc_contract", column: "contract_no", unique: true);
            migrationBuilder.CreateIndex(name: "IX_amc_term_contract_id", table: "amc_term", column: "contract_id");
            migrationBuilder.CreateIndex(name: "IX_amc_term_term_type_id", table: "amc_term", column: "term_type_id");

            migrationBuilder.AddForeignKey("FK_amc_term_amc_contract_contract_id", "amc_term", "contract_id", "amc_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_amc_payment_schedule_amc_contract_contract_id", "amc_payment_schedule", "contract_id", "amc_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_amc_scope_item_amc_contract_contract_id", "amc_scope_item", "contract_id", "amc_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_maintenance_visit_amc_contract_amc_contract_id", "maintenance_visit", "amc_contract_id", "amc_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_contract_fee_supply_contract_contract_id", "contract_fee", "contract_id", "supply_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_invoice_supply_contract_contract_id", "invoice", "contract_id", "supply_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_journal_line_supply_contract_contract_id", "journal_line", "contract_id", "supply_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_meter_reading_supply_contract_contract_id", "meter_reading", "contract_id", "supply_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey("FK_meter_service_supply_contract_contract_id", "meter_service", "contract_id", "supply_contract", principalColumn: "id", onDelete: ReferentialAction.Restrict);
        }
    }
}
