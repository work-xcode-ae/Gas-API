using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Data;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Security;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Full history of one record: /api/history/Meter/5
///   • every insert / update / delete of the record itself (field by field, old → new),
///   • with related=true (default) every change of records linked to it (readings of a meter, units / contracts / invoices of a building …),
///   • business events from the activity log (status changes with reasons, approvals …),
///   • usage: who opened / printed / exported it.
/// Foreign-key values are shown with their names (lookups, buildings, units, tenants, vendors, accounts …).
/// </summary>
[Route("api/history")]
[PermissionModule("audit")]
public class HistoryController : ApiControllerBase
{
    [HttpGet("{entityType}/{id}")]
    public async Task<IActionResult> Get(string entityType, string id, [FromQuery] bool related = true, [FromQuery] bool views = true,
        [FromQuery] string? kind = null, [FromQuery] long? userId = null, [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        var et = Db.Model.GetEntityTypes().FirstOrDefault(t => string.Equals(t.ClrType.Name, entityType, StringComparison.OrdinalIgnoreCase));
        var type = et?.ClrType.Name ?? entityType;
        // rows that point to the base type (e.g. invoice.contract_id → Contract) are linked under the base type's name; ids are shared
        var baseType = et?.BaseType?.ClrType.Name ?? type;
        var q = Db.AuditTrails.AsNoTracking().Where(a =>
            (a.EntityType == type && a.EntityId == id)
            || a.Links.Any(l => (l.EntityType == type || l.EntityType == baseType) && l.EntityId == id && (related || l.IsSelf)));
        if (!views) q = q.Where(a => a.Action != "view");
        if (kind == "changes") q = q.Where(a => a.Action == "insert" || a.Action == "update" || a.Action == "delete" || a.Action == "hard_delete");
        else if (kind == "usage") q = q.Where(a => a.Action == "view" || a.Action == "print" || a.Action == "export" || a.Action == "download");
        if (userId != null) q = q.Where(a => a.UserId == userId);
        if (from != null) { var f = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(4)); q = q.Where(a => a.OccurredAt >= f); }
        if (to != null) { var t = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(4)); q = q.Where(a => a.OccurredAt < t); }

        var total = await q.CountAsync(ct);
        pageSize = Math.Clamp(pageSize, 10, 500);
        page = Math.Max(1, page);
        var rows = await q.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        // business events (activity log uses snake_case entity names: meter, journal_entry …) — first page only
        var events = new List<object>();
        if (page == 1 && kind != "usage" && long.TryParse(id, out var lid))
        {
            var snake = Snake(type);
            var ev = await Db.ActivityLogs.AsNoTracking().Where(a => (a.EntityType == snake || a.EntityType == type) && a.EntityId == lid)
                .OrderByDescending(a => a.OccurredAt).Take(300)
                .Select(a => new { a.Id, a.OccurredAt, a.Action, a.Description, a.UserId, user = a.User!.NameAr ?? a.User.NameEn }).ToListAsync(ct);
            events.AddRange(ev.Select(e => (object)new { kind = "event", e.Id, e.OccurredAt, action = e.Action, e.Description, e.UserId, userName = e.user }));
        }

        // ---- resolve foreign keys to names ----
        var fkMap = new Dictionary<string, Dictionary<string, Type>>();   // entity type → property → principal CLR type
        Dictionary<string, Type> FksOf(string? t)
        {
            if (t == null) return new();
            if (fkMap.TryGetValue(t, out var m)) return m;
            var e = Db.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType.Name == t);
            m = e == null ? new() : e.GetForeignKeys().Where(f => f.Properties.Count == 1)
                .GroupBy(f => f.Properties[0].Name).ToDictionary(g => g.Key, g => g.First().PrincipalEntityType.ClrType);
            fkMap[t] = m;
            return m;
        }
        var parsed = rows.Select(r => (Row: r, Changes: Parse(r.Changes))).ToList();
        var want = new Dictionary<Type, HashSet<long>>();
        foreach (var (r, ch) in parsed)
        {
            var fks = FksOf(r.EntityType);
            foreach (var c in ch)
                if (fks.TryGetValue(c.Field, out var pt))
                    foreach (var v in new[] { c.Old, c.New })
                        if (v is JsonElement { ValueKind: JsonValueKind.Number } n && n.TryGetInt64(out var idv))
                            (want.TryGetValue(pt, out var set) ? set : want[pt] = new()).Add(idv);
        }
        // the related records themselves (to label "reading #12 of meter M-001")
        foreach (var r in rows.Where(r => r.EntityType != null && r.EntityId != null && !(r.EntityType == type && r.EntityId == id)))
        {
            var e = Db.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType.Name == r.EntityType);
            if (e != null && long.TryParse(r.EntityId, out var rid)) (want.TryGetValue(e.ClrType, out var set) ? set : want[e.ClrType] = new()).Add(rid);
        }
        await LoadLookups();
        var names = new Dictionary<Type, Dictionary<long, string>>();
        foreach (var (t, ids) in want)
        {
            if (t == typeof(LookupValue)) { names[t] = ids.ToDictionary(x => x, x => Label(x) ?? $"#{x}"); continue; }
            if (!typeof(AuditableEntity).IsAssignableFrom(t)) continue;
            try { names[t] = await NamesAsync(t, ids.ToList()); } catch { /* table missing / not mapped: show ids */ }
        }
        string? NameOf(Type t, JsonElement? v) =>
            v is JsonElement { ValueKind: JsonValueKind.Number } n && n.TryGetInt64(out var x) && names.TryGetValue(t, out var d) && d.TryGetValue(x, out var s) ? s : null;

        var items = parsed.Select(p =>
        {
            var r = p.Row;
            var fks = FksOf(r.EntityType);
            var self = r.EntityType == type && r.EntityId == id;
            string? recordName = null;
            if (!self && r.EntityType != null && long.TryParse(r.EntityId, out var rid))
            {
                var e = Db.Model.GetEntityTypes().FirstOrDefault(x => x.ClrType.Name == r.EntityType);
                if (e != null && names.TryGetValue(e.ClrType, out var d) && d.TryGetValue(rid, out var nm)) recordName = nm;
            }
            return (object)new
            {
                kind = r.Action is "view" or "print" or "export" or "download" or "logout" ? "usage" : r.Action is "login" or "login_failed" ? "auth" : "change",
                r.Id, r.OccurredAt, r.TransactionId, r.UserId, r.UserName, r.Action, table = r.TableName, r.EntityType, r.EntityId, self, recordName,
                r.RequestPath, r.IpAddress, r.HttpMethod,
                changes = p.Changes.Select(c =>
                {
                    var pt = fks.GetValueOrDefault(c.Field);
                    return new
                    {
                        field = c.Field, old = c.Old, @new = c.New,
                        oldText = pt == null ? null : NameOf(pt, c.Old), newText = pt == null ? null : NameOf(pt, c.New),
                        refType = pt?.Name,
                    };
                }),
            };
        }).ToList();

        // the record's own display name
        string? title = null;
        if (et != null && long.TryParse(id, out var selfId) && typeof(AuditableEntity).IsAssignableFrom(et.ClrType))
            try { title = (await NamesAsync(et.ClrType, new List<long> { selfId })).GetValueOrDefault(selfId); } catch { }

        return Ok(new { entityType = type, id, title, total, page, pageSize, items, events });
    }

    /// <summary>Summary for a badge: number of changes, last change and who did it.</summary>
    [HttpGet("{entityType}/{id}/summary")]
    public async Task<IActionResult> Summary(string entityType, string id, CancellationToken ct)
    {
        var baseType = Db.Model.GetEntityTypes().FirstOrDefault(t => t.ClrType.Name == entityType)?.BaseType?.ClrType.Name ?? entityType;
        var q = Db.AuditTrails.AsNoTracking().Where(a => (a.EntityType == entityType && a.EntityId == id) || a.Links.Any(l => (l.EntityType == entityType || l.EntityType == baseType) && l.EntityId == id));
        var changes = await q.CountAsync(a => a.Action != "view" && a.Action != "print" && a.Action != "export" && a.Action != "download", ct);
        var last = await q.Where(a => a.Action != "view").OrderByDescending(a => a.OccurredAt).Select(a => new { a.OccurredAt, a.UserName, a.Action }).FirstOrDefaultAsync(ct);
        var created = await q.Where(a => a.Action == "insert" && a.EntityType == entityType && a.EntityId == id).Select(a => new { a.OccurredAt, a.UserName }).FirstOrDefaultAsync(ct);
        var viewers = await q.Where(a => a.Action == "view").Select(a => a.UserId).Distinct().CountAsync(ct);
        return Ok(new { changes, last, created, viewers });
    }

    // ------------------------------------------------------------------ helpers

    private sealed record Change(string Field, JsonElement? Old, JsonElement? New);

    private static List<Change> Parse(string? json)
    {
        var list = new List<Change>();
        if (string.IsNullOrWhiteSpace(json)) return list;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return list;
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (p.Value.ValueKind == JsonValueKind.Object && (p.Value.TryGetProperty("old", out _) || p.Value.TryGetProperty("new", out _)))
                {
                    JsonElement? o = p.Value.TryGetProperty("old", out var ov) && ov.ValueKind != JsonValueKind.Null ? ov.Clone() : null;
                    JsonElement? n = p.Value.TryGetProperty("new", out var nv) && nv.ValueKind != JsonValueKind.Null ? nv.Clone() : null;
                    list.Add(new Change(p.Name, o, n));
                }
                else if (p.Value.ValueKind != JsonValueKind.Null) list.Add(new Change(p.Name, null, p.Value.Clone()));   // usage rows: {title, path}
            }
        }
        catch (JsonException) { }
        return list;
    }

    private static string Snake(string s) => string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    private Task<Dictionary<long, string>> NamesAsync(Type t, List<long> ids) =>
        (Task<Dictionary<long, string>>)typeof(HistoryController).GetMethod(nameof(LoadNamesAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(t).Invoke(null, new object[] { Db, ids, IsEnglish })!;

    private static async Task<Dictionary<long, string>> LoadNamesAsync<T>(AppDbContext db, List<long> ids, bool en) where T : AuditableEntity
    {
        var rows = await db.Set<T>().IgnoreQueryFilters().AsNoTracking().Where(e => ids.Contains(e.Id)).ToListAsync();
        return rows.ToDictionary(r => r.Id, r => Display(r, en));
    }

    private static readonly string[] NumberProps = { "InvoiceNo", "BillNo", "ReceiptNo", "VoucherNo", "CreditNoteNo", "RefundNo", "JournalNo", "ContractNo", "MeterNo",
        "UnitNo", "TankNo", "RequestNo", "ServiceNo", "VisitNo", "PoNumber", "CertificateNo", "TransferNo", "PaymentNo", "AccountCode", "Code", "VehicleNo", "PlateNo", "EmployeeNo", "AssetNo" };

    /// <summary>Human name of any record: its number/code and Arabic (or English) name when it has one.</summary>
    private static string Display(object o, bool en)
    {
        var t = o.GetType();
        string? Get(string p) => t.GetProperty(p)?.GetValue(o)?.ToString();
        var no = NumberProps.Select(Get).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        var name = en ? Get("NameEn") ?? Get("NameAr") : Get("NameAr") ?? Get("NameEn");
        name ??= Get("Description") is { Length: > 0 } d ? (d.Length > 60 ? d[..60] + "…" : d) : null;
        if (no != null && name != null && no != name) return $"{no} — {name}";
        return no ?? name ?? Get("Email") ?? $"#{Get("Id")}";
    }
}
