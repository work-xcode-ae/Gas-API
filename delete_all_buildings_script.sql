/* =====================================================================================================
   Sedra Petrol — delete ALL buildings and everything inside them (plain script).
   Units, tanks, contracts, meter readings/services, visits, service requests, civil-defence records,
   invoices / receipts / credit notes / refunds, supplier bills / payments, vouchers and their journal entries,
   documents and activity log of the buildings. Same rules as delete_building_script.sql, for every building.
   Kept: lookups, settings, users, chart of accounts, vendors, employees, vehicles, meters (back to stock unless
   @DeleteMeters = 1), other journal entries, the audit trail (one "purge" row is added).

   1. Run with @Execute = 0 first: shows how many rows each table would lose, changes nothing.
   2. Set @Execute = 1 and @Confirm = N'DELETE ALL BUILDINGS' to delete.
   ===================================================================================================== */
DECLARE @Confirm       nvarchar(50) = N'';      -- <== type DELETE ALL BUILDINGS to allow @Execute = 1
DECLARE @Execute       bit = 0;                 -- 0 = dry run, 1 = delete
DECLARE @DeleteMeters  bit = 0;                 -- 1 = delete meters used only in buildings, 0 = back to stock
DECLARE @DeleteParties bit = 1;                 -- 1 = delete tenants / owners left with nothing else

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @InStock bigint, @buildings int;
IF @Execute = 1 AND ISNULL(@Confirm, N'') <> N'DELETE ALL BUILDINGS'
    THROW 50002, N'To delete, set @Confirm = N''DELETE ALL BUILDINGS''.', 1;

DROP TABLE IF EXISTS #b;
CREATE TABLE #b (id bigint PRIMARY KEY);   -- all buildings
INSERT INTO #b SELECT id FROM dbo.building;
SET @buildings = @@ROWCOUNT;

SELECT @InStock = v.id
FROM dbo.lookup_value v JOIN dbo.lookup_type t ON t.id = v.lookup_type_id
WHERE t.code = N'METER_STATUS' AND v.code = N'in_stock';

-- table variable: survives the rollback of a dry run
DECLARE @log TABLE (step int, tbl nvarchar(100), action nvarchar(10), rows_affected int);
DECLARE @files TABLE (document_id bigint, entity_type nvarchar(50), entity_id bigint, file_name nvarchar(255), file_path nvarchar(500));
DECLARE @n int;

DROP TABLE IF EXISTS #u, #k, #tk, #rr, #mv, #sr, #cd, #cdr, #m, #mdel, #mopen, #mr, #ms, #inv, #cn, #tn, #pay, #ref, #vb, #vp, #vch, #je, #pt, #doc, #act;

CREATE TABLE #u    (id bigint PRIMARY KEY);   -- units
CREATE TABLE #k    (id bigint PRIMARY KEY);   -- contracts (supply + AMC)
CREATE TABLE #tk   (id bigint PRIMARY KEY);   -- tanks
CREATE TABLE #rr   (id bigint PRIMARY KEY);   -- refill requests
CREATE TABLE #mv   (id bigint PRIMARY KEY);   -- maintenance visits
CREATE TABLE #sr   (id bigint PRIMARY KEY);   -- service requests
CREATE TABLE #cd   (id bigint PRIMARY KEY);   -- civil-defence certificates
CREATE TABLE #cdr  (id bigint PRIMARY KEY);   -- civil-defence renewals
CREATE TABLE #m    (id bigint PRIMARY KEY);   -- meters used in the units
CREATE TABLE #mdel (id bigint PRIMARY KEY);   -- meters to delete (@DeleteMeters = 1)
CREATE TABLE #mopen(id bigint PRIMARY KEY);   -- meters installed in the units now (go back to stock)
CREATE TABLE #mr   (id bigint PRIMARY KEY);   -- meter readings
CREATE TABLE #ms   (id bigint PRIMARY KEY);   -- meter services
CREATE TABLE #inv  (id bigint PRIMARY KEY);   -- customer invoices
CREATE TABLE #cn   (id bigint PRIMARY KEY);   -- credit notes
CREATE TABLE #tn   (id bigint PRIMARY KEY);   -- tenants / customer accounts to delete
CREATE TABLE #pay  (id bigint PRIMARY KEY);   -- receipts
CREATE TABLE #ref  (id bigint PRIMARY KEY);   -- customer refunds
CREATE TABLE #vb   (id bigint PRIMARY KEY);   -- supplier bills
CREATE TABLE #vp   (id bigint PRIMARY KEY);   -- supplier payments
CREATE TABLE #vch  (id bigint PRIMARY KEY);   -- vouchers
CREATE TABLE #je   (id bigint PRIMARY KEY);   -- journal entries
CREATE TABLE #pt   (id bigint PRIMARY KEY);   -- owner parties to delete
CREATE TABLE #doc  (id bigint PRIMARY KEY);   -- documents
CREATE TABLE #act  (id bigint PRIMARY KEY);   -- activity log rows

BEGIN TRY
    BEGIN TRANSACTION;

    -- =============================== what belongs to the building ===============================
    INSERT INTO #u  SELECT id FROM dbo.unit WHERE building_id IN (SELECT id FROM #b);
    INSERT INTO #k  SELECT id FROM dbo.[contract] WHERE building_id IN (SELECT id FROM #b) OR unit_id IN (SELECT id FROM #u);
    INSERT INTO #tk SELECT id FROM dbo.asset WHERE asset_type = N'tank' AND building_id IN (SELECT id FROM #b);
    INSERT INTO #rr SELECT id FROM dbo.refill_request WHERE tank_id IN (SELECT id FROM #tk);
    INSERT INTO #mv SELECT id FROM dbo.maintenance_visit WHERE building_id IN (SELECT id FROM #b) OR amc_contract_id IN (SELECT id FROM #k);
    INSERT INTO #sr SELECT id FROM dbo.service_request WHERE building_id IN (SELECT id FROM #b) OR unit_id IN (SELECT id FROM #u);
    INSERT INTO #cd SELECT id FROM dbo.cd_certificate WHERE building_id IN (SELECT id FROM #b);
    INSERT INTO #cdr SELECT id FROM dbo.cd_renewal WHERE certificate_id IN (SELECT id FROM #cd) OR new_certificate_id IN (SELECT id FROM #cd);

    -- meters
    INSERT INTO #m SELECT DISTINCT meter_id FROM dbo.meter_assignment WHERE unit_id IN (SELECT id FROM #u)
                   UNION SELECT DISTINCT meter_id FROM dbo.meter_reading WHERE unit_id IN (SELECT id FROM #u);
    INSERT INTO #mopen SELECT DISTINCT meter_id FROM dbo.meter_assignment WHERE unit_id IN (SELECT id FROM #u) AND to_date IS NULL;
    IF @DeleteMeters = 1
        INSERT INTO #mdel SELECT m.id FROM #m m
        WHERE NOT EXISTS (SELECT 1 FROM dbo.meter_assignment a WHERE a.meter_id = m.id AND a.unit_id NOT IN (SELECT id FROM #u))
          AND NOT EXISTS (SELECT 1 FROM dbo.meter_reading r WHERE r.meter_id = m.id AND r.unit_id IS NOT NULL AND r.unit_id NOT IN (SELECT id FROM #u))
          AND NOT EXISTS (SELECT 1 FROM dbo.meter_reading r WHERE r.meter_id = m.id AND r.contract_id IS NOT NULL AND r.contract_id NOT IN (SELECT id FROM #k));
    INSERT INTO #mr SELECT id FROM dbo.meter_reading
                    WHERE unit_id IN (SELECT id FROM #u) OR contract_id IN (SELECT id FROM #k) OR meter_id IN (SELECT id FROM #mdel);
    INSERT INTO #ms SELECT id FROM dbo.meter_service WHERE contract_id IN (SELECT id FROM #k) OR meter_id IN (SELECT id FROM #mdel);

    -- customer invoices
    INSERT INTO #inv SELECT id FROM dbo.invoice WHERE building_id IN (SELECT id FROM #b) OR contract_id IN (SELECT id FROM #k)
        UNION SELECT invoice_id FROM dbo.amc_payment_schedule WHERE contract_id IN (SELECT id FROM #k) AND invoice_id IS NOT NULL
        UNION SELECT owner_invoice_id FROM dbo.cd_renewal WHERE id IN (SELECT id FROM #cdr) AND owner_invoice_id IS NOT NULL
        UNION SELECT invoice_id FROM dbo.meter_reading WHERE id IN (SELECT id FROM #mr) AND invoice_id IS NOT NULL
        UNION SELECT invoice_id FROM dbo.invoice_item WHERE meter_reading_id IN (SELECT id FROM #mr)
        UNION SELECT ii.invoice_id FROM dbo.invoice_item ii JOIN dbo.meter_service s ON s.fee_invoice_item_id = ii.id WHERE s.id IN (SELECT id FROM #ms);
    DELETE FROM #inv WHERE id NOT IN (SELECT id FROM dbo.invoice);   -- cd_renewal.owner_invoice_id has no foreign key

    INSERT INTO #cn SELECT id FROM dbo.credit_note
                    WHERE invoice_id IN (SELECT id FROM #inv) OR building_id IN (SELECT id FROM #b) OR contract_id IN (SELECT id FROM #k);

    -- tenants / customer accounts that have nothing left elsewhere
    IF @DeleteParties = 1
        INSERT INTO #tn SELECT c.tenant_id FROM (
                SELECT tenant_id FROM dbo.contract_supply_spec WHERE contract_id IN (SELECT id FROM #k)
                UNION SELECT tenant_id FROM dbo.invoice WHERE id IN (SELECT id FROM #inv)
                UNION SELECT tenant_id FROM dbo.service_request WHERE id IN (SELECT id FROM #sr) AND tenant_id IS NOT NULL
                UNION SELECT tenant_id FROM dbo.credit_note WHERE id IN (SELECT id FROM #cn)) c
        WHERE NOT EXISTS (SELECT 1 FROM dbo.contract_supply_spec x WHERE x.tenant_id = c.tenant_id AND x.contract_id NOT IN (SELECT id FROM #k))
          AND NOT EXISTS (SELECT 1 FROM dbo.invoice x WHERE x.tenant_id = c.tenant_id AND x.id NOT IN (SELECT id FROM #inv))
          AND NOT EXISTS (SELECT 1 FROM dbo.service_request x WHERE x.tenant_id = c.tenant_id AND x.id NOT IN (SELECT id FROM #sr))
          AND NOT EXISTS (SELECT 1 FROM dbo.credit_note x WHERE x.tenant_id = c.tenant_id AND x.id NOT IN (SELECT id FROM #cn))
          AND NOT EXISTS (SELECT 1 FROM dbo.payment p JOIN dbo.payment_allocation a ON a.payment_id = p.id
                          WHERE p.tenant_id = c.tenant_id AND a.invoice_id NOT IN (SELECT id FROM #inv));

    -- receipts: fully allocated to deleted invoices, or of a deleted tenant (advance payments)
    INSERT INTO #pay SELECT p.id FROM dbo.payment p
        WHERE NOT EXISTS (SELECT 1 FROM dbo.payment_allocation a WHERE a.payment_id = p.id AND a.invoice_id NOT IN (SELECT id FROM #inv))
          AND (p.tenant_id IN (SELECT id FROM #tn)
               OR EXISTS (SELECT 1 FROM dbo.payment_allocation a WHERE a.payment_id = p.id AND a.invoice_id IN (SELECT id FROM #inv)));

    -- refunds: of a deleted tenant, or paid only from deleted receipts / credit notes
    INSERT INTO #ref SELECT r.id FROM dbo.customer_refund r
        WHERE r.tenant_id IN (SELECT id FROM #tn)
           OR (EXISTS (SELECT 1 FROM dbo.customer_refund_source s WHERE s.refund_id = r.id)
               AND NOT EXISTS (SELECT 1 FROM dbo.customer_refund_source s WHERE s.refund_id = r.id
                               AND ISNULL(s.payment_id, -1) NOT IN (SELECT id FROM #pay) AND ISNULL(s.credit_note_id, -1) NOT IN (SELECT id FROM #cn)));

    -- supplier bills and payments
    INSERT INTO #vb SELECT vendor_bill_id FROM dbo.amc_payment_schedule WHERE contract_id IN (SELECT id FROM #k) AND vendor_bill_id IS NOT NULL
        UNION SELECT vendor_bill_id FROM dbo.refill_purchase_invoice WHERE refill_request_id IN (SELECT id FROM #rr)
        UNION SELECT id FROM dbo.vendor_bill WHERE source_type = N'amc_contract'   AND source_id IN (SELECT id FROM #k)
        UNION SELECT id FROM dbo.vendor_bill WHERE source_type = N'refill_request' AND source_id IN (SELECT id FROM #rr)
        UNION SELECT id FROM dbo.vendor_bill WHERE source_type = N'cd_renewal'     AND source_id IN (SELECT id FROM #cdr);
    INSERT INTO #vp SELECT vendor_payment_id FROM dbo.refill_supplier_payment WHERE refill_request_id IN (SELECT id FROM #rr)
        UNION SELECT p.id FROM dbo.vendor_payment p
        WHERE EXISTS (SELECT 1 FROM dbo.vendor_payment_allocation a WHERE a.vendor_payment_id = p.id)
          AND NOT EXISTS (SELECT 1 FROM dbo.vendor_payment_allocation a WHERE a.vendor_payment_id = p.id AND a.vendor_bill_id NOT IN (SELECT id FROM #vb));

    INSERT INTO #vch SELECT id FROM dbo.voucher WHERE building_id IN (SELECT id FROM #b);

    -- journal entries of all those documents (a reversal carries the same source, so it is included)
    INSERT INTO #je SELECT journal_entry_id FROM dbo.invoice         WHERE id IN (SELECT id FROM #inv) AND journal_entry_id IS NOT NULL
        UNION SELECT journal_entry_id FROM dbo.payment         WHERE id IN (SELECT id FROM #pay) AND journal_entry_id IS NOT NULL
        UNION SELECT journal_entry_id FROM dbo.credit_note     WHERE id IN (SELECT id FROM #cn)  AND journal_entry_id IS NOT NULL
        UNION SELECT journal_entry_id FROM dbo.customer_refund WHERE id IN (SELECT id FROM #ref) AND journal_entry_id IS NOT NULL
        UNION SELECT journal_entry_id FROM dbo.vendor_bill     WHERE id IN (SELECT id FROM #vb)  AND journal_entry_id IS NOT NULL
        UNION SELECT journal_entry_id FROM dbo.vendor_payment  WHERE id IN (SELECT id FROM #vp)  AND journal_entry_id IS NOT NULL
        UNION SELECT journal_entry_id FROM dbo.voucher         WHERE id IN (SELECT id FROM #vch) AND journal_entry_id IS NOT NULL
        UNION SELECT id FROM dbo.journal_entry WHERE source_id IS NOT NULL AND (
                 (source_type = N'invoice'         AND source_id IN (SELECT id FROM #inv))
              OR (source_type = N'payment'         AND source_id IN (SELECT id FROM #pay))
              OR (source_type = N'credit_note'     AND source_id IN (SELECT id FROM #cn))
              OR (source_type = N'customer_refund' AND source_id IN (SELECT id FROM #ref))
              OR (source_type = N'vendor_bill'     AND source_id IN (SELECT id FROM #vb))
              OR (source_type = N'vendor_payment'  AND source_id IN (SELECT id FROM #vp))
              OR (source_type = N'voucher'         AND source_id IN (SELECT id FROM #vch)));

    -- owners (parties) that own / contract nothing else
    IF @DeleteParties = 1
        INSERT INTO #pt SELECT o.party_id FROM (
                SELECT party_id FROM dbo.building_owner WHERE building_id IN (SELECT id FROM #b)
                UNION SELECT owner_party_id FROM dbo.contract_amc_spec WHERE contract_id IN (SELECT id FROM #k) AND owner_party_id IS NOT NULL) o
        WHERE NOT EXISTS (SELECT 1 FROM dbo.building_owner x WHERE x.party_id = o.party_id AND x.building_id NOT IN (SELECT id FROM #b))
          AND NOT EXISTS (SELECT 1 FROM dbo.contract_amc_spec x WHERE x.owner_party_id = o.party_id AND x.contract_id NOT IN (SELECT id FROM #k));

    -- documents and activity log of everything above
    INSERT INTO #doc SELECT id FROM dbo.document WHERE
           (entity_type = N'building' AND entity_id IN (SELECT id FROM #b))
        OR (entity_type = N'unit' AND entity_id IN (SELECT id FROM #u))
        OR (entity_type IN (N'contract', N'supply_contract', N'amc_contract', N'amc', N'SupplyContract', N'AmcContract') AND entity_id IN (SELECT id FROM #k))
        OR (entity_type = N'tank' AND entity_id IN (SELECT id FROM #tk))
        OR (entity_type = N'refill_request' AND entity_id IN (SELECT id FROM #rr))
        OR (entity_type = N'maintenance_visit' AND entity_id IN (SELECT id FROM #mv))
        OR (entity_type = N'service_request' AND entity_id IN (SELECT id FROM #sr))
        OR (entity_type = N'cd_certificate' AND entity_id IN (SELECT id FROM #cd))
        OR (entity_type LIKE N'cd[_]renewal%' AND entity_id IN (SELECT id FROM #cdr))
        OR (entity_type = N'meter' AND entity_id IN (SELECT id FROM #mdel))
        OR (entity_type = N'invoice' AND entity_id IN (SELECT id FROM #inv))
        OR (entity_type = N'tenant' AND entity_id IN (SELECT id FROM #tn))
        OR (entity_type = N'party' AND entity_id IN (SELECT id FROM #pt));

    INSERT INTO @files SELECT id, entity_type, entity_id, file_name, file_path FROM dbo.document WHERE id IN (SELECT id FROM #doc) AND file_path IS NOT NULL;

    INSERT INTO #act SELECT id FROM dbo.activity_log WHERE building_id IN (SELECT id FROM #b) OR (entity_id IS NOT NULL AND (
           (entity_type = N'building' AND entity_id IN (SELECT id FROM #b))
        OR (entity_type = N'unit' AND entity_id IN (SELECT id FROM #u))
        OR (entity_type IN (N'contract', N'supply_contract', N'amc_contract') AND entity_id IN (SELECT id FROM #k))
        OR (entity_type = N'tank' AND entity_id IN (SELECT id FROM #tk))
        OR (entity_type = N'refill_request' AND entity_id IN (SELECT id FROM #rr))
        OR (entity_type = N'maintenance_visit' AND entity_id IN (SELECT id FROM #mv))
        OR (entity_type = N'service_request' AND entity_id IN (SELECT id FROM #sr))
        OR (entity_type = N'cd_certificate' AND entity_id IN (SELECT id FROM #cd))
        OR (entity_type = N'cd_renewal' AND entity_id IN (SELECT id FROM #cdr))
        OR (entity_type = N'meter' AND entity_id IN (SELECT id FROM #mdel))
        OR (entity_type = N'meter_service' AND entity_id IN (SELECT id FROM #ms))
        OR (entity_type = N'invoice' AND entity_id IN (SELECT id FROM #inv))
        OR (entity_type = N'credit_note' AND entity_id IN (SELECT id FROM #cn))
        OR (entity_type = N'payment' AND entity_id IN (SELECT id FROM #pay))
        OR (entity_type = N'customer_refund' AND entity_id IN (SELECT id FROM #ref))
        OR (entity_type = N'vendor_bill' AND entity_id IN (SELECT id FROM #vb))
        OR (entity_type = N'voucher' AND entity_id IN (SELECT id FROM #vch))
        OR (entity_type = N'journal_entry' AND entity_id IN (SELECT id FROM #je))
        OR (entity_type = N'tenant' AND entity_id IN (SELECT id FROM #tn))));

    -- =============================== delete (children before parents) ===============================
    UPDATE dbo.[journal_line] SET building_id = NULL WHERE building_id IN (SELECT id FROM #b) AND journal_entry_id NOT IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (1, N'journal_line (unlinked building)', N'updated', @n);
    UPDATE dbo.[journal_line] SET unit_id = NULL WHERE unit_id IN (SELECT id FROM #u) AND journal_entry_id NOT IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (2, N'journal_line (unlinked unit)', N'updated', @n);
    UPDATE dbo.[journal_line] SET contract_id = NULL WHERE contract_id IN (SELECT id FROM #k) AND journal_entry_id NOT IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (3, N'journal_line (unlinked contract)', N'updated', @n);
    UPDATE dbo.[journal_line] SET tenant_id = NULL WHERE tenant_id IN (SELECT id FROM #tn) AND journal_entry_id NOT IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (4, N'journal_line (unlinked tenant)', N'updated', @n);
    DELETE FROM dbo.[customer_refund_source] WHERE refund_id IN (SELECT id FROM #ref) OR payment_id IN (SELECT id FROM #pay) OR credit_note_id IN (SELECT id FROM #cn);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (5, N'customer_refund_source', N'deleted', @n);
    DELETE FROM dbo.[customer_refund] WHERE id IN (SELECT id FROM #ref);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (6, N'customer_refund', N'deleted', @n);
    DELETE FROM dbo.[credit_note_allocation] WHERE credit_note_id IN (SELECT id FROM #cn) OR invoice_id IN (SELECT id FROM #inv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (7, N'credit_note_allocation', N'deleted', @n);
    UPDATE dbo.[credit_note_line] SET invoice_item_id = NULL WHERE credit_note_id NOT IN (SELECT id FROM #cn) AND invoice_item_id IN (SELECT id FROM dbo.invoice_item WHERE invoice_id IN (SELECT id FROM #inv));
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (8, N'credit_note_line (unlinked item)', N'updated', @n);
    DELETE FROM dbo.[credit_note_line] WHERE credit_note_id IN (SELECT id FROM #cn);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (9, N'credit_note_line', N'deleted', @n);
    DELETE FROM dbo.[credit_note] WHERE id IN (SELECT id FROM #cn);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (10, N'credit_note', N'deleted', @n);
    DELETE FROM dbo.[payment_allocation] WHERE payment_id IN (SELECT id FROM #pay) OR invoice_id IN (SELECT id FROM #inv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (11, N'payment_allocation', N'deleted', @n);
    DELETE FROM dbo.[meter_topup] WHERE payment_id IN (SELECT id FROM #pay) OR meter_id IN (SELECT id FROM #mdel);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (12, N'meter_topup', N'deleted', @n);
    DELETE FROM dbo.[payment] WHERE id IN (SELECT id FROM #pay);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (13, N'payment', N'deleted', @n);
    DELETE FROM dbo.[amc_payment_schedule] WHERE contract_id IN (SELECT id FROM #k);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (14, N'amc_payment_schedule', N'deleted', @n);
    UPDATE dbo.[amc_payment_schedule] SET invoice_id = NULL WHERE invoice_id IN (SELECT id FROM #inv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (15, N'amc_payment_schedule (unlinked invoice)', N'updated', @n);
    UPDATE dbo.[amc_payment_schedule] SET vendor_bill_id = NULL WHERE vendor_bill_id IN (SELECT id FROM #vb);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (16, N'amc_payment_schedule (unlinked bill)', N'updated', @n);
    DELETE FROM dbo.[meter_service] WHERE id IN (SELECT id FROM #ms);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (17, N'meter_service', N'deleted', @n);
    UPDATE dbo.[meter_service] SET fee_invoice_item_id = NULL WHERE fee_invoice_item_id IN (SELECT id FROM dbo.invoice_item WHERE invoice_id IN (SELECT id FROM #inv));
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (18, N'meter_service (unlinked fee)', N'updated', @n);
    DELETE FROM dbo.[invoice_installment] WHERE invoice_id IN (SELECT id FROM #inv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (19, N'invoice_installment', N'deleted', @n);
    DELETE FROM dbo.[invoice_item] WHERE invoice_id IN (SELECT id FROM #inv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (20, N'invoice_item', N'deleted', @n);
    UPDATE dbo.[invoice_item] SET meter_reading_id = NULL WHERE meter_reading_id IN (SELECT id FROM #mr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (21, N'invoice_item (unlinked reading)', N'updated', @n);
    DELETE FROM dbo.[meter_reading] WHERE id IN (SELECT id FROM #mr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (22, N'meter_reading', N'deleted', @n);
    UPDATE dbo.[meter_reading] SET invoice_id = NULL WHERE invoice_id IN (SELECT id FROM #inv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (23, N'meter_reading (unlinked invoice)', N'updated', @n);
    DELETE FROM dbo.[invoice] WHERE id IN (SELECT id FROM #inv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (24, N'invoice', N'deleted', @n);
    UPDATE dbo.[cd_renewal] SET owner_invoice_id = NULL WHERE owner_invoice_id IN (SELECT id FROM #inv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (25, N'cd_renewal (unlinked invoice)', N'updated', @n);
    DELETE FROM dbo.[vendor_payment_allocation] WHERE vendor_payment_id IN (SELECT id FROM #vp) OR vendor_bill_id IN (SELECT id FROM #vb);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (26, N'vendor_payment_allocation', N'deleted', @n);
    DELETE FROM dbo.[refill_supplier_payment] WHERE refill_request_id IN (SELECT id FROM #rr) OR vendor_payment_id IN (SELECT id FROM #vp);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (27, N'refill_supplier_payment', N'deleted', @n);
    DELETE FROM dbo.[vendor_payment] WHERE id IN (SELECT id FROM #vp);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (28, N'vendor_payment', N'deleted', @n);
    DELETE FROM dbo.[refill_purchase_invoice] WHERE refill_request_id IN (SELECT id FROM #rr) OR vendor_bill_id IN (SELECT id FROM #vb);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (29, N'refill_purchase_invoice', N'deleted', @n);
    DELETE FROM dbo.[vendor_bill_item] WHERE bill_id IN (SELECT id FROM #vb);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (30, N'vendor_bill_item', N'deleted', @n);
    DELETE FROM dbo.[vendor_bill] WHERE id IN (SELECT id FROM #vb);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (31, N'vendor_bill', N'deleted', @n);
    DELETE FROM dbo.[voucher] WHERE id IN (SELECT id FROM #vch);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (32, N'voucher', N'deleted', @n);
    DELETE FROM dbo.[journal_approval] WHERE journal_entry_id IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (33, N'journal_approval', N'deleted', @n);
    UPDATE dbo.[bank_statement_line] SET journal_line_id = NULL WHERE journal_line_id IN (SELECT id FROM dbo.journal_line WHERE journal_entry_id IN (SELECT id FROM #je));
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (34, N'bank_statement_line (unmatched)', N'updated', @n);
    UPDATE dbo.[bank_account] SET opening_journal_id = NULL WHERE opening_journal_id IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (35, N'bank_account (unlinked journal)', N'updated', @n);
    DELETE FROM dbo.[journal_line] WHERE journal_entry_id IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (36, N'journal_line', N'deleted', @n);
    UPDATE dbo.[journal_entry] SET reversed_entry_id = NULL WHERE reversed_entry_id IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (37, N'journal_entry (unlinked reversal)', N'updated', @n);
    DELETE FROM dbo.[journal_entry] WHERE id IN (SELECT id FROM #je);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (38, N'journal_entry', N'deleted', @n);
    DELETE FROM dbo.[visit_checklist_item] WHERE visit_id IN (SELECT id FROM #mv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (39, N'visit_checklist_item', N'deleted', @n);
    DELETE FROM dbo.[visit_finding] WHERE visit_id IN (SELECT id FROM #mv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (40, N'visit_finding', N'deleted', @n);
    UPDATE dbo.[visit_finding] SET service_request_id = NULL WHERE service_request_id IN (SELECT id FROM #sr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (41, N'visit_finding (unlinked request)', N'updated', @n);
    DELETE FROM dbo.[maintenance_visit] WHERE id IN (SELECT id FROM #mv);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (42, N'maintenance_visit', N'deleted', @n);
    DELETE FROM dbo.[service_request] WHERE id IN (SELECT id FROM #sr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (43, N'service_request', N'deleted', @n);
    DELETE FROM dbo.[contract_fee] WHERE contract_id IN (SELECT id FROM #k);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (44, N'contract_fee', N'deleted', @n);
    DELETE FROM dbo.[contract_term] WHERE contract_id IN (SELECT id FROM #k);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (45, N'contract_term', N'deleted', @n);
    DELETE FROM dbo.[amc_scope_item] WHERE contract_id IN (SELECT id FROM #k);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (46, N'amc_scope_item', N'deleted', @n);
    DELETE FROM dbo.[contract_supply_spec] WHERE contract_id IN (SELECT id FROM #k);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (47, N'contract_supply_spec', N'deleted', @n);
    DELETE FROM dbo.[contract_amc_spec] WHERE contract_id IN (SELECT id FROM #k);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (48, N'contract_amc_spec', N'deleted', @n);
    DELETE FROM dbo.[contract] WHERE id IN (SELECT id FROM #k);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (49, N'contract', N'deleted', @n);
    DELETE FROM dbo.[tank_level_history] WHERE tank_id IN (SELECT id FROM #tk);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (50, N'tank_level_history', N'deleted', @n);
    DELETE FROM dbo.[tank_refill] WHERE tank_id IN (SELECT id FROM #tk) OR refill_request_id IN (SELECT id FROM #rr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (51, N'tank_refill', N'deleted', @n);
    DELETE FROM dbo.[refill_collection] WHERE refill_request_id IN (SELECT id FROM #rr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (52, N'refill_collection', N'deleted', @n);
    DELETE FROM dbo.[refill_po] WHERE refill_request_id IN (SELECT id FROM #rr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (53, N'refill_po', N'deleted', @n);
    DELETE FROM dbo.[refill_schedule] WHERE refill_request_id IN (SELECT id FROM #rr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (54, N'refill_schedule', N'deleted', @n);
    DELETE FROM dbo.[refill_stage_history] WHERE refill_request_id IN (SELECT id FROM #rr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (55, N'refill_stage_history', N'deleted', @n);
    DELETE FROM dbo.[refill_request] WHERE id IN (SELECT id FROM #rr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (56, N'refill_request', N'deleted', @n);
    DELETE FROM dbo.[asset_tank] WHERE asset_id IN (SELECT id FROM #tk);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (57, N'asset_tank', N'deleted', @n);
    DELETE FROM dbo.[asset] WHERE id IN (SELECT id FROM #tk);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (58, N'asset (tanks)', N'deleted', @n);
    DELETE FROM dbo.[meter_assignment] WHERE unit_id IN (SELECT id FROM #u) OR meter_id IN (SELECT id FROM #mdel);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (59, N'meter_assignment', N'deleted', @n);
    UPDATE dbo.asset SET status_id = @InStock WHERE @InStock IS NOT NULL AND id IN (SELECT id FROM #mopen) AND id NOT IN (SELECT id FROM #mdel);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (60, N'meter (back to stock)', N'updated', @n);
    UPDATE dbo.[meter_service] SET new_meter_id = NULL WHERE new_meter_id IN (SELECT id FROM #mdel);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (61, N'meter_service (unlinked new meter)', N'updated', @n);
    UPDATE dbo.[service_request] SET meter_id = NULL WHERE meter_id IN (SELECT id FROM #mdel);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (62, N'service_request (unlinked meter)', N'updated', @n);
    DELETE FROM dbo.[asset_meter] WHERE asset_id IN (SELECT id FROM #mdel);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (63, N'asset_meter', N'deleted', @n);
    DELETE FROM dbo.[asset] WHERE id IN (SELECT id FROM #mdel);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (64, N'asset (meters)', N'deleted', @n);
    DELETE FROM dbo.[cd_renewal] WHERE id IN (SELECT id FROM #cdr);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (65, N'cd_renewal', N'deleted', @n);
    DELETE FROM dbo.[cd_certificate] WHERE id IN (SELECT id FROM #cd);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (66, N'cd_certificate', N'deleted', @n);
    DELETE FROM dbo.[building_owner] WHERE building_id IN (SELECT id FROM #b);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (67, N'building_owner', N'deleted', @n);
    UPDATE dbo.[asset] SET building_id = NULL WHERE building_id IN (SELECT id FROM #b);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (68, N'asset (unlinked building)', N'updated', @n);
    DELETE FROM dbo.[cashflow_item] WHERE building_id IN (SELECT id FROM #b);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (69, N'cashflow_item', N'deleted', @n);
    DELETE FROM dbo.[document] WHERE id IN (SELECT id FROM #doc);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (70, N'document', N'deleted', @n);
    DELETE FROM dbo.[activity_log] WHERE id IN (SELECT id FROM #act);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (71, N'activity_log', N'deleted', @n);
    DELETE FROM dbo.[unit] WHERE id IN (SELECT id FROM #u);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (72, N'unit', N'deleted', @n);
    DELETE FROM dbo.[building] WHERE id IN (SELECT id FROM #b);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (73, N'building', N'deleted', @n);
    DELETE FROM dbo.[party_customer] WHERE party_id IN (SELECT id FROM #tn);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (74, N'party_customer', N'deleted', @n);
    DELETE FROM dbo.[party_contact] WHERE party_contact.party_id IN (SELECT id FROM #tn UNION SELECT id FROM #pt) AND NOT EXISTS (SELECT 1 FROM dbo.party_customer r WHERE r.party_id = party_contact.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.party_vendor r WHERE r.party_id = party_contact.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.employee r WHERE r.party_id = party_contact.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.driver r WHERE r.party_id = party_contact.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.building_owner r WHERE r.party_id = party_contact.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.contract_amc_spec r WHERE r.owner_party_id = party_contact.party_id);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (75, N'party_contact', N'deleted', @n);
    DELETE FROM dbo.[party_bank_account] WHERE party_bank_account.party_id IN (SELECT id FROM #tn UNION SELECT id FROM #pt) AND NOT EXISTS (SELECT 1 FROM dbo.party_customer r WHERE r.party_id = party_bank_account.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.party_vendor r WHERE r.party_id = party_bank_account.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.employee r WHERE r.party_id = party_bank_account.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.driver r WHERE r.party_id = party_bank_account.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.building_owner r WHERE r.party_id = party_bank_account.party_id) AND NOT EXISTS (SELECT 1 FROM dbo.contract_amc_spec r WHERE r.owner_party_id = party_bank_account.party_id);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (76, N'party_bank_account', N'deleted', @n);
    DELETE FROM dbo.[party] WHERE party.id IN (SELECT id FROM #tn UNION SELECT id FROM #pt) AND NOT EXISTS (SELECT 1 FROM dbo.party_customer r WHERE r.party_id = party.id) AND NOT EXISTS (SELECT 1 FROM dbo.party_vendor r WHERE r.party_id = party.id) AND NOT EXISTS (SELECT 1 FROM dbo.employee r WHERE r.party_id = party.id) AND NOT EXISTS (SELECT 1 FROM dbo.driver r WHERE r.party_id = party.id) AND NOT EXISTS (SELECT 1 FROM dbo.building_owner r WHERE r.party_id = party.id) AND NOT EXISTS (SELECT 1 FROM dbo.contract_amc_spec r WHERE r.owner_party_id = party.id);
    SET @n = @@ROWCOUNT; INSERT INTO @log (step, tbl, action, rows_affected) VALUES (77, N'party', N'deleted', @n);

    IF @Execute = 1
    BEGIN
        INSERT INTO dbo.audit_trail (occurred_at, transaction_id, user_name, action, table_name, entity_type, entity_id, branch_id, changes)
        SELECT SYSDATETIMEOFFSET(), NEWID(), SUSER_SNAME(), N'purge', N'building', N'Building', N'*', NULL,
               N'{"script":"delete_all_buildings","buildings":' + CAST(@buildings AS nvarchar(20)) + N',"rows":' + CAST(ISNULL((SELECT SUM(rows_affected) FROM @log WHERE action = N'deleted'), 0) AS nvarchar(20)) + N'}';
        COMMIT TRANSACTION;
    END
    ELSE
        ROLLBACK TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- result 1: what was (or, in a dry run, would be) deleted / unlinked
SELECT CASE WHEN @Execute = 1 THEN N'DONE' ELSE N'DRY RUN - nothing changed' END AS mode,
       step, tbl AS [table], action, rows_affected
FROM @log WHERE rows_affected > 0 ORDER BY step;

-- result 2: uploaded files of the deleted documents — remove them from the API storage folder after a real delete
SELECT document_id, entity_type, entity_id, file_name, file_path FROM @files ORDER BY document_id;
