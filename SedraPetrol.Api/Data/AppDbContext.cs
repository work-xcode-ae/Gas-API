using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SedraPetrol.Api.Common;
using SedraPetrol.Api.Domain.Entities;

namespace SedraPetrol.Api.Data;

/// <summary>
/// Database-first context mapped onto the existing SedraPetrol SQL Server database
/// (created by sedra_schema_v2.2_sqlserver.sql). No EF migrations are used.
/// </summary>
public partial class AppDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null) : base(options)
    {
        _currentUser = currentUser;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            if (!typeof(AuditBase).IsAssignableFrom(clr)) continue;

            // Soft delete: hide rows with is_deleted = 1 everywhere (use IgnoreQueryFilters() to see them).
            var p = Expression.Parameter(clr, "e");
            var body = Expression.Equal(Expression.Property(p, nameof(AuditBase.IsDeleted)), Expression.Constant(false));
            modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(body, p));

            modelBuilder.Entity(clr).Property(nameof(AuditBase.CreatedAt)).HasDefaultValueSql("SYSDATETIMEOFFSET()");

            // No cascading deletes: financial rows must never be physically deleted.
            foreach (var fk in entityType.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;
        }

        ConfigureDefaults(modelBuilder);
    }

    /// <summary>Columns that have a SQL DEFAULT and are declared non-nullable in C#.</summary>
    private static void ConfigureDefaults(ModelBuilder mb)
    {
        foreach (var et in mb.Model.GetEntityTypes())
        {
            var isActive = et.FindProperty("IsActive");
            if (isActive != null && isActive.ClrType == typeof(bool))
                isActive.SetDefaultValue(true);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAudit()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser?.UserId;
        foreach (var entry in ChangeTracker.Entries<AuditBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy ??= userId;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Property(x => x.CreatedAt).IsModified = false;
                    entry.Property(x => x.CreatedBy).IsModified = false;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Deleted:
                    // Convert physical deletes into soft deletes: only is_deleted / updated_* are written.
                    entry.State = EntityState.Unchanged;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    entry.Property(x => x.IsDeleted).IsModified = true;
                    entry.Property(x => x.UpdatedAt).IsModified = true;
                    entry.Property(x => x.UpdatedBy).IsModified = true;
                    break;
            }
        }
    }
}
