# Sedra Petrol: Database Schema v2.2

**Files**
- `sedra_schema_v2.2.dbml`: **the master copy of the schema**. Paste it into [dbdiagram.io](https://dbdiagram.io) to get the ER diagram.
- `sedra_schema_v2.2.sql`: the PostgreSQL `CREATE TABLE` script generated from the DBML (71 tables, all foreign keys).

> Change the DBML first, then regenerate the diagram and the SQL. Never edit the image directly.

---

## 1. Conventions

| Rule | Detail |
|---|---|
| Primary keys | `id bigint` identity. Business numbers (`B-001`, `GSC-2026-0001`) are separate `UNIQUE` columns generated from `number_sequence`. |
| Audit columns | **Every table:** `created_at`, `created_by`, `updated_at`, `updated_by`, `is_deleted`. Left out of the DBML to keep it readable. Add them in a base migration or base entity. |
| Lookups | Any `*_type_id` or `*_status_id` column points to `lookup_value.id`. Validate the `lookup_type` code in the API. |
| Bilingual | `name_en` / `name_ar` pairs |
| Money | `decimal(14,2)` + `currency_id` + `exchange_rate`. Quantities `decimal(14,3)`, rates `decimal(18,6)`. |
| Branch | `branch_id` only on top-level records (building, tenant, meter, tank, contract, invoice, payment, bills, journal). Child records inherit the branch from their parent. |
| Soft delete | `is_deleted = true`. Never physically delete financial rows. |
| Reserved word | The users table is `app_user`, because `user` is reserved in PostgreSQL. |

---

## 2. Tables by domain (71)

| # | Domain | Tables |
|---|---|---|
| 1 | Organization & location | company, country, emirate, city, branch |
| 2 | Lookups | lookup_type, lookup_value |
| 3 | Parties, vendors & transport | party, party_contact, party_bank_account, vendor, vendor_contact, **driver**, vehicle |
| 4 | Users & security | app_user, role, permission, user_role, role_permission, user_branch |
| 5 | Buildings & units | building, building_owner, unit |
| 6 | Tenants & supply contracts | tenant, supply_contract, contract_fee |
| 7 | Meters | meter, meter_assignment, meter_reading, meter_service, meter_topup |
| 8 | Tanks & refill | gas_type, tank, tank_level_history, refill_request, refill_stage_history, refill_po, refill_supplier_payment, **refill_collection**, refill_purchase_invoice, refill_schedule, **tank_refill** |
| 9 | AMC contracts | amc_contract, amc_scope_item, amc_term, amc_payment_schedule |
| 10 | Maintenance visits | checklist_template, checklist_template_item, maintenance_visit, visit_checklist_item, visit_finding |
| 11 | Service requests | service_request |
| 12 | Civil Defense | cd_certificate, cd_renewal |
| 13 | Billing: tenants (AR) | invoice, invoice_item, payment, payment_allocation |
| 14 | Vendor bills (AP) | vendor_bill, vendor_bill_item, vendor_payment, vendor_payment_allocation |
| 15 | Accounting (GL) | **currency, fiscal_period, account, account_mapping, journal_entry, journal_line** |
| 16 | Documents & audit | document, activity_log, **number_sequence** |

---

## 3. Refill workflow: which table stores each stage

| Stage | Name | Table |
|---|---|---|
| 0 | Request | `refill_request` |
| 1 | Purchase order | `refill_po` |
| 2 | Supplier payment | `refill_supplier_payment` → `vendor_payment` |
| 3 | Collection at supplier | `refill_collection` |
| 4 | Purchase invoice | `refill_purchase_invoice` → `vendor_bill` |
| 5 | Schedule + Civil Defense confirmation | `refill_schedule` |
| 6 | Tank refill | `tank_refill` → inserts `tank_level_history` and updates `tank.current_level_litres` |
| 7 | Management approval | `refill_request.approved_by / approved_at` |
| 8 | Completed | `refill_request.completed_at` |

Every stage change also writes a row to `refill_stage_history`.

---

## 4. Money flow (AR / AP → GL)

```
Tenant side (AR)                       Vendor side (AP)
meter_reading ─┐                       amc_payment_schedule ─┐
contract_fee ──┼─► invoice/item        refill_po ────────────┼─► vendor_bill/item
               │         ▲                                   │          ▲
payment ─► payment_allocation          vendor_payment ─► vendor_payment_allocation
      │                                      │
      └──────────────► journal_entry ◄───────┘   (accounts chosen by account_mapping)
                            └─► journal_line (dimensions: building, unit, tenant, vendor, contract)
```

**Rules**
- A journal entry can be posted only if `SUM(debit) = SUM(credit)` and its `fiscal_period` is open.
- `meter_reading.invoice_id` is set when the reading is billed, so the same reading is never billed twice.
- VAT: `invoice_item.vat_rate` defaults to 5%, and the VAT account comes from `account_mapping` (source_type `vat`).
- Balances (a contract's balance, overdue amounts, amounts owed to a vendor) are **computed in views**, never stored.

---

## 5. Required indexes & constraints (beyond the DBML)

```sql
-- one open meter assignment per meter and per unit
CREATE UNIQUE INDEX ux_meter_assign_meter ON meter_assignment(meter_id) WHERE to_date IS NULL;
CREATE UNIQUE INDEX ux_meter_assign_unit  ON meter_assignment(unit_id)  WHERE to_date IS NULL;
-- one active supply contract per unit (use the id of the 'active' lookup_value)
CREATE UNIQUE INDEX ux_contract_active_unit ON supply_contract(unit_id) WHERE is_active AND NOT is_deleted;
-- one primary owner per building
CREATE UNIQUE INDEX ux_building_primary_owner ON building_owner(building_id) WHERE is_primary;
-- journal line: debit XOR credit
ALTER TABLE journal_line ADD CHECK ((debit = 0) <> (credit = 0));
-- tank level within capacity: enforce in the API (it is a cross-table rule)
```

---

## 6. Suggested views

| View | Used by |
|---|---|
| `v_building_summary` | Buildings list and KPIs: total, occupied and vacant units, meters, tank level %, open requests |
| `v_contract_balance` | Contract drawer: invoiced, paid, balance, overdue |
| `v_meter_current` | Meter drawer: current unit, current tenant, last reading |
| `v_vendor_balance` | AP: billed, paid, outstanding per vendor |
| `v_expiring_items` | Alerts for CD certificates, AMC contracts, supply contracts, meter calibration, driver licenses, tank inspections |

---

## 7. Changes from v2.1

1. Restored **currency, account, account_mapping and fiscal_period**, which v2.1 had dropped.
2. Fixed mislabeled tables: VENDOR_BILL_ITEM (was a second VENDOR_BILL), FISCAL_PERIOD (was a second VENDOR_PAYMENT), ACCOUNT_MAPPING (was VENDOR_PAYMENT_ALLOCATION). Added the real `vendor_payment_allocation`.
3. `journal_entry` is now a real header with no debit or credit. The lines are in `journal_line`.
4. Added **driver**. `vehicle.default_driver_id` and the refill tables point to it.
5. Restored **tank_refill** (stage 6) and added **refill_collection** (stage 3).
6. Removed the duplicate `refill_stage_history`.
7. Refill payment and invoice stages now point to `vendor_payment` and `vendor_bill`, so money is recorded only once.
8. `amc_payment_schedule` now points to `vendor_bill`.
9. `gas_type` is kept as a table (it has `litre_to_m3`) and removed from the lookup types.
10. `visit_checklist_item.is_done` added. `contract_fee` has effective_from and effective_to.
11. Added **number_sequence**, and journal lines have analysis columns (building, unit, tenant, vendor, contract).
12. `party_document` and `contract_document` are merged into the single `document` table.
13. **Branding:** `company` now has `logo_path`, `logo_dark_path`, `stamp_path`, `phone`, `email` and `website`. `branch.logo_path` optionally overrides the company logo. The database stores only the file path/URL; the image itself goes in file storage (a server folder or cloud storage).

---

## 8. Open business questions

1. Contract numbering: should the `GC-…` and `GSC-…` numbers in the UI become one scheme (`GSC-YYYY-NNNN`)?
2. Is unit rent information only, or billed?
3. Will prepaid meters be used? (`meter_topup` is ready.)
4. Is it one company or several? The schema already supports several through `company_id` on branch, user, account and fiscal_period.
5. Are refill tankers your own or the supplier's? Both are supported (`vehicle.vendor_id` null = your own).
