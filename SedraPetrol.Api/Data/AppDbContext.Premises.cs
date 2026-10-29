using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>
/// Restaurants &amp; Villas module (additive): villa / restaurant sites, their signatories and tanks, gas delivery contracts
/// and the refill → invoice links. No existing table, column, key or index is changed by this module.
/// </summary>
public partial class AppDbContext
{
    /// <summary>Every villa and restaurant. Typed views: <see cref="Villas"/>, <see cref="Restaurants"/>.</summary>
    public DbSet<Premises> Premises => Set<Premises>();
    public DbSet<Villa> Villas => Set<Villa>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<PremisesSignatory> PremisesSignatories => Set<PremisesSignatory>();
    public DbSet<PremisesTank> PremisesTanks => Set<PremisesTank>();
    public DbSet<DeliveryContract> DeliveryContracts => Set<DeliveryContract>();
    public DbSet<PremisesRefillInvoice> PremisesRefillInvoices => Set<PremisesRefillInvoice>();

    /// <summary>
    /// Runs after the global "Restrict everywhere" loop of <c>OnModelCreating</c>, so the delete behaviours set here
    /// apply to the new tables only. Cascades let the existing SQL clean-up scripts (usp_delete_building_data,
    /// delete_all_data, delete_all_buildings_script) keep deleting a building without knowing about these tables.
    /// The application itself never hard-deletes (SaveChanges turns deletes into soft deletes).
    /// </summary>
    private static void ConfigurePremises(ModelBuilder mb)
    {
        const string notDeleted = "[is_deleted] = 0";

        // table-per-type: premises (header) + one spec table per kind, same id
        mb.Entity<Premises>().UseTptMappingStrategy().ToTable("premises");
        mb.Entity<Villa>().ToTable("premises_villa_spec", t => t.Property(e => e.Id).HasColumnName("premises_id"));
        mb.Entity<Restaurant>().ToTable("premises_restaurant_spec", t => t.Property(e => e.Id).HasColumnName("premises_id"));
        // the delivery contract is one more spec table under the existing contract header
        mb.Entity<DeliveryContract>().ToTable("contract_delivery_spec", t => t.Property(e => e.Id).HasColumnName("contract_id"));

        // keys / indexes
        mb.Entity<Premises>().HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_premises_code");
        mb.Entity<Premises>().HasIndex(e => new { e.BuildingId, e.PremisesType }).HasDatabaseName("ix_premises_building_type");
        mb.Entity<Premises>().HasIndex(e => e.UnitId).IsUnique().HasFilter("[unit_id] IS NOT NULL AND " + notDeleted).HasDatabaseName("ux_premises_unit");
        mb.Entity<Premises>().HasIndex(e => e.OwnerPartyId).HasDatabaseName("ix_premises_owner");
        mb.Entity<PremisesSignatory>().HasIndex(e => e.PremisesId).HasDatabaseName("ix_premises_signatory_premises");
        mb.Entity<PremisesTank>().HasIndex(e => e.TankId).IsUnique().HasFilter(notDeleted).HasDatabaseName("ux_premises_tank_tank");
        mb.Entity<PremisesTank>().HasIndex(e => e.PremisesId).HasDatabaseName("ix_premises_tank_premises");
        mb.Entity<DeliveryContract>().HasIndex(e => e.PremisesId).HasDatabaseName("ix_contract_delivery_spec_premises");
        mb.Entity<DeliveryContract>().HasIndex(e => e.CustomerPartyId).HasDatabaseName("ix_contract_delivery_spec_customer");
        mb.Entity<PremisesRefillInvoice>().HasIndex(e => e.RefillRequestId).IsUnique().HasFilter(notDeleted).HasDatabaseName("ux_premises_refill_invoice_refill");
        mb.Entity<PremisesRefillInvoice>().HasIndex(e => e.InvoiceId).HasDatabaseName("ix_premises_refill_invoice_invoice");
        mb.Entity<PremisesRefillInvoice>().HasIndex(e => e.PremisesId).HasDatabaseName("ix_premises_refill_invoice_premises");

        // delete behaviour of the NEW foreign keys only (everything else stays Restrict)
        mb.Entity<Premises>().HasOne(e => e.Building).WithMany().HasForeignKey(e => e.BuildingId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Premises>().HasOne(e => e.Unit).WithMany().HasForeignKey(e => e.UnitId).OnDelete(DeleteBehavior.SetNull);
        mb.Entity<PremisesSignatory>().HasOne(e => e.Premises).WithMany().HasForeignKey(e => e.PremisesId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<PremisesTank>().HasOne(e => e.Premises).WithMany().HasForeignKey(e => e.PremisesId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<PremisesRefillInvoice>().HasOne(e => e.Premises).WithMany().HasForeignKey(e => e.PremisesId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<PremisesRefillInvoice>().HasOne(e => e.Invoice).WithMany().HasForeignKey(e => e.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        // the SQL clean-up scripts delete tanks (step asset_tank) and refill requests BEFORE the building row, so these links must go with them
        mb.Entity<PremisesTank>().HasOne(e => e.Tank).WithMany().HasForeignKey(e => e.TankId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<PremisesRefillInvoice>().HasOne(e => e.RefillRequest).WithMany().HasForeignKey(e => e.RefillRequestId).OnDelete(DeleteBehavior.Cascade);
    }
}
