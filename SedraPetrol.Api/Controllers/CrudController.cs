using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;
using SedraPetrol.Api.Services;

namespace SedraPetrol.Api.Controllers;

/// <summary>
/// Generic CRUD for one table:
///   GET    /           paged list  (?page=&amp;pageSize=&amp;search=&amp;sort=&amp;desc= plus any column as an equality filter, e.g. ?buildingId=5)
///   GET    /{id}       one row
///   POST   /           create
///   PUT    /{id}       full update
///   DELETE /{id}       soft delete (is_deleted = 1)
/// Override the virtual hooks to add validation or side effects.
/// </summary>
public abstract class CrudController<T> : ApiControllerBase where T : AuditableEntity, new()
{
    private static readonly PropertyInfo[] Scalars = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanWrite && IsScalar(p.PropertyType)).ToArray();

    private static readonly PropertyInfo? BranchProp = typeof(T).GetProperty("BranchId");

    private static bool IsScalar(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(DateOnly)
               || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeOnly) || t == typeof(Guid);
    }

    /// <summary>Base query for lists and single reads. Applies branch security for non-admin users.</summary>
    protected virtual IQueryable<T> Query()
    {
        IQueryable<T> q = Db.Set<T>();
        return ApplyBranchScope(q);
    }

    protected IQueryable<T> ApplyBranchScope(IQueryable<T> q)
    {
        if (BranchProp == null || Me.IsAdmin || Me.BranchIds.Count == 0) return q;
        var ids = Me.BranchIds.ToList();
        var p = Expression.Parameter(typeof(T), "e");
        Expression member = Expression.Property(p, BranchProp);
        if (member.Type == typeof(long?))
        {
            var nullableIds = ids.Select(i => (long?)i).ToList();
            var contains = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { typeof(long?) }, Expression.Constant(nullableIds), member);
            return q.Where(Expression.Lambda<Func<T, bool>>(Expression.OrElse(Expression.Equal(member, Expression.Constant(null, typeof(long?))), contains), p));
        }
        var c = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { typeof(long) }, Expression.Constant(ids), member);
        return q.Where(Expression.Lambda<Func<T, bool>>(c, p));
    }

    /// <summary>e.Prop, or e.Party.Name for a role property stored on the party ([PartyField]).</summary>
    protected static Expression MemberOf(ParameterExpression p, PropertyInfo prop)
    {
        var pf = prop.GetCustomAttribute<PartyFieldAttribute>();
        return pf == null ? Expression.Property(p, prop) : Expression.Property(Expression.Property(p, nameof(PartyRole.Party)), pf.Name);
    }

    /// <summary>Default search: any short text column contains the term.</summary>
    protected virtual Expression<Func<T, bool>>? SearchPredicate(string term)
    {
        var p = Expression.Parameter(typeof(T), "e");
        Expression? body = null;
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        foreach (var prop in Scalars.Where(x => x.PropertyType == typeof(string)))
        {
            var max = prop.GetCustomAttribute<MaxLengthAttribute>()?.Length;
            if (max is null or > 300) continue;
            var member = MemberOf(p, prop);
            var test = Expression.AndAlso(Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
                                          Expression.Call(member, containsMethod, Expression.Constant(term)));
            body = body == null ? test : Expression.OrElse(body, test);
        }
        return body == null ? null : Expression.Lambda<Func<T, bool>>(body, p);
    }

    protected virtual Task OnCreatingAsync(T entity) => Task.CompletedTask;
    protected virtual Task OnUpdatingAsync(T existing, T incoming) => Task.CompletedTask;
    protected virtual Task OnDeletingAsync(T entity) => Task.CompletedTask;
    protected virtual Task OnSavedAsync(T entity, string action) => Task.CompletedTask;

    /// <summary>Paged list. Every column can be used as an exact-match filter, e.g. ?statusId=3&amp;buildingId=1.</summary>
    [HttpGet]
    public virtual async Task<ActionResult<PagedResult<T>>> List([FromQuery] PageQuery q, CancellationToken ct)
    {
        var query = ApplyFilters(Query().AsNoTracking(), q);
        return Ok(await query.ToPagedAsync(q, ct));
    }

    [HttpGet("{id:long}")]
    public virtual async Task<ActionResult<T>> Get(long id, CancellationToken ct)
    {
        var e = await Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return e == null ? throw new NotFoundException(typeof(T).Name, id) : Ok(e);
    }

    [HttpPost]
    public virtual async Task<ActionResult<T>> Create([FromBody] T entity)
    {
        entity.Id = 0;
        ResetAudit(entity);
        if (BranchProp != null) EnsureBranchAccess(BranchProp.GetValue(entity) as long?);
        await OnCreatingAsync(entity);
        var parties = entity is PartyRole ? HttpContext.RequestServices.GetRequiredService<PartyService>() : null;
        if (entity is PartyRole role) await parties!.PrepareRoleAsync(role, BranchProp?.GetValue(entity) as long?);
        Db.Set<T>().Add(entity);
        await Db.SaveChangesAsync();
        if (parties != null) await parties.EnsureAccountAsync(entity.Id);
        await OnSavedAsync(entity, "create");
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:long}")]
    public virtual async Task<ActionResult<T>> Update(long id, [FromBody] T incoming)
    {
        var existing = await Query().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException(typeof(T).Name, id);
        await OnUpdatingAsync(existing, incoming);
        foreach (var p in Scalars)
        {
            if (p.SetMethod is not { IsPublic: true }) continue;   // set by the system (contract_type, asset_type …)
            if (p.Name is nameof(AuditableEntity.Id) or nameof(AuditBase.CreatedAt) or nameof(AuditBase.CreatedBy)
                or nameof(AuditBase.UpdatedAt) or nameof(AuditBase.UpdatedBy) or nameof(AuditBase.IsDeleted)) continue;
            p.SetValue(existing, p.GetValue(incoming));
        }
        if (BranchProp != null) EnsureBranchAccess(BranchProp.GetValue(existing) as long?);
        await Db.SaveChangesAsync();
        if (existing is PartyRole) await HttpContext.RequestServices.GetRequiredService<PartyService>().EnsureAccountAsync(existing.Id);   // keeps the account name in step
        await OnSavedAsync(existing, "update");
        return Ok(existing);
    }

    [HttpDelete("{id:long}")]
    public virtual async Task<IActionResult> Delete(long id)
    {
        var existing = await Query().FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException(typeof(T).Name, id);
        await OnDeletingAsync(existing);
        Db.Set<T>().Remove(existing); // converted to a soft delete by AppDbContext
        await Db.SaveChangesAsync();
        await OnSavedAsync(existing, "delete");
        return NoContent();
    }

    private static void ResetAudit(T e)
    {
        e.CreatedAt = default; e.CreatedBy = null; e.UpdatedAt = null; e.UpdatedBy = null; e.IsDeleted = false;
    }

    protected IQueryable<T> ApplyFilters(IQueryable<T> query, PageQuery q)
    {
        foreach (var (key, values) in Request.Query)
        {
            if (PageQuery.Reserved.Contains(key)) continue;
            var prop = Scalars.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
            if (prop == null) continue;
            var raw = values.ToString();
            var p = Expression.Parameter(typeof(T), "e");
            var member = MemberOf(p, prop);
            Expression body;
            if (raw.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                body = Expression.Equal(member, Expression.Constant(null, prop.PropertyType));
            }
            else if (raw.Contains(','))
            {
                // ?statusId=1,2,3 -> IN (...)
                var listType = typeof(List<>).MakeGenericType(prop.PropertyType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries)) list.Add(Convert(part, prop.PropertyType));
                body = Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), new[] { prop.PropertyType }, Expression.Constant(list), member);
            }
            else
            {
                body = Expression.Equal(member, Expression.Constant(Convert(raw, prop.PropertyType), prop.PropertyType));
            }
            query = query.Where(Expression.Lambda<Func<T, bool>>(body, p));
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pred = SearchPredicate(q.Search.Trim());
            if (pred != null) query = query.Where(pred);
        }

        var sortProp = Scalars.FirstOrDefault(p => string.Equals(p.Name, q.Sort, StringComparison.OrdinalIgnoreCase));
        if (sortProp?.GetCustomAttribute<PartyFieldAttribute>() is { } spf)
        {
            var sp = Expression.Parameter(typeof(T), "e");
            var key = Expression.Lambda<Func<T, object?>>(Expression.Convert(MemberOf(sp, sortProp), typeof(object)), sp);
            return q.Desc ? query.OrderByDescending(key) : query.OrderBy(key);
        }
        var sortName = sortProp?.Name ?? nameof(AuditableEntity.Id);
        return q.Desc ? query.OrderByDescending(e => EF.Property<object>(e, sortName))
                      : query.OrderBy(e => EF.Property<object>(e, sortName));
    }

    private static object? Convert(string raw, Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        try
        {
            if (t == typeof(string)) return raw;
            if (t == typeof(DateOnly)) return DateOnly.Parse(raw, CultureInfo.InvariantCulture);
            if (t == typeof(DateTimeOffset)) return DateTimeOffset.Parse(raw, CultureInfo.InvariantCulture);
            if (t == typeof(bool)) return raw is "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase);
            return System.Convert.ChangeType(raw, t, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            throw new AppException($"Invalid filter value '{raw}' for {type.Name}.");
        }
    }
}
