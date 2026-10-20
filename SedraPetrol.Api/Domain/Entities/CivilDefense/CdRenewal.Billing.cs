using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Owner billing of a certificate request / renewal.</summary>
public partial class CdRenewal
{
    /// <summary>Invoice issued to the building owner when the request was opened.</summary>
    [Column("owner_invoice_id")]
    public long? OwnerInvoiceId { get; set; }

    /// <summary>Government fees invoiced to the owner so far (to bill any difference when the actual fees are higher).</summary>
    [Column("invoiced_gov_fees")]
    [Precision(14, 2)]
    public decimal? InvoicedGovFees { get; set; }

    /// <summary>Years covered by the request (invoice = per-year amounts × years).</summary>
    [Column("duration_years")]
    public short? DurationYears { get; set; }
}
