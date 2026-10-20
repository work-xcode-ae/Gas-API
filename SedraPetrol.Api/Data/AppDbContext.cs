using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>
/// Code-first context. The database is created and upgraded by EF Core migrations (Migrations/ folder),
/// applied automatically on startup when Database:MigrateOnStartup is true.
/// Tables / columns follow sedra_schema_v2.2 (explicit [Table] / [Column] names on every entity).
/// </summary>
public partial class AppDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;
    private readonly IHttpContextAccessor? _http;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null, IHttpContextAccessor? http = null) : base(options)
    {
        _currentUser = currentUser;
        _http = http;
    }

    // ======================================================================
    // Tables (71) — one DbSet per table, grouped by domain
    // ======================================================================
    // Organization
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Emirate> Emirates => Set<Emirate>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Branch> Branches => Set<Branch>();
    // Lookups
    public DbSet<LookupType> LookupTypes => Set<LookupType>();
    public DbSet<LookupValue> LookupValues => Set<LookupValue>();
    // Parties
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<PartyContact> PartyContacts => Set<PartyContact>();
    public DbSet<PartyBankAccount> PartyBankAccounts => Set<PartyBankAccount>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorContact> VendorContacts => Set<VendorContact>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    // Security
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    // Buildings
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<BuildingOwner> BuildingOwners => Set<BuildingOwner>();
    public DbSet<Unit> Units => Set<Unit>();
    // Contracts
    public DbSet<Tenant> Tenants => Set<Tenant>();
    /// <summary>Every contract (header). Typed views: <see cref="SupplyContracts"/>, <see cref="AmcContracts"/>.</summary>
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<SupplyContract> SupplyContracts => Set<SupplyContract>();
    public DbSet<ContractFee> ContractFees => Set<ContractFee>();
    // Meters
    public DbSet<Meter> Meters => Set<Meter>();
    public DbSet<MeterAssignment> MeterAssignments => Set<MeterAssignment>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<MeterService> MeterServices => Set<MeterService>();
    public DbSet<MeterTopup> MeterTopups => Set<MeterTopup>();
    // Tanks
    public DbSet<GasType> GasTypes => Set<GasType>();
    public DbSet<Tank> Tanks => Set<Tank>();
    public DbSet<TankLevelHistory> TankLevelHistories => Set<TankLevelHistory>();
    public DbSet<RefillRequest> RefillRequests => Set<RefillRequest>();
    public DbSet<RefillStageHistory> RefillStageHistories => Set<RefillStageHistory>();
    public DbSet<RefillPo> RefillPos => Set<RefillPo>();
    public DbSet<RefillSupplierPayment> RefillSupplierPayments => Set<RefillSupplierPayment>();
    public DbSet<RefillCollection> RefillCollections => Set<RefillCollection>();
    public DbSet<RefillPurchaseInvoice> RefillPurchaseInvoices => Set<RefillPurchaseInvoice>();
    public DbSet<RefillSchedule> RefillSchedules => Set<RefillSchedule>();
    public DbSet<TankRefill> TankRefills => Set<TankRefill>();
    // Amc
    public DbSet<AmcContract> AmcContracts => Set<AmcContract>();
    public DbSet<AmcScopeItem> AmcScopeItems => Set<AmcScopeItem>();
    // Terms & conditions: setup terms (contract_id null) and each contract's terms, in one table
    public DbSet<ContractTerm> ContractTerms => Set<ContractTerm>();
    public DbSet<AmcPaymentSchedule> AmcPaymentSchedules => Set<AmcPaymentSchedule>();
    // Maintenance
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<ChecklistTemplateItem> ChecklistTemplateItems => Set<ChecklistTemplateItem>();
    public DbSet<MaintenanceVisit> MaintenanceVisits => Set<MaintenanceVisit>();
    public DbSet<VisitChecklistItem> VisitChecklistItems => Set<VisitChecklistItem>();
    public DbSet<VisitFinding> VisitFindings => Set<VisitFinding>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    // CivilDefense
    public DbSet<CdCertificate> CdCertificates => Set<CdCertificate>();
    public DbSet<CdRenewal> CdRenewals => Set<CdRenewal>();
    // Billing
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<VendorBill> VendorBills => Set<VendorBill>();
    public DbSet<VendorBillItem> VendorBillItems => Set<VendorBillItem>();
    public DbSet<VendorPayment> VendorPayments => Set<VendorPayment>();
    public DbSet<VendorPaymentAllocation> VendorPaymentAllocations => Set<VendorPaymentAllocation>();
    // Accounting
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountMapping> AccountMappings => Set<AccountMapping>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    // Common
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (!typeof(AuditBase).IsAssignableFrom(clr)) continue;
            // Query filters and column defaults belong to the root of an inheritance tree (contract → supply / amc spec).
            if (entityType.BaseType != null) { foreach (var fk in entityType.GetDeclaredForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict; continue; }

            // Soft delete: hide rows with is_deleted = 1 everywhere (use IgnoreQueryFilters() to see them).
            var p = Expression.Parameter(clr, "e");
            var body = Expression.Equal(Expression.Property(p, nameof(AuditBase.IsDeleted)), Expression.Constant(false));
            modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(body, p));

            modelBuilder.Entity(clr).Property(nameof(AuditBase.CreatedAt)).HasDefaultValueSql("SYSDATETIMEOFFSET()");

            // No cascading deletes: financial rows must never be physically deleted.
            foreach (var fk in entityType.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        ConfigureParties(modelBuilder);
        ConfigureContracts(modelBuilder);
        ConfigureAssets(modelBuilder);
        ConfigureIndexes(modelBuilder);
        // Note: non-nullable defaults (is_active = true, vat_rate = 5 ...) are set by property initialisers
        // in the entity classes. A database DEFAULT on a bool would make EF skip an explicit "false" on insert.
    }

    // ======================================================================
    // Parties: one row per person / company (party) + one table per role, keyed by party_id
    // ======================================================================
    private static void ConfigureParties(ModelBuilder mb)
    {
        mb.Entity<Party>().HasIndex(e => e.PartyCode).IsUnique().HasDatabaseName("ux_party_party_code");
        mb.Entity<Party>().HasIndex(e => e.EmiratesId).IsUnique().HasFilter("[emirates_id] IS NOT NULL AND " + NotDeleted).HasDatabaseName("ux_party_emirates_id");
        mb.Entity<Party>().HasIndex(e => e.AccountId).IsUnique().HasFilter("[account_id] IS NOT NULL").HasDatabaseName("ux_party_account");

        mb.Entity<Tenant>().Property(e => e.Id).HasColumnName("party_id").ValueGeneratedNever();
        mb.Entity<Tenant>().HasOne(e => e.Party).WithOne(p => p.Customer).HasForeignKey<Tenant>(e => e.Id).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Vendor>().Property(e => e.Id).HasColumnName("party_id").ValueGeneratedNever();
        mb.Entity<Vendor>().HasOne(e => e.Party).WithOne(p => p.Vendor).HasForeignKey<Vendor>(e => e.Id).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Employee>().Property(e => e.Id).HasColumnName("party_id").ValueGeneratedNever();
        mb.Entity<Employee>().HasOne(e => e.Party).WithOne(p => p.Employee).HasForeignKey<Employee>(e => e.Id).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Employee>().HasIndex(e => e.UserId).IsUnique().HasFilter("[user_id] IS NOT NULL AND " + NotDeleted).HasDatabaseName("ux_employee_user");
        mb.Entity<Driver>().Property(e => e.Id).HasColumnName("party_id").ValueGeneratedNever();
        mb.Entity<Driver>().HasOne(e => e.Party).WithOne(p => p.Driver).HasForeignKey<Driver>(e => e.Id).OnDelete(DeleteBehavior.Restrict);
        // a role is never used without its person: load the party with it
        mb.Entity<Tenant>().Navigation(e => e.Party).AutoInclude();
        mb.Entity<Vendor>().Navigation(e => e.Party).AutoInclude();
        mb.Entity<Employee>().Navigation(e => e.Party).AutoInclude();
        mb.Entity<Driver>().Navigation(e => e.Party).AutoInclude();
    }

    // ======================================================================
    // Assets: one table for every asset + one spec table per kind (table-per-type, same id)
    // ======================================================================
    private static void ConfigureAssets(ModelBuilder mb)
    {
        mb.Entity<Asset>().UseTptMappingStrategy().ToTable("asset");
        mb.Entity<Vehicle>().ToTable("asset_vehicle", t => t.Property(e => e.Id).HasColumnName("asset_id"));
        mb.Entity<Meter>().ToTable("asset_meter", t => t.Property(e => e.Id).HasColumnName("asset_id"));
        mb.Entity<Tank>().ToTable("asset_tank", t => t.Property(e => e.Id).HasColumnName("asset_id"));
        mb.Entity<Asset>().HasIndex(e => e.AssetCode).IsUnique().HasFilter("[asset_code] IS NOT NULL AND " + NotDeleted).HasDatabaseName("ux_asset_code");
        mb.Entity<Asset>().HasIndex(e => new { e.AssetType, e.StatusId }).HasDatabaseName("ix_asset_type_status");
        // a meter serial number is unique among meters
        mb.Entity<Asset>().HasIndex(e => e.SerialNo).IsUnique().HasFilter("[asset_type] = N'meter' AND [serial_no] IS NOT NULL AND " + NotDeleted).HasDatabaseName("ux_asset_meter_serial_no");
        mb.Entity<Meter>().HasIndex(e => e.MeterNo).IsUnique().HasDatabaseName("ux_asset_meter_meter_no");
        mb.Entity<Vehicle>().HasIndex(e => e.PlateNo).IsUnique().HasDatabaseName("ux_asset_vehicle_plate_no");
        mb.Entity<Tank>().HasIndex(e => e.TankNo).HasDatabaseName("ix_asset_tank_tank_no");
    }

    // ======================================================================
    // Contracts: one header table + one spec table per contract type (table-per-type, same id)
    // ======================================================================
    private static void ConfigureContracts(ModelBuilder mb)
    {
        mb.Entity<Contract>().UseTptMappingStrategy().ToTable("contract");
        // the spec row's key is the contract's id, named contract_id in the spec tables
        mb.Entity<SupplyContract>().ToTable("contract_supply_spec", t => t.Property(e => e.Id).HasColumnName("contract_id"));
        mb.Entity<AmcContract>().ToTable("contract_amc_spec", t => t.Property(e => e.Id).HasColumnName("contract_id"));
        mb.Entity<Contract>().HasIndex(e => e.ContractNo).IsUnique().HasDatabaseName("ux_contract_contract_no");
        mb.Entity<Contract>().HasIndex(e => new { e.ContractType, e.StatusId }).HasDatabaseName("ix_contract_type_status");
        mb.Entity<Contract>().HasIndex(e => e.BuildingId).HasDatabaseName("ix_contract_building");
        // one active contract per unit
        mb.Entity<Contract>().HasIndex(e => e.UnitId).IsUnique().HasFilter("[unit_id] IS NOT NULL AND [is_active] = 1 AND " + NotDeleted).HasDatabaseName("ux_contract_active_unit");
        mb.Entity<SupplyContract>().HasIndex(e => e.TenantId).HasDatabaseName("ix_contract_supply_spec_tenant");
        mb.Entity<AmcContract>().HasIndex(e => e.VendorId).HasDatabaseName("ix_contract_amc_spec_vendor");
    }

    // ======================================================================
    // Indexes & CHECK constraints (ported from sedra_schema_v2.2_sqlserver.sql)
    // Foreign keys come from the [ForeignKey] navigation properties on the entities.
    // ======================================================================
    /// <summary>Soft-deleted rows must not block re-using a value (unit no, plate no, email ...).</summary>
    private const string NotDeleted = "[is_deleted] = 0";

    private static void ConfigureIndexes(ModelBuilder mb)
    {
        mb.Entity<Voucher>().HasIndex(e => e.VoucherNo).IsUnique().HasDatabaseName("ux_voucher_no");
        // ---------- audit trail ----------
        mb.Entity<AuditTrail>().HasIndex(e => e.OccurredAt).HasDatabaseName("ix_audit_trail_occurred");
        mb.Entity<AuditTrail>().HasIndex(e => new { e.TableName, e.EntityId }).HasDatabaseName("ix_audit_trail_entity");
        mb.Entity<AuditTrail>().HasIndex(e => e.UserId).HasDatabaseName("ix_audit_trail_user");
        mb.Entity<AuditTrail>().HasIndex(e => e.TransactionId).HasDatabaseName("ix_audit_trail_tx");

        // ---------- unique business codes (generated by number_sequence or fixed codes) ----------
        mb.Entity<Company>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_company_code");
        mb.Entity<Country>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_country_code");
        mb.Entity<Branch>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_branch_code");
        mb.Entity<LookupType>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_lookup_type_code");
        mb.Entity<Role>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_role_code");
        mb.Entity<Permission>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_permission_code");
        mb.Entity<Building>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_building_code");
        mb.Entity<GasType>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_gas_type_code");
        mb.Entity<RefillRequest>().HasIndex(e => e.RequestNo).IsUnique().HasDatabaseName("ux_refill_request_request_no");
        mb.Entity<RefillPo>().HasIndex(e => e.PoNumber).IsUnique().HasDatabaseName("ux_refill_po_po_number");
        mb.Entity<ServiceRequest>().HasIndex(e => e.RequestNo).IsUnique().HasDatabaseName("ux_service_request_request_no");
        mb.Entity<Invoice>().HasIndex(e => e.InvoiceNo).IsUnique().HasDatabaseName("ux_invoice_invoice_no");
        mb.Entity<Payment>().HasIndex(e => e.ReceiptNo).IsUnique().HasDatabaseName("ux_payment_receipt_no");
        mb.Entity<VendorPayment>().HasIndex(e => e.PaymentNo).IsUnique().HasDatabaseName("ux_vendor_payment_payment_no");
        mb.Entity<Currency>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_currency_code");
        mb.Entity<JournalEntry>().HasIndex(e => e.JournalNo).IsUnique().HasDatabaseName("ux_journal_entry_journal_no");
        mb.Entity<LookupValue>().HasIndex(e => new { e.LookupTypeId, e.Code }).IsUnique().HasDatabaseName("lookup_value_index_0");
        mb.Entity<FiscalPeriod>().HasIndex(e => new { e.CompanyId, e.Year, e.PeriodNo }).IsUnique().HasDatabaseName("fiscal_period_index_5");
        mb.Entity<Account>().HasIndex(e => new { e.CompanyId, e.AccountCode }).IsUnique().HasDatabaseName("account_index_6");
        mb.Entity<NumberSequence>().HasIndex(e => new { e.CompanyId, e.BranchId, e.Prefix, e.Year }).IsUnique().HasDatabaseName("number_sequence_index_8");

        // ---------- one row per refill request (stage tables) ----------
        mb.Entity<RefillPo>().HasIndex(e => e.RefillRequestId).IsUnique().HasDatabaseName("ux_refill_po_refill_request_id");
        mb.Entity<RefillSupplierPayment>().HasIndex(e => e.RefillRequestId).IsUnique().HasDatabaseName("ux_refill_supplier_payment_refill_request_id");
        mb.Entity<RefillCollection>().HasIndex(e => e.RefillRequestId).IsUnique().HasDatabaseName("ux_refill_collection_refill_request_id");
        mb.Entity<RefillPurchaseInvoice>().HasIndex(e => e.RefillRequestId).IsUnique().HasDatabaseName("ux_refill_purchase_invoice_refill_request_id");
        mb.Entity<RefillSchedule>().HasIndex(e => e.RefillRequestId).IsUnique().HasDatabaseName("ux_refill_schedule_refill_request_id");
        mb.Entity<TankRefill>().HasIndex(e => e.RefillRequestId).IsUnique().HasDatabaseName("ux_tank_refill_refill_request_id");

        // ---------- user-entered values: unique among non-deleted rows ----------
        mb.Entity<AppUser>().HasIndex(e => e.Email).IsUnique().HasFilter(NotDeleted).HasDatabaseName("ux_app_user_email");
        mb.Entity<Unit>().HasIndex(e => new { e.BuildingId, e.UnitNo }).IsUnique().HasFilter(NotDeleted).HasDatabaseName("unit_index_1");
        mb.Entity<VendorBill>().HasIndex(e => new { e.VendorId, e.BillNo }).IsUnique().HasFilter(NotDeleted).HasDatabaseName("vendor_bill_index_4");

        // ---------- nullable unique columns ----------
        mb.Entity<Emirate>().HasIndex(e => e.Code).IsUnique().HasFilter("[code] IS NOT NULL").HasDatabaseName("ux_emirate_code");
        mb.Entity<City>().HasIndex(e => e.Code).IsUnique().HasFilter("[code] IS NOT NULL").HasDatabaseName("ux_city_code");
        mb.Entity<MeterService>().HasIndex(e => e.ServiceNo).IsUnique().HasFilter("[service_no] IS NOT NULL").HasDatabaseName("ux_meter_service_service_no");
        mb.Entity<MaintenanceVisit>().HasIndex(e => e.VisitNo).IsUnique().HasFilter("[visit_no] IS NOT NULL").HasDatabaseName("ux_maintenance_visit_visit_no");
        mb.Entity<CdCertificate>().HasIndex(e => e.CertificateNo).IsUnique().HasFilter("[certificate_no] IS NOT NULL").HasDatabaseName("ux_cd_certificate_certificate_no");

        // ---------- business rules ----------
        mb.Entity<MeterAssignment>().HasIndex(e => e.MeterId).IsUnique().HasFilter("[to_date] IS NULL AND " + NotDeleted).HasDatabaseName("ux_meter_assignment_open_meter");
        mb.Entity<MeterAssignment>().HasIndex(e => e.UnitId).IsUnique().HasFilter("[to_date] IS NULL AND " + NotDeleted).HasDatabaseName("ux_meter_assignment_open_unit");
        mb.Entity<BuildingOwner>().HasIndex(e => e.BuildingId).IsUnique().HasFilter("[is_primary] = 1 AND " + NotDeleted).HasDatabaseName("ux_building_owner_primary");

        // ---------- lookups for lists ----------
        mb.Entity<MeterReading>().HasIndex(e => new { e.MeterId, e.ReadingDate }).HasDatabaseName("meter_reading_index_2");
        mb.Entity<MeterReading>().HasIndex(e => new { e.ContractId, e.ReadingDate }).HasDatabaseName("ix_meter_reading_contract");
        mb.Entity<MeterReading>().HasIndex(e => new { e.UnitId, e.ReadingDate }).HasDatabaseName("ix_meter_reading_unit");
        mb.Entity<Document>().HasIndex(e => new { e.EntityType, e.EntityId }).HasDatabaseName("document_index_7");

        // ---------- CHECK constraints ----------
        mb.Entity<JournalLine>().ToTable(t => t.HasCheckConstraint("ck_journal_line_dr_cr", "([debit] = 0 AND [credit] > 0) OR ([credit] = 0 AND [debit] > 0)"));
        mb.Entity<PaymentAllocation>().ToTable(t => t.HasCheckConstraint("ck_payment_allocation_amount", "[amount] > 0"));
        mb.Entity<VendorPaymentAllocation>().ToTable(t => t.HasCheckConstraint("ck_vendor_payment_allocation_amount", "[amount] > 0"));
    }

    // ======================================================================
    // Audit columns + soft delete
    // ======================================================================
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAudit();
        if (_writingTrail) return base.SaveChanges(acceptAllChangesOnSuccess);
        var pending = CaptureTrail();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        if (pending.Count > 0)
        {
            _writingTrail = true;
            try { AuditTrails.AddRange(BuildTrail(pending)); base.SaveChanges(true); }
            finally { _writingTrail = false; }
        }
        return result;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        if (_writingTrail) return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        var pending = CaptureTrail();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (pending.Count > 0)
        {
            _writingTrail = true;
            try { AuditTrails.AddRange(BuildTrail(pending)); await base.SaveChangesAsync(true, cancellationToken); }
            finally { _writingTrail = false; }
        }
        return result;
    }

    // ======================================================================
    // Audit trail: every insert / update / delete of every table (mandatory, automatic)
    // ======================================================================
    private bool _writingTrail;
    private static readonly HashSet<Type> NotAudited = new() { typeof(AuditTrail), typeof(AuditTrailLink), typeof(ActivityLog), typeof(NumberSequence), typeof(RefreshToken) };
    /// <summary>Reference tables that are not worth a history link (every row points to them).</summary>
    private static readonly HashSet<string> NoLinkTypes = new() { "LookupValue", "LookupType", "Branch", "Company", "Currency", "AppUser", "Emirate", "City", "GasType", "FiscalPeriod", "NumberSequence" };
    /// <summary>Second hop: rows that point to a unit / tank are also linked to its building.</summary>
    private static readonly HashSet<string> BuildingHop = new() { "Unit", "Tank" };
    private static readonly HashSet<string> AuditColumns = new() { nameof(AuditBase.CreatedAt), nameof(AuditBase.CreatedBy), nameof(AuditBase.UpdatedAt), nameof(AuditBase.UpdatedBy) };
    private static readonly string[] SecretWords = { "password", "hash", "token", "secret", "salt" };
    private static readonly JsonSerializerOptions TrailJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private sealed record PendingTrail(EntityEntry Entry, string Action, Dictionary<string, object?[]> Changes, string? Id, long? BranchId, List<(string Type, string Id)>? Links);

    /// <summary>Records referenced by an entry through its foreign keys (old and new value when a reference changed).</summary>
    private static List<(string Type, string Id)> RefsOf(EntityEntry e, string action)
    {
        var list = new List<(string Type, string Id)>();
        foreach (var fk in e.Metadata.GetForeignKeys())
        {
            if (fk.Properties.Count != 1) continue;
            var principal = fk.PrincipalEntityType.ClrType.Name;
            if (NoLinkTypes.Contains(principal)) continue;
            var prop = e.Property(fk.Properties[0].Name);
            object? cur = action == "hard_delete" ? prop.OriginalValue : prop.CurrentValue;
            if (cur != null) list.Add((principal, cur.ToString()!));
            if (action == "update" && prop.IsModified && prop.OriginalValue != null && !Equals(prop.OriginalValue, prop.CurrentValue))
                list.Add((principal, prop.OriginalValue.ToString()!));
        }
        return list;
    }

    private static string? KeyOf(EntityEntry e)
    {
        var key = e.Metadata.FindPrimaryKey();
        return key == null ? null : string.Join(",", key.Properties.Select(k => e.Property(k.Name).CurrentValue?.ToString()));
    }
    private static long? BranchOf(EntityEntry e) => e.Metadata.FindProperty("BranchId") != null ? e.Property("BranchId").CurrentValue as long? : null;

    private static object? Safe(string property, object? value) =>
        value != null && SecretWords.Any(w => property.Contains(w, StringComparison.OrdinalIgnoreCase)) ? "***" : value;

    /// <summary>Snapshot the changes before saving (original values are lost after SaveChanges).</summary>
    private List<PendingTrail> CaptureTrail()
    {
        var list = new List<PendingTrail>();
        foreach (var e in ChangeTracker.Entries())
        {
            if (e.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted) || NotAudited.Contains(e.Entity.GetType())) continue;
            var changes = new Dictionary<string, object?[]>();
            string action;
            if (e.State == EntityState.Added)
            {
                action = "insert";
                foreach (var p in e.Properties)
                    if (!AuditColumns.Contains(p.Metadata.Name) && p.Metadata.Name != nameof(AuditBase.IsDeleted) && !p.Metadata.IsPrimaryKey() && p.CurrentValue != null)
                        changes[p.Metadata.Name] = new[] { null, Safe(p.Metadata.Name, p.CurrentValue) };
            }
            else if (e.State == EntityState.Deleted)
            {
                action = "hard_delete";
                foreach (var p in e.Properties)
                    if (!AuditColumns.Contains(p.Metadata.Name) && p.OriginalValue != null)
                        changes[p.Metadata.Name] = new[] { Safe(p.Metadata.Name, p.OriginalValue), null };
            }
            else
            {
                var isDel = e.Entity is AuditBase && e.Property(nameof(AuditBase.IsDeleted)) is { IsModified: true, CurrentValue: true };
                action = isDel ? "delete" : "update";
                foreach (var p in e.Properties)
                {
                    if (!p.IsModified || AuditColumns.Contains(p.Metadata.Name)) continue;
                    if (Equals(p.OriginalValue, p.CurrentValue)) continue;
                    changes[p.Metadata.Name] = new[] { Safe(p.Metadata.Name, p.OriginalValue), Safe(p.Metadata.Name, p.CurrentValue) };
                }
                if (changes.Count == 0) continue; // only audit columns touched
            }
            // ids of new rows are only known after saving; the others are taken now (a hard-deleted entry is detached after saving)
            // references of new rows are only final after saving (new parents get their ids then)
            list.Add(new PendingTrail(e, action, changes, action == "insert" ? null : KeyOf(e), BranchOf(e), action == "insert" ? null : RefsOf(e, action)));
        }
        return list;
    }

    private List<AuditTrail> BuildTrail(List<PendingTrail> pending)
    {
        var ctx = _http?.HttpContext;
        var tx = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var userName = ctx?.User?.Identity?.Name;
        var refs = pending.Select(p => p.Links ?? RefsOf(p.Entry, p.Action)).ToList();
        // second hop: unit / tank → building
        var hopIds = refs.SelectMany(r => r).Where(r => BuildingHop.Contains(r.Type) && long.TryParse(r.Id, out _)).Distinct().ToList();
        var unitIds = hopIds.Where(h => h.Type == "Unit").Select(h => long.Parse(h.Id)).ToList();
        var tankIds = hopIds.Where(h => h.Type == "Tank").Select(h => long.Parse(h.Id)).ToList();
        var unitBuilding = unitIds.Count == 0 ? new Dictionary<long, long>() : Units.IgnoreQueryFilters().AsNoTracking().Where(u => unitIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.BuildingId);
        var tankBuilding = tankIds.Count == 0 ? new Dictionary<long, long>() : Tanks.IgnoreQueryFilters().AsNoTracking().Where(t => tankIds.Contains(t.Id)).Where(t => t.BuildingId != null).ToDictionary(t => t.Id, t => t.BuildingId!.Value);
        return pending.Select((p, idx) =>
        {
            var id = p.Id ?? KeyOf(p.Entry);
            var branch = p.BranchId ?? (p.Action == "insert" ? BranchOf(p.Entry) : null);
            var type = p.Entry.Metadata.ClrType.Name;
            var links = new List<AuditTrailLink>();
            if (id != null) links.Add(new AuditTrailLink { EntityType = type, EntityId = id, IsSelf = true });
            foreach (var r in refs[idx])
            {
                links.Add(new AuditTrailLink { EntityType = r.Type, EntityId = r.Id });
                if (long.TryParse(r.Id, out var rid))
                {
                    if (r.Type == "Unit" && unitBuilding.TryGetValue(rid, out var b1)) links.Add(new AuditTrailLink { EntityType = "Building", EntityId = b1.ToString() });
                    if (r.Type == "Tank" && tankBuilding.TryGetValue(rid, out var b2)) links.Add(new AuditTrailLink { EntityType = "Building", EntityId = b2.ToString() });
                }
            }
            links = links.GroupBy(l => (l.EntityType, l.EntityId)).Select(g => g.OrderByDescending(x => x.IsSelf).First()).ToList();
            return new AuditTrail
            {
                Links = links,
                OccurredAt = now, TransactionId = tx, UserId = _currentUser?.UserId, UserName = userName, BranchId = branch ?? _currentUser?.BranchId,
                Action = p.Action, TableName = p.Entry.Metadata.GetTableName(), EntityType = p.Entry.Metadata.ClrType.Name, EntityId = id,
                Changes = JsonSerializer.Serialize(p.Changes.ToDictionary(c => c.Key, c => new { old = c.Value[0], @new = c.Value[1] }), TrailJson),
                HttpMethod = ctx?.Request.Method, RequestPath = Trim(ctx?.Request.Path.Value, 300),
                IpAddress = Trim(ctx?.Connection.RemoteIpAddress?.ToString(), 64),
            };
        }).ToList();
    }

    private static string? Trim(string? s, int max) => s == null ? null : s.Length <= max ? s : s[..max];

    private void ApplyAudit()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser?.UserId;
        foreach (var entry in ChangeTracker.Entries<AuditBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity is Asset asset) asset.FillDefaults();
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= userId;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedBy).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    // Convert physical deletes into soft deletes: only is_deleted / updated_* are written.
                    entry.State = EntityState.Unchanged;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    entry.Property(x => x.IsDeleted).IsModified = true;
                    entry.Property(x => x.UpdatedAt).IsModified = true;
                    entry.Property(x => x.UpdatedBy).IsModified = true;
                    break;
            }
        }
    }
}
