using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SedraPetrol.Api.Migrations
{
    /// <summary>
    /// One table for every asset: <c>asset</c> (+ <c>asset_type</c>, <c>is_active</c>). Vehicles / tankers, gas meters and building
    /// tanks become assets with a spec table each: <c>asset_vehicle</c>, <c>asset_meter</c>, <c>asset_tank</c> (key <c>asset_id</c>).
    /// Data is moved: every vehicle / meter / tank gets an asset row (new id) and every reference to it is updated.
    /// </summary>
    public partial class AssetsUnified : Migration
    {
        private static string In(string[] t) => string.Join(", ", Array.ConvertAll(t, x => $"N'{x}'"));

        private static string DropFksTo(params string[] tables) => $@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
             + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';'
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.referenced_object_id) IN ({In(tables)}) OR OBJECT_NAME(fk.parent_object_id) IN ({In(tables)});
EXEC sp_executesql @sql;";

        private static string DropIndexesOf(params string[] tables) => $@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'DROP INDEX ' + QUOTENAME(i.name) + N' ON ' + QUOTENAME(OBJECT_NAME(i.object_id)) + N';'
FROM sys.indexes i
WHERE OBJECT_NAME(i.object_id) IN ({In(tables)}) AND i.is_primary_key = 0 AND i.type > 0;
EXEC sp_executesql @sql;";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------- 1. release the old tables and keep them aside ----------
            migrationBuilder.Sql(DropFksTo("meter", "vehicle", "tank"));
            migrationBuilder.Sql(DropIndexesOf("meter", "vehicle", "tank"));
            migrationBuilder.Sql(@"
EXEC sp_rename N'dbo.meter',   N'meter_old';
EXEC sp_rename N'dbo.vehicle', N'vehicle_old';
EXEC sp_rename N'dbo.tank',    N'tank_old';
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
FROM sys.key_constraints WHERE type = 'PK' AND OBJECT_NAME(parent_object_id) IN (N'meter_old', N'vehicle_old', N'tank_old');
EXEC sp_executesql @sql;");

            // ---------- 2. asset: kind + active flag ----------
            migrationBuilder.AddColumn<string>(name: "asset_type", table: "asset", type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "general");
            migrationBuilder.AddColumn<bool>(name: "is_active", table: "asset", type: "bit", nullable: false, defaultValue: true);

            // ---------- 3. spec tables ----------
            migrationBuilder.CreateTable(
                name: "asset_vehicle",
                columns: table => new
                {
                    asset_id = table.Column<long>(type: "bigint", nullable: false),
                    plate_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    capacity_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    registration_expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    vendor_id = table.Column<long>(type: "bigint", nullable: true),
                    default_driver_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_asset_vehicle", x => x.asset_id));

            migrationBuilder.CreateTable(
                name: "asset_meter",
                columns: table => new
                {
                    asset_id = table.Column<long>(type: "bigint", nullable: false),
                    meter_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    meter_type_id = table.Column<long>(type: "bigint", nullable: true),
                    brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    sim_card_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    calibration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    install_date = table.Column<DateOnly>(type: "date", nullable: true),
                    initial_reading = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    location_note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_asset_meter", x => x.asset_id));

            migrationBuilder.CreateTable(
                name: "asset_tank",
                columns: table => new
                {
                    asset_id = table.Column<long>(type: "bigint", nullable: false),
                    tank_no = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    gas_type_id = table.Column<long>(type: "bigint", nullable: false),
                    capacity_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: false),
                    current_level_litres = table.Column<decimal>(type: "decimal(14,3)", precision: 14, scale: 3, nullable: true),
                    manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    install_date = table.Column<DateOnly>(type: "date", nullable: true),
                    last_inspection_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_inspection_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_asset_tank", x => x.asset_id));

            // ---------- 4. move the data ----------
            migrationBuilder.Sql(@"
CREATE TABLE #mm (old_id bigint PRIMARY KEY, new_id bigint NOT NULL);   -- meter   → asset
CREATE TABLE #mh (old_id bigint PRIMARY KEY, new_id bigint NOT NULL);   -- vehicle → asset
CREATE TABLE #mk (old_id bigint PRIMARY KEY, new_id bigint NOT NULL);   -- tank    → asset

MERGE asset AS tgt
USING (SELECT o.*, CASE WHEN EXISTS (SELECT 1 FROM asset a WHERE a.asset_code = o.meter_no) THEN N'MTR-' + CAST(o.id AS nvarchar(20)) ELSE o.meter_no END AS code
       FROM meter_old o) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (asset_type, asset_code, name_en, name_ar, branch_id, serial_no, status_id, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
VALUES (N'meter', s.code,
        N'Gas meter ' + s.meter_no, N'عداد غاز ' + s.meter_no, s.branch_id, s.serial_no, s.status_id, s.is_active, s.created_at, s.created_by, s.updated_at, s.updated_by, s.is_deleted)
OUTPUT s.id, inserted.id INTO #mm (old_id, new_id);
INSERT INTO asset_meter (asset_id, meter_no, meter_type_id, brand, model, sim_card_no, calibration_date, install_date, initial_reading, location_note)
SELECT m.new_id, o.meter_no, o.meter_type_id, o.brand, o.model, o.sim_card_no, o.calibration_date, o.install_date, o.initial_reading, o.location_note
FROM meter_old o JOIN #mm m ON m.old_id = o.id;

MERGE asset AS tgt
USING (SELECT o.*, CASE WHEN EXISTS (SELECT 1 FROM asset a WHERE a.asset_code = o.plate_no) THEN N'VEH-' + CAST(o.id AS nvarchar(20)) ELSE o.plate_no END AS code
       FROM vehicle_old o) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (asset_type, asset_code, name_en, name_ar, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
VALUES (N'vehicle', s.code,
        N'Vehicle ' + s.plate_no, N'مركبة ' + s.plate_no, s.is_active, s.created_at, s.created_by, s.updated_at, s.updated_by, s.is_deleted)
OUTPUT s.id, inserted.id INTO #mh (old_id, new_id);
INSERT INTO asset_vehicle (asset_id, plate_no, capacity_litres, registration_expiry, vendor_id, default_driver_id)
SELECT m.new_id, o.plate_no, o.capacity_litres, o.registration_expiry, o.vendor_id, o.default_driver_id
FROM vehicle_old o JOIN #mh m ON m.old_id = o.id;

MERGE asset AS tgt USING (SELECT t.*, b.code AS building_code FROM tank_old t LEFT JOIN building b ON b.id = t.building_id) AS s ON 1 = 0
WHEN NOT MATCHED THEN INSERT (asset_type, asset_code, name_en, name_ar, branch_id, building_id, serial_no, status_id, is_active, created_at, created_by, updated_at, updated_by, is_deleted)
VALUES (N'tank', LEFT(N'TK-' + ISNULL(s.building_code + N'-', N'') + s.tank_no + N'-' + CAST(s.id AS nvarchar(20)), 30),
        N'Gas tank ' + s.tank_no, N'خزان غاز ' + s.tank_no, s.branch_id, s.building_id, s.serial_no, s.status_id, s.is_active, s.created_at, s.created_by, s.updated_at, s.updated_by, s.is_deleted)
OUTPUT s.id, inserted.id INTO #mk (old_id, new_id);
INSERT INTO asset_tank (asset_id, tank_no, gas_type_id, capacity_litres, current_level_litres, manufacturer, install_date, last_inspection_date, next_inspection_date)
SELECT m.new_id, o.tank_no, o.gas_type_id, o.capacity_litres, o.current_level_litres, o.manufacturer, o.install_date, o.last_inspection_date, o.next_inspection_date
FROM tank_old o JOIN #mk m ON m.old_id = o.id;

-- every reference → asset id
UPDATE x SET x.meter_id = m.new_id     FROM meter_assignment x JOIN #mm m ON m.old_id = x.meter_id;
UPDATE x SET x.meter_id = m.new_id     FROM meter_reading    x JOIN #mm m ON m.old_id = x.meter_id;
UPDATE x SET x.meter_id = m.new_id     FROM meter_service    x JOIN #mm m ON m.old_id = x.meter_id;
UPDATE x SET x.new_meter_id = m.new_id FROM meter_service    x JOIN #mm m ON m.old_id = x.new_meter_id;
UPDATE x SET x.meter_id = m.new_id     FROM meter_topup      x JOIN #mm m ON m.old_id = x.meter_id;
UPDATE x SET x.meter_id = m.new_id     FROM service_request  x JOIN #mm m ON m.old_id = x.meter_id;
UPDATE x SET x.vehicle_id = m.new_id   FROM refill_collection x JOIN #mh m ON m.old_id = x.vehicle_id;
UPDATE x SET x.vehicle_id = m.new_id   FROM tank_refill       x JOIN #mh m ON m.old_id = x.vehicle_id;
UPDATE x SET x.tank_id = m.new_id      FROM refill_request     x JOIN #mk m ON m.old_id = x.tank_id;
UPDATE x SET x.tank_id = m.new_id      FROM tank_level_history x JOIN #mk m ON m.old_id = x.tank_id;
UPDATE x SET x.tank_id = m.new_id      FROM tank_refill        x JOIN #mk m ON m.old_id = x.tank_id;

-- references without a foreign key (activity log, documents, audit trail)
UPDATE x SET x.entity_id = m.new_id FROM activity_log x JOIN #mm m ON m.old_id = x.entity_id WHERE x.entity_type = N'meter';
UPDATE x SET x.entity_id = m.new_id FROM activity_log x JOIN #mk m ON m.old_id = x.entity_id WHERE x.entity_type = N'tank';
UPDATE x SET x.entity_id = m.new_id FROM activity_log x JOIN #mh m ON m.old_id = x.entity_id WHERE x.entity_type = N'vehicle';
UPDATE x SET x.entity_id = m.new_id FROM document     x JOIN #mm m ON m.old_id = x.entity_id WHERE x.entity_type = N'meter';
UPDATE x SET x.entity_id = m.new_id FROM document     x JOIN #mk m ON m.old_id = x.entity_id WHERE x.entity_type = N'tank';
UPDATE x SET x.entity_id = m.new_id FROM document     x JOIN #mh m ON m.old_id = x.entity_id WHERE x.entity_type = N'vehicle';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail      x JOIN #mm m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Meter';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail      x JOIN #mk m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Tank';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail      x JOIN #mh m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Vehicle';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail_link x JOIN #mm m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Meter';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail_link x JOIN #mk m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Tank';
UPDATE x SET x.entity_id = CAST(m.new_id AS nvarchar(100)) FROM audit_trail_link x JOIN #mh m ON CAST(m.old_id AS nvarchar(100)) = x.entity_id WHERE x.entity_type = N'Vehicle';
");

            // ---------- 5. drop the old tables ----------
            migrationBuilder.DropTable(name: "meter_old");
            migrationBuilder.DropTable(name: "vehicle_old");
            migrationBuilder.DropTable(name: "tank_old");

            // ---------- 6. indexes ----------
            migrationBuilder.CreateIndex(name: "ix_asset_type_status", table: "asset", columns: new[] { "asset_type", "status_id" });
            migrationBuilder.CreateIndex(name: "ux_asset_code", table: "asset", column: "asset_code", unique: true, filter: "[asset_code] IS NOT NULL AND [is_deleted] = 0");
            migrationBuilder.CreateIndex(name: "ux_asset_meter_serial_no", table: "asset", column: "serial_no", unique: true,
                filter: "[asset_type] = N'meter' AND [serial_no] IS NOT NULL AND [is_deleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_asset_vehicle_default_driver_id", table: "asset_vehicle", column: "default_driver_id");
            migrationBuilder.CreateIndex(name: "IX_asset_vehicle_vendor_id", table: "asset_vehicle", column: "vendor_id");
            migrationBuilder.CreateIndex(name: "ux_asset_vehicle_plate_no", table: "asset_vehicle", column: "plate_no", unique: true, filter: "[plate_no] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_asset_meter_meter_type_id", table: "asset_meter", column: "meter_type_id");
            migrationBuilder.CreateIndex(name: "ux_asset_meter_meter_no", table: "asset_meter", column: "meter_no", unique: true, filter: "[meter_no] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_asset_tank_gas_type_id", table: "asset_tank", column: "gas_type_id");
            migrationBuilder.CreateIndex(name: "ix_asset_tank_tank_no", table: "asset_tank", column: "tank_no");

            // ---------- 7. foreign keys ----------
            void Fk(string name, string table, string column, string principal, string principalColumn, ReferentialAction onDelete = ReferentialAction.Restrict) =>
                migrationBuilder.AddForeignKey(name, table, column, principal, principalColumn: principalColumn, onDelete: onDelete);

            Fk("FK_asset_vehicle_asset_asset_id", "asset_vehicle", "asset_id", "asset", "id", ReferentialAction.Cascade);
            Fk("FK_asset_vehicle_driver_default_driver_id", "asset_vehicle", "default_driver_id", "driver", "party_id");
            Fk("FK_asset_vehicle_party_vendor_vendor_id", "asset_vehicle", "vendor_id", "party_vendor", "party_id");
            Fk("FK_asset_meter_asset_asset_id", "asset_meter", "asset_id", "asset", "id", ReferentialAction.Cascade);
            Fk("FK_asset_meter_lookup_value_meter_type_id", "asset_meter", "meter_type_id", "lookup_value", "id");
            Fk("FK_asset_tank_asset_asset_id", "asset_tank", "asset_id", "asset", "id", ReferentialAction.Cascade);
            Fk("FK_asset_tank_gas_type_gas_type_id", "asset_tank", "gas_type_id", "gas_type", "id");

            Fk("FK_meter_assignment_asset_meter_meter_id", "meter_assignment", "meter_id", "asset_meter", "asset_id");
            Fk("FK_meter_reading_asset_meter_meter_id", "meter_reading", "meter_id", "asset_meter", "asset_id");
            Fk("FK_meter_service_asset_meter_meter_id", "meter_service", "meter_id", "asset_meter", "asset_id");
            Fk("FK_meter_service_asset_meter_new_meter_id", "meter_service", "new_meter_id", "asset_meter", "asset_id");
            Fk("FK_meter_topup_asset_meter_meter_id", "meter_topup", "meter_id", "asset_meter", "asset_id");
            Fk("FK_service_request_asset_meter_meter_id", "service_request", "meter_id", "asset_meter", "asset_id");
            Fk("FK_refill_collection_asset_vehicle_vehicle_id", "refill_collection", "vehicle_id", "asset_vehicle", "asset_id");
            Fk("FK_tank_refill_asset_vehicle_vehicle_id", "tank_refill", "vehicle_id", "asset_vehicle", "asset_id");
            Fk("FK_refill_request_asset_tank_tank_id", "refill_request", "tank_id", "asset_tank", "asset_id");
            Fk("FK_tank_level_history_asset_tank_tank_id", "tank_level_history", "tank_id", "asset_tank", "asset_id");
            Fk("FK_tank_refill_asset_tank_tank_id", "tank_refill", "tank_id", "asset_tank", "asset_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("AssetsUnified cannot be rolled back automatically: meters, vehicles and tanks were merged into asset. Restore a database backup taken before this migration.");
        }
    }
}
