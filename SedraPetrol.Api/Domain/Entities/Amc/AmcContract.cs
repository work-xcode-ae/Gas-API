using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>amc_contract</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("amc_contract")]
public partial class AmcContract : AuditableEntity
{
    [Column("branch_id")]
    public long BranchId { get; set; }

    [Column("contract_no")]
    [MaxLength(30)]
    [Required]
    public string ContractNo { get; set; } = string.Empty;

    [Column("vendor_id")]
    public long VendorId { get; set; }

    [Column("building_id")]
    public long BuildingId { get; set; }

    [Column("owner_party_id")]
    public long? OwnerPartyId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("annual_value")]
    [Precision(14, 2)]
    public decimal? AnnualValue { get; set; }

    [Column("currency_id")]
    public long? CurrencyId { get; set; }

    [Column("visit_frequency_id")]
    public long? VisitFrequencyId { get; set; }

    [Column("visits_total")]
    public short? VisitsTotal { get; set; }

    [Column("response_time_hours")]
    public short? ResponseTimeHours { get; set; }

    [Column("emergency_included")]
    public bool EmergencyIncluded { get; set; }

    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(OwnerPartyId)), JsonIgnore]
    public Party? OwnerParty { get; set; }

    [ForeignKey(nameof(CurrencyId)), JsonIgnore]
    public Currency? Currency { get; set; }

    [ForeignKey(nameof(VisitFrequencyId)), JsonIgnore]
    public LookupValue? VisitFrequency { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }
}
