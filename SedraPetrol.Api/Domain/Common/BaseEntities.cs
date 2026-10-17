using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>Audit columns present on every table (see schema conventions).</summary>
public abstract class AuditBase
{
    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    [Column("updated_by")]
    public long? UpdatedBy { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }
}

/// <summary>Base for every table with a bigint identity primary key.</summary>
public abstract class AuditableEntity : AuditBase
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
}
