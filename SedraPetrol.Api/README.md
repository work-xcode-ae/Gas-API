# Sedra Petrol API

ASP.NET Core 8 Web API for the Sedra Petrol portal (buildings, gas supply, meters, tanks & refill,
AMC maintenance, Civil Defense, billing and accounting). It is **database-first**: it maps onto the
`SedraPetrol` SQL Server database created by `../sedra_schema_v2.2_sqlserver.sql` (71 tables). No EF migrations.

| | |
|---|---|
| Framework | .NET 8 (LTS), ASP.NET Core Web API, controllers |
| Data | EF Core 8 + SQL Server (`Microsoft.EntityFrameworkCore.SqlServer 8.0.8`) |
| Auth | JWT bearer, role + permission based |
| Docs | Swagger UI at `/swagger` |

---

## 1. Run it

1. **Create the database**: open `sedra_schema_v2.2_sqlserver.sql` in SSMS and run it (F5).
2. **Connection string**: edit `appsettings.json` → `ConnectionStrings:SedraPetrol`
   (default: `Server=localhost;Database=SedraPetrol;Trusted_Connection=True;TrustServerCertificate=True`).
3. **JWT key**: change `Jwt:Key` (any random string of 32+ characters). In production set it with the
   environment variable `Jwt__Key` instead of the file.
4. Run:
   ```bash
   cd SedraPetrol.Api
   dotnet run --launch-profile http      # http://localhost:5080/swagger
   ```
   or open `SedraPetrol.sln` in Visual Studio / Rider and press F5.
5. **First start seeds the master data** (only inserts what is missing, safe to leave on):
   lookups (all status/type codes), AED/USD, UAE emirates + cities, company *Sedra Petrol*, 5 branches
   (AUH, DXB, SHJ, AJM, FUJ), gas types (LPG, NG), permissions, roles (ADMIN, MANAGER, ACCOUNTANT,
   TECHNICIAN, READER), a starter chart of accounts + account mappings, fiscal periods for this year
   and next, and a preventive-maintenance checklist.
6. **Log in** with the seeded admin: `admin@sedrapetrol.ae` / `Admin@12345` (from `Seed:AdminEmail` /
   `Seed:AdminPassword`). **Change the password** after the first login (`POST /api/auth/change-password`).

In Swagger: call `POST /api/auth/login`, copy `token`, click **Authorize** and paste it.

---

## 2. Conventions for the portal

- **Base URL**: `http://localhost:5080/api`. CORS allows `http://localhost:5173` (Vite) — edit `Cors:Origins`.
- **Auth header**: `Authorization: Bearer <token>`. `401` = not logged in, `403` = missing permission.
- **Language**: send `Accept-Language: ar` (default) or `en` (or `?lang=en`). Fields like `name`,
  `typeLabel`, `statusLabel` come back in that language; `nameEn` / `nameAr` are always included too.
- **Status / type fields** are returned as **codes** (`"active"`, `"overdue"`, `"apartment"` …) — the same
  codes the portal's `STATUS_MAP`s already use. Request bodies accept codes as well (e.g. `"type": "apartment"`).
  All codes and their Arabic/English labels: `GET /api/lookups` (grouped by type) or `GET /api/lookups/{TYPE}`.
- **Lists** return `{ items, total, page, pageSize }`. Query: `?page=1&pageSize=20&search=...&sort=...&desc=true`.
  `pageSize=0` returns everything (max 5000) — handy for dropdowns.
- **Errors** are RFC 7807 JSON: `{ status, title, detail, code }`. Show `detail` to the user.
  `400` validation, `404` not found, `409` business rule (e.g. *"The unit already has an active contract"*).
- **Dates**: `DateOnly` fields are `"yyyy-MM-dd"`; timestamps are ISO-8601 with offset.
- **Branch security**: non-admin users only see records of branches assigned to them (`user_branch`).

Suggested `src/api.ts` for the portal:
```ts
const BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5080/api';
export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(BASE + path, {
    ...init,
    headers: { 'Content-Type': 'application/json', 'Accept-Language': localStorage.getItem('lang') ?? 'ar',
               Authorization: `Bearer ${localStorage.getItem('token') ?? ''}`, ...init.headers },
  });
  if (res.status === 204) return undefined as T;
  const body = await res.json();
  if (!res.ok) throw new Error(body.detail ?? res.statusText);
  return body as T;
}
```

---

## 3. Endpoints by portal page

| Portal page / component | Endpoints |
|---|---|
| Login | `POST /auth/login`, `GET /auth/me`, `POST /auth/change-password` |
| **Dashboard** | `GET /dashboard` (KPI cards, revenue chart, invoice & contract donuts, recent invoices / readings / service requests, refill charts, notifications), `GET /alerts?days=30` |
| **Buildings** list + KPIs | `GET /buildings`, `GET /buildings/kpis` |
| Add building wizard (building → owners → tanks) | `POST /buildings` (one call; owners can be existing `partyId` or a new `party`; optional `units`) |
| **BuildingProfile** header | `GET /buildings/{id}`, `PUT /buildings/{id}`, `PATCH /buildings/{id}/status`, `GET /buildings/{id}/activity` |
| Owners tab | `GET/POST /buildings/{id}/owners`, `PUT/DELETE /buildings/{id}/owners/{ownerId}`, `GET /parties/{id}/details` |
| Units tab / **Apartments** page / AddUnitModal | `GET /units?buildingId=`, `POST /units`, `POST /units/bulk`, `PUT/DELETE /units/{id}` |
| Tenants tab / **Customers** / AddTenantModal / TenantProfileDrawer | `GET /tenants?buildingId=`, `GET /tenants/{id}`, `POST /tenants`, `PUT /tenants/{id}`, `PATCH /tenants/{id}/contact`, `GET /tenants/{id}/statement` |
| **GasContracts** / AddGasContractModal / GasContractDrawer | `GET /supply-contracts`, `GET /supply-contracts/{id}`, `POST /supply-contracts`, `PUT …/{id}`, `POST …/{id}/activate`, `…/terminate`, `…/renew`, fees `GET/POST …/{id}/fees`, `PUT/DELETE …/{id}/fees/{feeId}`, `POST …/{id}/generate-invoice` |
| **Meters** / AddMeterModal / MeterDetailDrawer | `GET /meters?buildingId=`, `GET /meters/{id}`, `POST /meters`, `PUT/DELETE /meters/{id}`, `POST /meters/{id}/assign`, `…/unassign` |
| MeterReadingModal | `POST /meters/{id}/readings`, `GET /meters/{id}/readings`, `GET /meter-readings` (recent), `POST /meter-readings/bulk`, `DELETE /meter-readings/{id}` |
| MeterServiceModal | `GET/POST /meter-services`, `POST /meter-services/{id}/complete`, `…/cancel` |
| Tanks tab | `GET /tanks?buildingId=`, `POST/PUT/DELETE /tanks`, `GET/POST /tanks/{id}/levels` |
| **RefillRequestDrawer** | see §4 |
| **MaintenanceContracts** / AddAmcContractModal | `GET /amc-contracts`, `GET /amc-contracts/{id}`, `POST`, `PUT /{id}`, `PATCH /{id}/status`, `POST /{id}/payment-schedule/{sid}/bill` |
| **Maintenance** (visits) | `GET /maintenance-visits`, `GET /{id}`, `POST`, `PUT /{id}`, `POST /{id}/start`, `POST /{id}/complete`, `…/cancel`, `PUT /{id}/checklist/{itemId}`, `POST /{id}/findings`, `GET /checklist-templates` |
| **ServiceRequests** | `GET /service-requests`, `POST`, `PUT /{id}`, `POST /{id}/assign`, `POST /{id}/status` |
| Civil Defense | `GET /cd-certificates`, `POST/PUT/DELETE`, `POST /cd-certificates/{id}/renewals`, `PUT /cd-certificates/renewals/{rid}`, `POST …/renewals/{rid}/complete` |
| **Invoices** / TenantInvoicesDrawer / NewInvoiceModal | `GET /invoices`, `GET /invoices/{id}`, `POST /invoices`, `PUT /invoices/{id}` (draft), `POST /invoices/{id}/issue`, `…/cancel`, `POST /invoices/generate` (billing run), `GET /invoices/summary` |
| **Payments** / RecordPaymentModal | `GET /payments`, `GET /payments/{id}`, `GET /payments/open-invoices?tenantId=`, `POST /payments`, `POST /payments/{id}/allocate`, `…/cancel` |
| **Overdue** | `GET /invoices/overdue` (`?groupBy=tenant` for one row per tenant), `GET /reports/aging` |
| **Reports** | `GET /reports/consumption`, `/collections`, `/aging`, `/occupancy`, `/refills`, `/vendor-balances`, `/vat` |
| Vendors, drivers, vehicles | `GET /vendors/options?type=gas_supplier`, `GET /vendors/{id}/details`, CRUD `/vendors`, `/vendor-contacts`, `/drivers`, `/vehicles` |
| Vendor bills & payments (A/P) | `/vendor-bills`, `/vendor-payments` (+ `/{id}/cancel`) |
| Accounting | `/accounts` (+ `/accounts/tree`), `/account-mappings`, `/fiscal-periods` (+ `/generate/{year}`, `/{id}/close`, `/{id}/reopen`), `/journal-entries` (+ `/{id}/post`, `/{id}/reverse`), `GET /accounting/trial-balance`, `GET /accounting/ledger/{accountId}` |
| **Users** | `/users` (+ `/{id}/reset-password`), `/roles`, `GET /permissions` |
| **Settings** | `/companies` (+ `POST /companies/{id}/branding/{logo|logo_dark|stamp}`), `/branches`, `/countries`, `/emirates`, `/cities`, `/currencies`, `/gas-types`, `/lookup-types`, `/lookup-values`, `/number-sequences`, `GET /lookups/reference` |
| Attachments (any record) | `GET /documents?entityType=tenant&entityId=5`, `POST /documents` (multipart), `GET /documents/{id}/download`, `DELETE` |

Every remaining table also has a plain CRUD controller (`GET` list with any column as a filter, `GET /{id}`,
`POST`, `PUT /{id}`, `DELETE /{id}` soft delete) — see Swagger for the full list.

---

## 4. Business rules implemented

**Refill workflow** (`/refill-requests`) — each step checks the current stage and writes `refill_stage_history`:

| Stage | Call | Effect |
|---|---|---|
| 0 Request | `POST /refill-requests` `{tankId, requestedQty, priority}` | checks free capacity, one open request per tank |
| 1 Purchase order | `POST /{id}/po` `{vendorId, unitPrice, qty?}` | `refill_po` (PO number auto) |
| 2 Supplier payment | `POST /{id}/payment` `{amount, method, reference}` | creates a `vendor_payment` + journal |
| 3 Collection | `POST /{id}/collection` `{tankerPlate / vehicleId, driverName / driverId, meterBefore, meterAfter}` | vehicle / driver created if new |
| 4 Purchase invoice | `POST /{id}/purchase-invoice` `{invoiceNumber, amount?}` | creates a `vendor_bill` (+ VAT, journal) and applies the stage-2 payment |
| 5 Schedule | `POST /{id}/schedule` `{deliveryDate, timeSlot, civilDefenseConfirmed, civilDefenseRef}` | can be re-sent to reschedule |
| 6 Refill | `POST /{id}/refill` `{levelBefore, levelAfter, …}` | needs Civil Defense confirmation; capacity enforced; `tank_refill` + `tank_level_history` + tank level updated; moves to stage 7 |
| 7→8 Approval | `POST /{id}/approve` | needs permission `tanks.approve_refill` |
| Cancel | `POST /{id}/cancel` | only before the tank was refilled |

`GET /refill-requests` returns the portal's `RefillRequestItem` shape including `stageData`.

**Supply contracts** — one active contract per unit; the unit's meter is linked automatically; unit status
becomes occupied / vacant; `issueInitialInvoice: true` bills one-time fees (connection …) and the deposit.

**Meter readings** — consumption = value − previous reading; lower-than-previous readings are rejected
(`allowReset` for a replaced/rolled-over meter); back-dated readings keep the chain consistent; billed readings are locked.

**Billing** — invoice lines × VAT (default 5 %, `Billing:DefaultVatRate`); due date = issue + `Billing:InvoiceDueDays`.
`POST /invoices/generate {buildingId, periodStart, periodEnd}` bills every active contract: unbilled readings ×
the contract's `gas_price` fee + monthly fees; each reading gets `invoice_id` so it is never billed twice.
Payments are allocated to the given invoice(s) or FIFO to the oldest open invoices; invoice status
(pending / partial / paid / overdue) is recalculated. Balances are computed, never stored.

**Accounting** — invoices, payments, vendor bills and vendor payments create balanced, posted journal entries
using `account_mapping` (seeded). If a mapping is missing the document is still saved and no journal is made.
Posting into a closed fiscal period is refused. Cancelling a document reverses its journal.

**Background job** (every 6 h): marks overdue invoices, expires / auto-renews supply contracts, expires AMC
contracts and Civil Defense certificates, flags low tanks (< 25 %).

**Audit** — `created_*` / `updated_*` are filled automatically; deletes are soft (`is_deleted = 1`) everywhere;
key actions are written to `activity_log`.

**Numbering** — from `number_sequence` with a row lock: `B-001`, `T-0001`, `GSC-2026-0001`, `INV-…`, `RCP-…`,
`RFR-…`, `SR-…`, `AMC-…`, `MV-…`, `MS-…`, `PO-…`, `VP-…`, `JE-…`, `M-0001`, `V-0001`.

---

## 5. Permissions

Each module has `module.view` (GET) and `module.manage` (POST/PUT/PATCH/DELETE; also grants view).
Modules: dashboard, settings, users, parties, vendors, buildings, tenants, contracts, meters, tanks, amc,
maintenance, service_requests, civil_defense, invoices, payments, payables, accounting, documents, reports.
Extra: `invoices.post`, `invoices.cancel`, `payments.cancel`, `tanks.approve_refill`, `accounting.post`,
`accounting.close_period`. The `ADMIN` role passes every check. `GET /auth/me` returns the user's permission
list so the portal can hide menu items.

---

## 6. Project layout

```
SedraPetrol.Api/
  Program.cs                 DI, JWT, CORS, Swagger, seeding
  Domain/Entities/<Domain>/  71 entity classes generated from the SQL script (one per table)
  Data/                      AppDbContext (soft-delete filter, audit columns), DbSets
  Services/                  Lookup, Numbering, Billing, Contract, Meter, Posting (GL), Balances, Alerts, Seeder, DailyJobs, FileStorage
  Security/                  JWT token service, permission catalog + filter
  Controllers/<Domain>/      Buildings, Contracts, Meters, Tanks, Maintenance, Billing, Vendors, Accounting, Security, Settings, Common
  Common/                    errors, ProblemDetails middleware, paging, current user
```

If the schema changes, update the SQL script first, then update the matching entity class in
`Domain/Entities` (the property ↔ column mapping is explicit with `[Column("...")]`).

## 7. Not covered yet

The portal also has **Employees**, **Vehicles (fleet: fines, documents, driver history)** and **Assets** pages
(not in the router yet). Those tables are not in schema v2.2, so there are no endpoints for them — add the
tables to the DBML/SQL first, then controllers can follow the same pattern.
