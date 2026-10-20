using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Settings → Terms &amp; conditions: the setup rows of <c>contract_term</c> (contract_id NULL).
/// One list for every contract kind; <c>contractType</c> (supply | amc) decides which form uses a term.
/// </summary>
[Route("api/contract-term-templates")]
[PermissionModule("settings")]
public class ContractTermTemplatesController : ApiControllerBase
{
    public static readonly string[] ContractTypes = { "supply", "amc" };
    public static readonly string[] Categories = { "general", "payment", "service", "safety", "liability", "renewal", "dispute", "other" };

    public record TemplateInput(string? ContractType, string? Category, string? TextAr, string? TextEn, bool IsDefault, int? SortOrder, bool? IsActive);
    public record ReorderInput(List<long> Ids);

    private static object Dto(ContractTerm t) => new
    {
        t.Id, t.ContractType, t.Category, t.TextAr, t.TextEn, t.IsDefault, t.SortOrder, t.IsActive, t.CreatedAt, t.UpdatedAt,
    };

    /// <summary>Settings list (active and inactive). ?contractType=supply|amc</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? contractType)
    {
        var q = Db.ContractTerms.AsNoTracking().Where(x => x.ContractId == null);
        if (!string.IsNullOrWhiteSpace(contractType)) q = q.Where(t => t.ContractType == contractType);
        var rows = await q.OrderBy(t => t.ContractType).ThenBy(t => t.SortOrder).ThenBy(t => t.Id).ToListAsync();
        return Ok(rows.Select(Dto));
    }

    /// <summary>Active terms for a contract form — any signed-in user (the forms are used outside Settings).</summary>
    [HttpGet("options")]
    [RequirePermission("dashboard.view")]
    public async Task<IActionResult> Options([FromQuery] string contractType = "supply")
    {
        var rows = await Db.ContractTerms.AsNoTracking().Where(x => x.ContractId == null).Where(t => t.IsActive && t.ContractType == contractType)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Id).ToListAsync();
        return Ok(rows.Select(t => new { t.Id, t.Category, text = (IsEnglish ? t.TextEn : null) ?? t.TextAr, t.TextAr, t.TextEn, t.IsDefault }));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TemplateInput input)
    {
        var t = new ContractTerm();
        Apply(t, input);
        if (input.SortOrder == null)
            t.SortOrder = (await Db.ContractTerms.Where(x => x.ContractId == null).Where(x => x.ContractType == t.ContractType).MaxAsync(x => (int?)x.SortOrder) ?? 0) + 10;
        Db.Add(t);
        await Db.SaveChangesAsync();
        Activity.Log("contract_term_template", t.Id, "create", $"Term added ({t.ContractType})");
        await Db.SaveChangesAsync();
        return Ok(Dto(t));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] TemplateInput input)
    {
        var t = await Db.ContractTerms.Where(x => x.ContractId == null).FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Term", id);
        Apply(t, input);
        await Db.SaveChangesAsync();
        return Ok(Dto(t));
    }

    /// <summary>Soft delete. Contracts keep their own copy of the text.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var t = await Db.ContractTerms.Where(x => x.ContractId == null).FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException("Term", id);
        Db.Remove(t);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Save a new order (ids in display order).</summary>
    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderInput input)
    {
        var rows = await Db.ContractTerms.Where(x => x.ContractId == null).Where(t => input.Ids.Contains(t.Id)).ToListAsync();
        for (var i = 0; i < input.Ids.Count; i++)
        {
            var t = rows.FirstOrDefault(x => x.Id == input.Ids[i]);
            if (t != null) t.SortOrder = (i + 1) * 10;
        }
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(ContractTerm t, TemplateInput i)
    {
        var type = (i.ContractType ?? t.ContractType ?? "supply").Trim().ToLowerInvariant();
        if (!ContractTypes.Contains(type)) throw new AppException($"Contract type must be one of: {string.Join(", ", ContractTypes)}.");
        var cat = (i.Category ?? t.Category ?? "general").Trim().ToLowerInvariant();
        if (!Categories.Contains(cat)) throw new AppException($"Category must be one of: {string.Join(", ", Categories)}.");
        if (string.IsNullOrWhiteSpace(i.TextAr)) throw new AppException("The term text is required.");
        t.ContractType = type; t.Category = cat; t.TextAr = i.TextAr.Trim(); t.TextEn = string.IsNullOrWhiteSpace(i.TextEn) ? null : i.TextEn.Trim();
        t.IsDefault = i.IsDefault;
        if (i.SortOrder != null) t.SortOrder = i.SortOrder.Value;
        if (i.IsActive != null) t.IsActive = i.IsActive.Value;
    }
}
