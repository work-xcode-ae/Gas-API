using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

// Auto-generated: one DbSet per table (71).
public partial class AppDbContext
{
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
    public DbSet<AmcTerm> AmcTerms => Set<AmcTerm>();
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
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
}
