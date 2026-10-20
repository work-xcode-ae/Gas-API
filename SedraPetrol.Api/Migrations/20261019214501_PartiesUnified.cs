using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <summary>
    /// One table for every person / company: <c>party</c>. Customers (tenant), suppliers (vendor), employees and drivers become
    /// role tables keyed by <c>party_id</c>: <c>party_customer</c>, <c>party_vendor</c>, <c>employee</c>, <c>driver</c>
    /// (owners keep using <c>building_owner.party_id</c>). Name, ID numbers, phone, e-mail, address and the ledger account
    /// (<c>party.account_id</c>) are stored once on <c>party</c>.
    /// Data is moved: every tenant / vendor / employee / driver becomes a party (or joins the existing party with the same
    /// Emirates ID / trade licence) and every reference to it is updated to the party id.
    /// </summary>
    public partial class PartiesUnified : Migration
    {
        private static string DropFksTo(params string[] tables) => $@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.referenced_object_id) IN ({string.Join(", ", Array.ConvertAll(tables, t => $"N'{t}'"))})
   OR OBJECT_NAME(fk.parent_object_id) IN ({string.Join(", ", Array.ConvertAll(tables, t => $"N'{t}'"))});
EXEC sp_executesql @sql;";

        private static string DropIndexesOf(params string[] tables) => $@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'DROP INDEX ' + QUOTENAME(i.name) + N' ON ' + QUOTENAME(OBJECT_NAME(i.object_id)) + N';'
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN ({string.Join(", ", Array.ConvertAll(tables, t => $"N'{t}'"))}) AND i.is_primary_key = 0 AND i.type > 0;
EXEC sp_executesql @sql;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- 1. release the old role tables and keep them aside ----------
            migrationBuilder.Sql(DropFksTo("tenant", "vendor", "employee", "driver"));
            migrationBuilder.Sql(DropIndexesOf("tenant", "vendor", "employee", "driver"));
            migrationBuilder.Sql(@"
EXEC sp_rename N'dbo.tenant',   N'tenant_old';
EXEC sp_rename N'dbo.vendor',   N'vendor_old';
EXEC sp_rename N'dbo.employee', N'employee_old';
EXEC sp_rename N'dbo.driver',   N'driver_old';
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
FROM sys.key_constraints WHERE type = 'PK' AND OBJECT_NAME(parent_object_id) IN (N'tenant_old', N'vendor_old', N'employee_old', N'driver_old');
EXEC sp_executesql @sql;");

            // ---------- 2. party: the shared person / company fields ----------
            migrationBuilder.AlterColumn<string>(name: "emirates_id", table: "party", type: "nvarchar(30)", maxLength: 30, nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(20)", oldMaxLength: 20, oldNullable: true);
            migrationBuilder.AddColumn<string>(name: "party_code", table: "party", type: "nvarchar(20)", maxLength: 20, nullable: true);
            migrationBuilder.AddColumn<DateOnly>(name: "date_of_birth", table: "party", type: "date", nullable: true);
            migrationBuilder.AddColumn<string>(name: "phone", table: "party", type: "nvarchar(30)", maxLength: 30, nullable: true);
            migrationBuilder.AddColumn<string>(name: "email", table: "party", type: "nvarchar(150)", maxLength: 150, nullable: true);
            migrationBuilder.AddColumn<long>(name: "account_id", table: "party", type: "bigint", nullable: true);
            migrationBuilder.AddColumn<string>(name: "notes", table: "party", type: "nvarchar(max)", nullable: true);

            // ---------- 3. role tables ----------
            migrationBuilder.CreateTable(
                name: "party_customer",
                columns: table => new
                {
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_party_customer", x => x.party_id));

            migrationBuilder.CreateTable(
                name: "party_vendor",
                columns: table => new
                {
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
                    vendor_type_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_party_vendor", x => x.party_id));

            migrationBuilder.CreateTable(
                name: "employee",
                columns: table => new
                {
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    employee_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    job_title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    department_id = table.Column<long>(type: "bigint", nullable: true),
                    branch_id = table.Column<long>(type: "bigint", nullable: true),
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
                constraints: table => table.PrimaryKey("PK_employee", x => x.party_id));

            migrationBuilder.CreateTable(
                name: "driver",
                columns: table => new
                {
                    party_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_id = table.Column<long>(type: "bigint", nullable: true),
                    license_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    license_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    hazmat_permit_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    updated_by = table.Column<long>(type: "bigint", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_driver", x => x.party_id));

            // ---------- 4. move the people into party ----------
            migrationBuilder.Sql(@"
DECLARE @ind bigint = (SELECT v.id FROM lookup_value v JOIN lookup_type t ON t.id = v.lookup_type_id WHERE t.code = N'PARTY_TYPE' AND v.code = N'individual');
DECLARE @co  bigint = (SELECT v.id FROM lookup_value v JOIN lookup_type t ON t.id = v.lookup_type_id WHERE t.code = N'PARTY_TYPE' AND v.code = N'company');
SET @co = ISNULL(@co, @ind);

-- existing parties (owners): code + main phone / e-mail from their contacts
UPDATE party SET party_code = N'PT-' + RIGHT(N'000000' + CAST(id AS nvarchar(20)), 6) WHERE party_code IS NULL;
UPDATE p SET
    phone = (SELECT TOP 1 c.value FROM party_contact c JOIN lookup_value v ON v.id = c.contact_type_id
             WHERE c.party_id = p.id AND v.code IN (N'mobile', N'phone') ORDER BY c.is_default DESC, c.id),
    email = (SELECT TOP 1 c.value FROM party_contact c JOIN lookup_value v ON v.id = c.contact_type_id
             WHERE c.party_id = p.id AND v.code = N'email' ORDER BY c.is_default DESC, c.id)
FROM party p;

CREATE TABLE #mt (old_id bigint PRIMARY KEY, new_id bigint NOT NULL);   -- tenant   → party
CREATE TABLE #mv (old_id bigint PRIMARY KEY, new_id bigint NOT NULL);   -- vendor   → party
CREATE TABLE #me (old_id bigint PRIMARY KEY, new_id bigint NOT NULL);   -- employee → party
CREATE TABLE #md (old_id bigint PRIMARY KEY, new_id bigint NOT NULL);   -- driver   → party

-- customers: same Emirates ID = same person (e.g. an owner)
INSERT INTO #mt (old_id, new_id)
SELECT t.id, x.id FROM tenant_old t CROSS APPLY (SELECT TOP 1 p.id FROM party p WHERE p.emirates_id = t.emirates_id ORDER BY p.id) x
WHERE t.emirates_id IS NOT NULL AND t.emirates_id <> N'';
MERGE party AS tgt
USING (SELECT * FROM tenant_old WHERE id NOT IN (SELECT old_id FROM #mt)) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (party_code, branch_id, party_type_id, name_en, name_ar, emirates_id, nationality_id, date_of_birth, phone, email, city_id, address,
                              account_id, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
VALUES (s.tenant_code, s.branch_id, @ind, s.name_en, s.name_ar, NULLIF(s.emirates_id, N''), s.nationality_id, s.date_of_birth, s.phone, s.email, s.city_id, s.address,
        s.receivable_account_id, s.is_active, s.created_at, s.created_by, s.updated_at, s.updated_by, s.is_deleted)
OUTPUT s.id, inserted.id INTO #mt (old_id, new_id);
UPDATE p SET p.phone = ISNULL(p.phone, t.phone), p.email = ISNULL(p.email, t.email), p.date_of_birth = ISNULL(p.date_of_birth, t.date_of_birth),
             p.account_id = ISNULL(p.account_id, t.receivable_account_id)
FROM party p JOIN #mt m ON m.new_id = p.id JOIN tenant_old t ON t.id = m.old_id;
INSERT INTO party_customer (party_id, branch_id, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
SELECT m.new_id, MIN(t.branch_id), CAST(MAX(CAST(t.is_active AS int)) AS bit), MIN(t.created_at), MIN(t.created_by), MAX(t.updated_at), MAX(t.updated_by), CAST(MIN(CAST(t.is_deleted AS int)) AS bit)
FROM tenant_old t JOIN #mt m ON m.old_id = t.id GROUP BY m.new_id;

-- suppliers: same trade licence = same company
INSERT INTO #mv (old_id, new_id)
SELECT v.id, x.id FROM vendor_old v CROSS APPLY (SELECT TOP 1 p.id FROM party p WHERE p.trade_license_no = v.trade_license_no ORDER BY p.id) x
WHERE v.trade_license_no IS NOT NULL AND v.trade_license_no <> N'';
MERGE party AS tgt
USING (SELECT o.*, CASE WHEN o.code IS NULL OR o.code = N'' OR EXISTS (SELECT 1 FROM party x WHERE x.party_code = o.code)
                         THEN N'PV-' + RIGHT(N'000000' + CAST(o.id AS nvarchar(20)), 6) ELSE o.code END AS new_code
       FROM vendor_old o WHERE o.id NOT IN (SELECT old_id FROM #mv)) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (party_code, branch_id, party_type_id, name_en, name_ar, trade_license_no, tax_number, phone, email, address,
                              account_id, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
VALUES (s.new_code,
        s.branch_id, @co, s.name_en, s.name_ar, s.trade_license_no, s.trn, s.phone, s.email, s.address,
        s.payable_account_id, s.is_active, s.created_at, s.created_by, s.updated_at, s.updated_by, s.is_deleted)
OUTPUT s.id, inserted.id INTO #mv (old_id, new_id);
UPDATE p SET p.tax_number = ISNULL(p.tax_number, v.trn), p.phone = ISNULL(p.phone, v.phone), p.email = ISNULL(p.email, v.email),
             p.account_id = ISNULL(p.account_id, v.payable_account_id)
FROM party p JOIN #mv m ON m.new_id = p.id JOIN vendor_old v ON v.id = m.old_id;
INSERT INTO party_vendor (party_id, branch_id, vendor_type_id, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
SELECT m.new_id, MIN(v.branch_id), MIN(v.vendor_type_id), CAST(MAX(CAST(v.is_active AS int)) AS bit), MIN(v.created_at), MIN(v.created_by), MAX(v.updated_at), MAX(v.updated_by), CAST(MIN(CAST(v.is_deleted AS int)) AS bit)
FROM vendor_old v JOIN #mv m ON m.old_id = v.id GROUP BY m.new_id;

-- employees: same Emirates ID = same person
INSERT INTO #me (old_id, new_id)
SELECT e.id, x.id FROM employee_old e CROSS APPLY (SELECT TOP 1 p.id FROM party p WHERE p.emirates_id = e.emirates_id ORDER BY p.id) x
WHERE e.emirates_id IS NOT NULL AND e.emirates_id <> N'';
MERGE party AS tgt
USING (SELECT * FROM employee_old WHERE id NOT IN (SELECT old_id FROM #me)) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (party_code, branch_id, party_type_id, name_en, name_ar, emirates_id, phone, email, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
VALUES (N'PE-' + RIGHT(N'000000' + CAST(s.id AS nvarchar(20)), 6), s.branch_id, @ind, s.name_en, s.name_ar, NULLIF(s.emirates_id, N''), s.phone, s.email,
        s.is_active, s.created_at, s.created_by, s.updated_at, s.updated_by, s.is_deleted)
OUTPUT s.id, inserted.id INTO #me (old_id, new_id);
INSERT INTO employee (party_id, employee_no, job_title, department_id, branch_id, hire_date, user_id, is_active, notes, created_at, created_by, updated_at, updated_by, is_deleted)
SELECT m.new_id, e.employee_no, e.job_title, e.department_id, e.branch_id, e.hire_date, e.user_id, e.is_active, e.notes, e.created_at, e.created_by, e.updated_at, e.updated_by, e.is_deleted
FROM employee_old e JOIN #me m ON m.old_id = e.id
WHERE e.id = (SELECT MIN(e2.id) FROM employee_old e2 JOIN #me m2 ON m2.old_id = e2.id WHERE m2.new_id = m.new_id);

-- drivers
MERGE party AS tgt
USING driver_old AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (party_code, party_type_id, name_en, name_ar, phone, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
VALUES (N'PD-' + RIGHT(N'000000' + CAST(s.id AS nvarchar(20)), 6), @ind, s.name_en, s.name_ar, s.phone, s.is_active, s.created_at, s.created_by, s.updated_at, s.updated_by, s.is_deleted)
OUTPUT s.id, inserted.id INTO #md (old_id, new_id);
INSERT INTO driver (party_id, vendor_id, license_no, license_expiry, hazmat_permit_no, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
SELECT m.new_id, mv.new_id, d.license_no, d.license_expiry, d.hazmat_permit_no, d.is_active, d.created_at, d.created_by, d.updated_at, d.updated_by, d.is_deleted
FROM driver_old d JOIN #md m ON m.old_id = d.id LEFT JOIN #mv mv ON mv.old_id = d.vendor_id;

-- ---------- every reference → party id ----------
UPDATE x SET x.tenant_id = m.new_id FROM contract_supply_spec x JOIN #mt m ON m.old_id = x.tenant_id;
UPDATE x SET x.tenant_id = m.new_id FROM credit_note          x JOIN #mt m ON m.old_id = x.tenant_id;
UPDATE x SET x.tenant_id = m.new_id FROM customer_refund      x JOIN #mt m ON m.old_id = x.tenant_id;
UPDATE x SET x.tenant_id = m.new_id FROM invoice              x JOIN #mt m ON m.old_id = x.tenant_id;
UPDATE x SET x.tenant_id = m.new_id FROM journal_line         x JOIN #mt m ON m.old_id = x.tenant_id;
UPDATE x SET x.tenant_id = m.new_id FROM payment              x JOIN #mt m ON m.old_id = x.tenant_id;
UPDATE x SET x.tenant_id = m.new_id FROM service_request      x JOIN #mt m ON m.old_id = x.tenant_id;

UPDATE x SET x.vendor_id = m.new_id FROM contract_amc_spec x JOIN #mv m ON m.old_id = x.vendor_id;
UPDATE x SET x.vendor_id = m.new_id FROM journal_line      x JOIN #mv m ON m.old_id = x.vendor_id;
UPDATE x SET x.vendor_id = m.new_id FROM refill_po         x JOIN #mv m ON m.old_id = x.vendor_id;
UPDATE x SET x.assigned_vendor_id = m.new_id FROM service_request x JOIN #mv m ON m.old_id = x.assigned_vendor_id;
UPDATE x SET x.vendor_id = m.new_id FROM vehicle           x JOIN #mv m ON m.old_id = x.vendor_id;
UPDATE x SET x.vendor_id = m.new_id FROM vendor_bill       x JOIN #mv m ON m.old_id = x.vendor_id;
UPDATE x SET x.vendor_id = m.new_id FROM vendor_contact    x JOIN #mv m ON m.old_id = x.vendor_id;
UPDATE x SET x.vendor_id = m.new_id FROM vendor_payment    x JOIN #mv m ON m.old_id = x.vendor_id;

UPDATE x SET x.assigned_employee_id = m.new_id FROM asset x JOIN #me m ON m.old_id = x.assigned_employee_id;

UPDATE x SET x.default_driver_id = m.new_id FROM vehicle           x JOIN #md m ON m.old_id = x.default_driver_id;
UPDATE x SET x.driver_id         = m.new_id FROM refill_collection x JOIN #md m ON m.old_id = x.driver_id;
UPDATE x SET x.driver_id         = m.new_id FROM tank_refill       x JOIN #md m ON m.old_id = x.driver_id;

-- references without a foreign key (activity log, documents, audit trail)
UPDATE x SET x.entity_id = m.new_id FROM activity_log x JOIN #mt m ON m.old_id = x.entity_id WHERE x.entity_type = N'tenant';
UPDATE x SET x.entity_id = m.new_id FROM activity_log x JOIN #mv m ON m.old_id = x.entity_id WHERE x.entity_type = N'vendor';
UPDATE x SET x.entity_id = m.new_id FROM document     x JOIN #mt m ON m.old_id = x.entity_id WHERE x.entity_type = N'tenant';
UPDATE x SET x.entity_id = m.new_id FROM document     x JOIN #mv m ON m.old_id = x.entity_id WHERE x.entity_type = N'vendor';
UPDATE x SET x.entity_id = m.new_id FROM document     x JOIN #me m ON m.old_id = x.entity_id WHERE x.entity_type = N'employee';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail      x JOIN #mt m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Tenant';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail      x JOIN #mv m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Vendor';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail      x JOIN #me m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Employee';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail      x JOIN #md m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Driver';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail_link x JOIN #mt m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Tenant';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail_link x JOIN #mv m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Vendor';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail_link x JOIN #me m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Employee';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail_link x JOIN #md m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Driver';

-- two parties may not share one ledger account
UPDATE p SET account_id = NULL FROM party p
WHERE p.account_id IS NOT NULL AND EXISTS (SELECT 1 FROM party q WHERE q.account_id = p.account_id AND q.id < p.id);
");

            // ---------- 5. drop the old tables ----------
            migrationBuilder.DropTable(name: "tenant_old");
            migrationBuilder.DropTable(name: "vendor_old");
            migrationBuilder.DropTable(name: "employee_old");
            migrationBuilder.DropTable(name: "driver_old");

            migrationBuilder.AlterColumn<string>(name: "party_code", table: "party", type: "nvarchar(20)", maxLength: 20, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(20)", oldMaxLength: 20, oldNullable: true);

            // ---------- 6. indexes ----------
            migrationBuilder.CreateIndex(name: "ux_party_party_code", table: "party", column: "party_code", unique: true);
            migrationBuilder.CreateIndex(name: "ux_party_emirates_id", table: "party", column: "emirates_id", unique: true, filter: "[emirates_id] IS NOT NULL AND [is_deleted] = 0");
            migrationBuilder.CreateIndex(name: "ux_party_account", table: "party", column: "account_id", unique: true, filter: "[account_id] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_party_customer_branch_id", table: "party_customer", column: "branch_id");
            migrationBuilder.CreateIndex(name: "IX_party_vendor_branch_id", table: "party_vendor", column: "branch_id");
            migrationBuilder.CreateIndex(name: "IX_party_vendor_vendor_type_id", table: "party_vendor", column: "vendor_type_id");
            migrationBuilder.CreateIndex(name: "IX_employee_branch_id", table: "employee", column: "branch_id");
            migrationBuilder.CreateIndex(name: "IX_employee_department_id", table: "employee", column: "department_id");
            migrationBuilder.CreateIndex(name: "ux_employee_user", table: "employee", column: "user_id", unique: true, filter: "[user_id] IS NOT NULL AND [is_deleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_driver_vendor_id", table: "driver", column: "vendor_id");

            // ---------- 7. foreign keys ----------
            void Fk(string name, string table, string column, string principal, string principalColumn = "id") =>
                migrationBuilder.AddForeignKey(name, table, column, principal, principalColumn: principalColumn, onDelete: ReferentialAction.Restrict);

            Fk("FK_party_account_account_id", "party", "account_id", "account");
            // role → party
            Fk("FK_party_customer_party_party_id", "party_customer", "party_id", "party");
            Fk("FK_party_customer_branch_branch_id", "party_customer", "branch_id", "branch");
            Fk("FK_party_vendor_party_party_id", "party_vendor", "party_id", "party");
            Fk("FK_party_vendor_branch_branch_id", "party_vendor", "branch_id", "branch");
            Fk("FK_party_vendor_lookup_value_vendor_type_id", "party_vendor", "vendor_type_id", "lookup_value");
            Fk("FK_employee_party_party_id", "employee", "party_id", "party");
            Fk("FK_employee_app_user_user_id", "employee", "user_id", "app_user");
            Fk("FK_employee_branch_branch_id", "employee", "branch_id", "branch");
            Fk("FK_employee_lookup_value_department_id", "employee", "department_id", "lookup_value");
            Fk("FK_driver_party_party_id", "driver", "party_id", "party");
            Fk("FK_driver_party_vendor_vendor_id", "driver", "vendor_id", "party_vendor", "party_id");
            // customers
            Fk("FK_contract_supply_spec_party_customer_tenant_id", "contract_supply_spec", "tenant_id", "party_customer", "party_id");
            Fk("FK_credit_note_party_customer_tenant_id", "credit_note", "tenant_id", "party_customer", "party_id");
            Fk("FK_customer_refund_party_customer_tenant_id", "customer_refund", "tenant_id", "party_customer", "party_id");
            Fk("FK_invoice_party_customer_tenant_id", "invoice", "tenant_id", "party_customer", "party_id");
            Fk("FK_journal_line_party_customer_tenant_id", "journal_line", "tenant_id", "party_customer", "party_id");
            Fk("FK_payment_party_customer_tenant_id", "payment", "tenant_id", "party_customer", "party_id");
            Fk("FK_service_request_party_customer_tenant_id", "service_request", "tenant_id", "party_customer", "party_id");
            // suppliers
            Fk("FK_contract_amc_spec_party_vendor_vendor_id", "contract_amc_spec", "vendor_id", "party_vendor", "party_id");
            Fk("FK_journal_line_party_vendor_vendor_id", "journal_line", "vendor_id", "party_vendor", "party_id");
            Fk("FK_refill_po_party_vendor_vendor_id", "refill_po", "vendor_id", "party_vendor", "party_id");
            Fk("FK_service_request_party_vendor_assigned_vendor_id", "service_request", "assigned_vendor_id", "party_vendor", "party_id");
            Fk("FK_vehicle_party_vendor_vendor_id", "vehicle", "vendor_id", "party_vendor", "party_id");
            Fk("FK_vendor_bill_party_vendor_vendor_id", "vendor_bill", "vendor_id", "party_vendor", "party_id");
            Fk("FK_vendor_contact_party_vendor_vendor_id", "vendor_contact", "vendor_id", "party_vendor", "party_id");
            Fk("FK_vendor_payment_party_vendor_vendor_id", "vendor_payment", "vendor_id", "party_vendor", "party_id");
            // employees / drivers
            Fk("FK_asset_employee_assigned_employee_id", "asset", "assigned_employee_id", "employee", "party_id");
            Fk("FK_vehicle_driver_default_driver_id", "vehicle", "default_driver_id", "driver", "party_id");
            Fk("FK_refill_collection_driver_driver_id", "refill_collection", "driver_id", "driver", "party_id");
            Fk("FK_tank_refill_driver_driver_id", "tank_refill", "driver_id", "driver", "party_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("PartiesUnified cannot be rolled back automatically: tenants, vendors, employees and drivers were merged into party. Restore a database backup taken before this migration.");
        }
    }
}
