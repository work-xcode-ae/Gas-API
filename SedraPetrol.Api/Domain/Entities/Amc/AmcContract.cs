using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Annual maintenance contract of a building (client = building owner, provider = our company / a vendor).
/// Header fields (contract no, building, dates, status, notes …) are in <c>contract</c>;
/// the AMC-only fields are in table <c>contract_amc_spec</c> (<c>contract_id</c> = <c>contract.id</c>).
/// </summary>
[Table("contract_amc_spec")]
public partial class AmcContract : Contract
{
    public AmcContract() { ContractType = ContractTypes.Amc; }

    /// <summary>Provider.</summary>
    [Column("vendor_id")]
    public long VendorId { get; set; }

    /// <summary>Client (building owner).</summary>
    [Column("owner_party_id")]
    public long? OwnerPartyId { get; set; }

    [Column("annual_value")]
    [Precision(14, 2)]
    public decimal? AnnualValue { get; set; }

    [Column("visit_frequency_id")]
    public long? VisitFrequencyId { get; set; }

    [Column("visits_total")]
    public short? VisitsTotal { get; set; }

    [Column("response_time_hours")]
    public short? ResponseTimeHours { get; set; }

    [Column("emergency_included")]
    public bool EmergencyIncluded { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(VendorId)), JsonIgnore]
    public Vendor? Vendor { get; set; }

    [ForeignKey(nameof(OwnerPartyId)), JsonIgnore]
    public Party? OwnerParty { get; set; }

    [ForeignKey(nameof(VisitFrequencyId)), JsonIgnore]
    public LookupValue? VisitFrequency { get; set; }
}
