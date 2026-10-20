using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Employee role of a party — table <c>employee</c> (<c>party_id</c> = <c>party.id</c>): company staff (technicians, readers,
/// drivers, office). Name, Emirates ID, phone, email and the staff ledger account are on <see cref="PartyRole.Party"/>.
/// </summary>
[Table("employee")]
public partial class Employee : PartyRole
{
    [Column("employee_no"), MaxLength(30)]
    public string? EmployeeNo { get; set; }

    [Column("job_title"), MaxLength(100)]
    public string? JobTitle { get; set; }

    /// <summary>Lookup DEPARTMENT.</summary>
    [Column("department_id")]
    public long? DepartmentId { get; set; }

    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("hire_date")]
    public DateOnly? HireDate { get; set; }

    /// <summary>Optional system login of the employee.</summary>
    [Column("user_id")]
    public long? UserId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes"), MaxLength(500)]
    public string? Notes { get; set; }

    // ---- person / company fields, stored on party (flat in the API) ----
    [NotMapped, PartyField(nameof(Entities.Party.NameEn)), MaxLength(200)]
    public string? NameEn { get => Party?.NameEn; set => P.NameEn = value; }

    [NotMapped, PartyField(nameof(Entities.Party.NameAr)), MaxLength(200)]
    public string? NameAr { get => Party?.NameAr; set => P.NameAr = value; }

    [NotMapped, PartyField(nameof(Entities.Party.Phone)), MaxLength(30)]
    public string? Phone { get => Party?.Phone; set => P.Phone = value; }

    [NotMapped, PartyField(nameof(Entities.Party.Email)), MaxLength(150)]
    public string? Email { get => Party?.Email; set => P.Email = value; }

    [NotMapped, PartyField(nameof(Entities.Party.EmiratesId)), MaxLength(30)]
    public string? EmiratesId { get => Party?.EmiratesId; set => P.EmiratesId = value; }

    [ForeignKey(nameof(DepartmentId)), JsonIgnore]
    public LookupValue? Department { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(UserId)), JsonIgnore]
    public AppUser? User { get; set; }
}
