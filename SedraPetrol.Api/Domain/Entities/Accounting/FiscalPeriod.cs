using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Table <c>fiscal_period</c> (auto-generated from sedra_schema_v2.2_sqlserver.sql).</summary>
[Table("fiscal_period")]
public partial class FiscalPeriod : AuditableEntity
{
    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("year")]
    public short Year { get; set; }

    [Column("period_no")]
    public short PeriodNo { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("is_closed")]
    public bool IsClosed { get; set; }

    // ---- navigation properties (not serialized) ----
    [ForeignKey(nameof(CompanyId)), JsonIgnore]
    public Company? Company { get; set; }
}
