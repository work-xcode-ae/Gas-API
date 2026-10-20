# Sedra Petrol: Database Schema v2.3

**Files**
- `sedra_schema_v2.3.dbml`: **the master copy of the schema**. Paste it into [dbdiagram.io](https://dbdiagram.io) to get the ER diagram.
- `sedra_schema_v2.3.sql`: PostgreSQL script generated from the DBML (102 tables, 432 foreign keys, business-rule indexes, reminder seed). Tested on PostgreSQL 16.
- `sedra_schema_v2.3_sqlserver.sql`: SQL Server script generated the same way as v2.2 (nvarchar, audit columns, filtered unique indexes). It creates a **new** database `SedraPetrol_v23`; upgrading an existing v2.2 database needs a migration.

> Change the DBML first, then regenerate the diagram and the SQL. The v2.2 files are kept unchanged next to these.

---

## 1. What changed in v2.3 (summary)

| # | Change | Tables |
|---|---|---|
| 1 | **Party is the master** of every person, organisation and holding place. Party types: company, individual, employee, supplier, customer, **stock**, **repository**. | `party` |
| 2 | **Party extensions** instead of separate masters: employee details, store details. Supplier details = `vendor`, customer details = `tenant` — both now point to `party`. | `party_employee`, `party_store`, `vendor.party_id`, `tenant.party_id` |
| 3 | **`driver` table removed.** A driver is an employee (`party_employee`) with designation **DRIVER**. Supplier drivers are employees whose `employer_party_id` = the supplier. | `vehicle.default_driver_party_id`, `refill_collection.driver_party_id`, `tank_refill.driver_party_id` |
| 4 | **Asset master** for everything the company owns, held by a custodian party (company / employee / stock / repository). Vehicle, tank and meter link to it. | `asset`, `asset_custody_history`, `vehicle.asset_id`, `tank.asset_id`, `meter.asset_id` |
| 5 | **One document table for every attachment** with type, number, issue/expiry date and **reminders (1 year / 1 month / 1 week before)**. No other table stores file paths. | `document_type`, `document`, `document_file`, `reminder_offset`, `document_type_reminder`, `document_reminder` |
| 6 | **Inventory**: item list (linked to assets), current qty, transactions/transfers, history, stock count. | `item`, `item_stock`, `stock_transaction`, `stock_transaction_line`, `stock_ledger`, `stock_count`, `stock_count_line` |
| 7 | **Sales & purchasing**: quotation, sales order, purchase order, delivery note (outbound DN + inbound GRN), material request. | 10 tables (header + lines) |
| 8 | **Accounting vouchers (DR/CR)**: payment, receipt, journal, debit note, credit note, petty cash. | `voucher`, `voucher_line` |
| 9 | **Customer requests** (refund, cancel, …) with approval, status and field operations. | `customer_request`, `customer_request_operation`, `approval_rule`, `approval_step` |

Table count: 71 − 1 (driver) + 32 new = **102**.

---

## 2. Conventions (unchanged from v2.2)

| Rule | Detail |
|---|---|
| Primary keys | `id bigint` identity. Business numbers are separate `UNIQUE` columns from `number_sequence`. New prefixes: `P EMP AST ITM ST SC QT SO DN GRN MR PV RV JV DBN CRN CR`. |
| Audit columns | Every table: `created_at`, `created_by`, `updated_at`, `updated_by`, `is_deleted`. |
| Lookups | `*_type_id` / `*_status_id` → `lookup_value.id`. New lookup types: DEPARTMENT, DESIGNATION, EMPLOYMENT_STATUS, ASSET_CATEGORY, ASSET_STATUS, DEPRECIATION_METHOD, ITEM_TYPE, ITEM_CATEGORY, UOM, STOCK_TXN_TYPE, STOCK_TXN_STATUS, STOCK_REASON, QUOTATION_STATUS, SO_STATUS, PO_STATUS, DN_DIRECTION, DN_STATUS, MR_STATUS, VOUCHER_TYPE, VOUCHER_STATUS, CUSTOMER_REQUEST_TYPE, CUSTOMER_REQUEST_STATUS, REQUEST_CHANNEL, REQUEST_REASON, NEXT_ACTION, APPROVAL_DECISION, DOCUMENT_CATEGORY, DOCUMENT_STATUS. (DOCUMENT_TYPE moved to its own table.) |
| Money / qty | `decimal(14,2)` money, `decimal(14,3)` quantities, `decimal(14,4)` unit prices, `decimal(18,6)` rates. |
| Soft delete | Never physically delete financial or stock rows. |

---

## 3. Party master & extensions

```
                         party  (master: party_type)
   ┌──────────────┬──────────────┼──────────────┬───────────────┐
party_employee   vendor        tenant        party_store     (company / individual:
(employee)     (supplier)    (customer)   (stock/repository)   no extension needed)
   │ designation = DRIVER → used by vehicle, refill_collection, tank_refill, delivery_note
```

| Party type | Extension table | Example |
|---|---|---|
| company | – | Sedra Petrol itself, a building-owner company |
| individual | – | Building owner (person) |
| employee | `party_employee` | Driver, technician, storekeeper, accountant |
| supplier | `vendor` | Gas supplier, AMC vendor |
| customer | `tenant` | Tenant, or any walk-in customer buying items |
| stock | `party_store` | Main warehouse of a branch |
| repository | `party_store` | Site store, van store |

- `party.is_internal = true` for our own company, employees and stores.
- `party.parent_party_id`: a store belongs to the company; a supplier's employee belongs to the supplier.
- Identity **numbers** (Emirates ID, trade licence) stay on `party` for searching. The **scan, issue and expiry dates** are documents.
- `vendor` and `tenant` keep their existing columns for now so the current API keeps working; name/phone/email should be read from `party` going forward and the duplicates dropped in v2.4.

---

## 4. Documents & expiry reminders

```
document_type ──< document_type_reminder >── reminder_offset (1Y, 1M, 1W …)
      │                                              │
      └──< document ──< document_file                │
               └──< document_reminder >──────────────┘
```

- **One `document` row = one business document** (e.g. an Emirates ID). Its scans (front, back, pages) are rows in `document_file`.
- **Owner:** `entity_type` + `entity_id` work for any table. For the two masters there is also a real foreign key: `party_id` (EID, passport, visa, driving licence, hazmat permit, trade licence…) and `asset_id` (vehicle registration/Mulkiya, insurance, warranty…). A check constraint makes sure `party_id = entity_id` when `entity_type = 'party'` (same for asset).
- **Reminders:** `reminder_offset` holds the options (seeded: 1 year, 1 month, 1 week before; you can add more). Each `document_type` has default reminders; when a document is saved they are copied to `document_reminder` with `remind_on = expiry_date − offset`. The user can add or remove reminders per document. If `expiry_date` changes, `remind_on` is recalculated and `is_sent` reset.
- **Daily job:** sends reminders where `remind_on <= today AND NOT is_sent AND NOT is_dismissed`, and updates `document.status_id` (valid → expiring → expired).
- **Renewal:** a new document row with `renewed_from_id` = the old one; the old row gets `is_current = false`, status *renewed*, and its pending reminders are dismissed.
- **All attachments moved here:** company logo / dark logo / stamp and branch logo (`COMPANY_LOGO`, `COMPANY_LOGO_DARK`, `COMPANY_STAMP` types), driver licence and vehicle registration expiry (were columns on `driver` / `vehicle`), contract copies, CD certificate scans, visit photos, etc.
- `v_expiring_items` is now simply `document` + `document_reminder`, plus the operational dates that stay on their own tables (tank inspection, meter calibration, CD certificate, AMC and supply-contract end dates).

**Suggested document types**

| Code | Applies to | Number | Expiry | Default reminders |
|---|---|---|---|---|
| EID | party | ✓ | ✓ | 1M, 1W |
| PASSPORT / VISA / LABOUR_CARD | party | ✓ | ✓ | 1Y (passport), 1M, 1W |
| DRIVING_LICENSE / HAZMAT_PERMIT | party (employee) | ✓ | ✓ | 1M, 1W |
| TRADE_LICENSE / VAT_CERTIFICATE | party (company, supplier) | ✓ | ✓ | 1M, 1W |
| VEHICLE_REGISTRATION / VEHICLE_INSURANCE | asset | ✓ | ✓ | 1M, 1W |
| WARRANTY | asset | – | ✓ | 1M |
| TITLE_DEED | building | ✓ | – | – |
| CD_CERTIFICATE | building, cd_certificate | ✓ | ✓ | 1Y, 1M, 1W |
| CONTRACT_COPY | supply_contract, amc_contract | – | – | – |
| COMPANY_LOGO / COMPANY_LOGO_DARK / COMPANY_STAMP | company, branch | – | – | – |
| PHOTO / OTHER | any | – | – | – |

---

## 5. Assets

- `asset` is the master of everything the company **owns**: vehicles, tanks, meters, cylinders, equipment, tools, IT, furniture.
- `asset.custodian_party_id` = who holds it now (company, employee, stock, repository). Every hand-over closes the open row in `asset_custody_history` and opens a new one (one open row per asset is enforced by a partial unique index).
- `vehicle`, `tank` and `meter` keep their own operational columns and point to the asset with `asset_id` (one-to-one). Supplier tankers are vehicles **without** an asset.
- `asset.item_id` links to the item list: a serialized item (e.g. a meter model) has one asset row per physical unit.
- Purchase source: `purchase_order_id`, `vendor_bill_id`; depreciation fields are ready for a monthly depreciation run that posts a journal entry.

---

## 6. Inventory

| Screen | Table(s) |
|---|---|
| Item list (connected to assets) | `item` (`is_serialized` items create `asset` rows) |
| Item current qty | `item_stock` — one row per item per location (store or employee), cache updated only on posting |
| Item transfer / item transactions | `stock_transaction` + `stock_transaction_line` |
| Transaction history | `stock_ledger` — append-only, one row per item per location per movement |
| Inventory details (stock count) | `stock_count` + `stock_count_line` → posts an *adjustment* transaction for the differences |

**Transaction types** (`STOCK_TXN_TYPE`) and how `from_party_id` / `to_party_id` are used:

| Type | From | To | Ledger rows |
|---|---|---|---|
| opening / receipt / restock | – | store | 1 in |
| issue / consumption | store or employee | – (or employee for issue) | 1 out (+1 in) |
| transfer_branch | store, branch A | store, branch B | 1 out + 1 in |
| transfer_location | store | store | 1 out + 1 in |
| transfer_employee | employee | employee | 1 out + 1 in |
| return | employee / customer | store | 1 in (+1 out) |
| damage / write_off | store or employee | – | 1 out, journal entry |
| adjustment | – | store | in or out |
| sale_delivery | store | – (customer) | 1 out, from delivery note |

**Posting rules:** status must be *approved*; for each line write ledger rows, update `item_stock` (qty and moving-average cost), move `asset.custodian_party_id` and write `asset_custody_history` for serialized lines; block negative stock unless `party_store.allow_negative_stock`. Damage, write-off and adjustments create a journal entry.

---

## 7. Sales & purchasing flow

```
Sales:      quotation ─► sales_order ─► delivery_note (outbound) ─► invoice
                                            └─► stock_transaction (sale_delivery)
Purchasing: material_request ─► (stock issue/transfer if available)
                     └─► purchase_order ─► delivery_note (inbound = GRN) ─► vendor_bill
                                                 └─► stock_transaction (receipt)
```

- Line-to-line links (`quotation_line_id`, `sales_order_line_id`, `purchase_order_line_id`, `material_request_line_id`) keep `delivered_qty`, `received_qty`, `invoiced_qty`, `billed_qty`, `issued_qty` and `ordered_qty` correct, so partial deliveries and partial bills work.
- `invoice` now has `customer_party_id` (required) and `tenant_id` becomes optional, so items can be sold to customers who are not tenants. `invoice_item.item_id` and `vendor_bill_item.item_id` added.
- `refill_po` stays for the gas refill workflow; general purchases use `purchase_order`.

---

## 8. Accounting vouchers (DR / CR)

- `voucher` header + `voucher_line` (account, party, debit **or** credit, analysis dimensions).
- Types: payment (PV), receipt (RV), journal (JV), debit note, credit note, petty cash.
- For PV/RV the cash/bank side comes from `cash_bank_account_id`.
- Rules: `SUM(debit) = SUM(credit)` before approval; each line is debit XOR credit (check constraint). Posting creates a `journal_entry` with `source_type = 'voucher'` and stores its id on the voucher. Approval goes through `approval_step`.
- `journal_line` now also has `party_id` and `asset_id` as analysis dimensions.

---

## 9. Customer requests

| Table | Holds |
|---|---|
| `customer_request` | Request no, type (refund, cancellation, deposit refund, disconnection, reconnection, transfer of ownership, meter change, billing dispute, complaint, other), customer, contract/unit/meter/invoice/payment references, requested & approved amount, priority, **status**, current approval step, assigned employee, refund voucher, resolution. |
| `customer_request_operation` | Each field action: **visit date, employee, meter, meter reading, issue details, remark, next action (+ note, date)**, status after the visit. |
| `approval_rule` | Which roles approve, in which order, by type and amount range (e.g. refunds above 1,000 AED need Finance Manager). |
| `approval_step` | The actual approvals for any record (customer request, material request, PO, voucher, stock transaction): step, approver, decision, date, remarks. |

Typical refund: *new → under_review → (operation: site visit + final reading) → pending_approval → approved → payment voucher posted → completed → closed.* Every status change is logged in `activity_log`.

---

## 10. Required indexes & constraints (in both SQL scripts)

```sql
-- carried over from v2.2: open meter assignment, active contract per unit, primary owner, journal DR xor CR
CREATE UNIQUE INDEX ux_asset_custody_open ON asset_custody_history(asset_id) WHERE to_date IS NULL;
ALTER TABLE voucher_line ADD CHECK ((debit = 0) <> (credit = 0));
ALTER TABLE stock_transaction_line ADD CHECK (quantity > 0);
ALTER TABLE stock_transaction_line ADD CHECK (asset_id IS NULL OR quantity = 1);
ALTER TABLE stock_ledger ADD CHECK (qty_in >= 0 AND qty_out >= 0 AND (qty_in = 0 OR qty_out = 0));
ALTER TABLE document ADD CHECK (entity_type <> 'party' OR party_id = entity_id);
ALTER TABLE document ADD CHECK (entity_type <> 'asset' OR asset_id = entity_id);
-- enforce in the API: one current document per type per entity when document_type.allow_multiple = false;
-- driver_party_id must be an employee with designation DRIVER; custodian must be company/employee/stock/repository.
```

---

## 11. Impact on the existing API (v2.2 code)

1. Remove the `Driver` entity; replace `DriverId` with `DriverPartyId` on Vehicle, RefillCollection, TankRefill.
2. Add `PartyId` to Vendor and Tenant; create the party row first when creating a vendor or tenant.
3. `Vehicle.RegistrationExpiry`, `Company.LogoPath/LogoDarkPath/StampPath` and `Branch.LogoPath` are gone → read them from `document`.
4. `Invoice.TenantId` becomes nullable and `CustomerPartyId` is required.
5. `DocumentsController` changes to the new document + file + reminder model.
6. Data migration: one `party` row per existing vendor, tenant and driver; driver licence expiry → DRIVING_LICENSE document; vehicle registration expiry → VEHICLE_REGISTRATION document; logos → branding documents.

---

## 12. Open business questions

1. Should one party be allowed several roles (e.g. a supplier that is also a customer)? v2.3 allows it through extensions (a party can have both a `vendor` and a `tenant` row), but `party_type` holds only the main type.
2. Who receives expiry reminders by default — the document owner's manager, a role (HR / Fleet), or both?
3. Inventory valuation: moving average (designed) or FIFO?
4. Should employee-held stock (van stock) be counted as inventory or expensed when issued?
5. Approval limits for refunds, vouchers and purchase orders (amounts and roles) to seed `approval_rule`.
