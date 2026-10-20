using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SedraPetrol.Api.Domain.Entities;

/// <summary>
/// Table <c>asset</c> — ONE row for every asset of the company: general equipment, vehicles / tankers, gas meters and building
/// tanks. Code, name, status, branch, building, serial no., purchase data and custodian live here; the fields of one kind of
/// asset live in its spec table (table-per-type, same id):
/// <list type="bullet">
/// <item><c>asset_vehicle</c> → <see cref="Vehicle"/> (cars, tankers)</item>
/// <item><c>asset_meter</c> → <see cref="Meter"/> (gas meters)</item>
/// <item><c>asset_tank</c> → <see cref="Tank"/> (gas tanks at buildings)</item>
/// </list>
/// A row with no spec row is general equipment (<c>asset_type</c> = general).
/// </summary>
[Table("asset")]
public partial class Asset : AuditableEntity
{
    /// <summary>general | vehicle | meter | tank — set by the class.</summary>
    [Column("asset_type"), MaxLength(20), Required]
    public string AssetType { get; protected set; } = AssetTypes.General;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("asset_code"), MaxLength(30)]
    public string? AssetCode { get; set; }

    [Column("name_ar"), MaxLength(150)]
    public string? NameAr { get; set; }

    [Column("name_en"), MaxLength(150)]
    public string? NameEn { get; set; }

    /// <summary>Lookup ASSET_CATEGORY.</summary>
    [Column("category_id")]
    public long? CategoryId { get; set; }

    /// <summary>Lookup ASSET_STATUS.</summary>
    [Column("status_id")]
    public long? StatusId { get; set; }

    [Column("branch_id")]
    public long? BranchId { get; set; }

    [Column("building_id")]
    public long? BuildingId { get; set; }

    [Column("serial_no"), MaxLength(100)]
    public string? SerialNo { get; set; }

    [Column("purchase_date")]
    public DateOnly? PurchaseDate { get; set; }

    [Column("purchase_cost", TypeName = "decimal(18,2)")]
    public decimal? PurchaseCost { get; set; }

    [Column("assigned_employee_id")]
    public long? AssignedEmployeeId { get; set; }

    [Column("warranty_expiry")]
    public DateOnly? WarrantyExpiry { get; set; }

    [Column("notes"), MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(CategoryId)), JsonIgnore]
    public LookupValue? Category { get; set; }

    [ForeignKey(nameof(StatusId)), JsonIgnore]
    public LookupValue? Status { get; set; }

    [ForeignKey(nameof(BranchId)), JsonIgnore]
    public Branch? Branch { get; set; }

    [ForeignKey(nameof(BuildingId)), JsonIgnore]
    public Building? Building { get; set; }

    [ForeignKey(nameof(AssignedEmployeeId)), JsonIgnore]
    public Employee? AssignedEmployee { get; set; }
}

public partial class Asset
{
    /// <summary>Code / name of a new asset when not given (meter no., plate no., tank no.).</summary>
    public virtual void FillDefaults() { }
}

public partial class Meter
{
    public override void FillDefaults()
    {
        AssetCode ??= MeterNo;
        NameEn ??= $"Gas meter {MeterNo}";
        NameAr ??= $"عداد غاز {MeterNo}";
    }
}

public partial class Vehicle
{
    public override void FillDefaults()
    {
        AssetCode ??= PlateNo;
        NameEn ??= $"Vehicle {PlateNo}";
        NameAr ??= $"مركبة {PlateNo}";
    }
}

public partial class Tank
{
    public override void FillDefaults()
    {
        NameEn ??= $"Gas tank {TankNo}";
        NameAr ??= $"خزان غاز {TankNo}";
    }
}

/// <summary>Asset type codes (<c>asset.asset_type</c>).</summary>
public static class AssetTypes
{
    public const string General = "general", Vehicle = "vehicle", Meter = "meter", Tank = "tank";
}
