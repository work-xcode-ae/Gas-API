using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>Dropdown values. Any signed-in user can read them.</summary>
[Route("api/lookups")]
public class LookupsController : ApiControllerBase
{
    /// <summary>All lookup values grouped by type code: { "UNIT_TYPE": [{id, code, label, nameEn, nameAr}], ... }.</summary>
    [HttpGet]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> All([FromQuery] bool includeInactive = false)
    {
        var all = await Lookups.AllAsync();
        return Ok(all.Values.Where(v => includeInactive || v.IsActive).GroupBy(v => v.TypeCode)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.SortOrder).Select(v => new { v.Id, v.Code, label = Loc(v.NameEn, v.NameAr), v.NameEn, v.NameAr, v.ParentId, v.IsActive })));
    }

    /// <summary>Values of one lookup type, e.g. /api/lookups/UNIT_TYPE.</summary>
    [HttpGet("{type}")]
    public async Task<IActionResult> ByType(string type) =>
        Ok((await Lookups.ByTypeAsync(type.ToUpperInvariant())).Where(v => v.IsActive)
            .Select(v => new { v.Id, v.Code, label = Loc(v.NameEn, v.NameAr), v.NameEn, v.NameAr, v.ParentId }));

    /// <summary>Common reference lists for forms in one call: branches, cities, emirates, gas types, currencies.</summary>
    [HttpGet("reference")]
    public async Task<IActionResult> Reference() => Ok(new
    {
        branches = await Db.Branches.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Code).Select(b => new { b.Id, b.Code, b.NameEn, b.NameAr, b.CityId }).ToListAsync(),
        emirates = await Db.Emirates.AsNoTracking().OrderBy(e => e.NameEn).Select(e => new { e.Id, e.Code, e.NameEn, e.NameAr }).ToListAsync(),
        cities = await Db.Cities.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.NameEn).Select(c => new { c.Id, c.Code, c.NameEn, c.NameAr, c.EmirateId }).ToListAsync(),
        gasTypes = await Db.GasTypes.AsNoTracking().Where(g => g.IsActive).Select(g => new { g.Id, g.Code, g.NameEn, g.NameAr, g.LitreToM3 }).ToListAsync(),
        currencies = await Db.Currencies.AsNoTracking().Where(c => c.IsActive).Select(c => new { c.Id, c.Code, c.NameEn, c.NameAr, c.Symbol }).ToListAsync(),
    });
}

[Route("api/lookup-types")]
[PermissionModule("settings")]
public class LookupTypesController : CrudController<LookupType>
{
    protected override Task OnSavedAsync(LookupType entity, string action) { Lookups.Invalidate(); return Task.CompletedTask; }
}

[Route("api/lookup-values")]
[PermissionModule("settings")]
public class LookupValuesController : CrudController<LookupValue>
{
    protected override Task OnSavedAsync(LookupValue entity, string action) { Lookups.Invalidate(); return Task.CompletedTask; }

    protected override async Task OnDeletingAsync(LookupValue entity)
    {
        var type = await Db.LookupTypes.Where(t => t.Id == entity.LookupTypeId).Select(t => t.Code).FirstAsync();
        if (DataSeeder.Lookups.Any(l => l.Type == type && l.Values.Any(v => v.Code == entity.Code)))
            throw new BusinessRuleException("This value is used by the system and cannot be deleted. Deactivate it or rename its labels instead.");
    }

    protected override async Task OnUpdatingAsync(LookupValue existing, LookupValue incoming)
    {
        var type = await Db.LookupTypes.Where(t => t.Id == existing.LookupTypeId).Select(t => t.Code).FirstAsync();
        if (incoming.Code != existing.Code && DataSeeder.Lookups.Any(l => l.Type == type && l.Values.Any(v => v.Code == existing.Code)))
            throw new BusinessRuleException("System lookup codes cannot be changed (the API relies on them). You can change the labels.");
    }
}
