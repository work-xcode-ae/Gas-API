using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>meter_reading</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("meter_reading")]
public partial class MeterReading : AuditableEntity
{
    [Column("meter_id")]
    public long MeterId { get; set; }

    [Column("reading_date")]
    public DateTimeOffset ReadingDate { get; set; }

    [Column("reading_value")]
    [Precision(14, 3)]
    public decimal ReadingValue { get; set; }

    [Column("consumption")]
    [Precision(14, 3)]
    public decimal? Consumption { get; set; }

    [Column("method_id")]
    public long? MethodId { get; set; }

    [Column("read_by")]
    public long? ReadBy { get; set; }

    [Column("invoice_id")]
    public long? InvoiceId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>READING_TYPE: opening | periodic | closing | replacement_out | replacement_in | removal.</summary>
    [Column("reading_type_id")]
    public long? ReadingTypeId { get; set; }

    /// <summary>Unit the meter was installed in when the reading was taken.</summary>
    [Column("unit_id")]
    public long? UnitId { get; set; }

    /// <summary>Tenancy (supply contract) the reading belongs to; null while the unit is vacant.</summary>
    [Column("contract_id")]
    public long? ContractId { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(MeterId)), JsonIgnore]
    public Meter? Meter { get; set; }

    [ForeignKey(nameof(MethodId)), JsonIgnore]
    public LookupValue? Method { get; set; }

    [ForeignKey(nameof(ReadBy)), JsonIgnore]
    public AppUser? ReadByUser { get; set; }

    [ForeignKey(nameof(InvoiceId)), JsonIgnore]
    public Invoice? Invoice { get; set; }

    [ForeignKey(nameof(ReadingTypeId)), JsonIgnore]
    public LookupValue? ReadingType { get; set; }

    [ForeignKey(nameof(UnitId)), JsonIgnore]
    public Unit? Unit { get; set; }

    [ForeignKey(nameof(ContractId)), JsonIgnore]
    public SupplyContract? Contract { get; set; }
}
