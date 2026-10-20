using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>Receivables tables added for BRD phase 1 (instalments, credit notes, refunds).</summary>
public partial class AppDbContext
{
    public DbSet<InvoiceInstallment> InvoiceInstallments => Set<InvoiceInstallment>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<CreditNoteLine> CreditNoteLines => Set<CreditNoteLine>();
    public DbSet<CreditNoteAllocation> CreditNoteAllocations => Set<CreditNoteAllocation>();
    public DbSet<CustomerRefund> CustomerRefunds => Set<CustomerRefund>();
    public DbSet<CustomerRefundSource> CustomerRefundSources => Set<CustomerRefundSource>();
}
