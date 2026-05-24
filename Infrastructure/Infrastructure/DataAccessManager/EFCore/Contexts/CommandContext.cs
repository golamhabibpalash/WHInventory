using Application.Common.CQS.Commands;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;

public class CommandContext : DataContext, ICommandContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CommandContext(DbContextOptions<DataContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = BuildAuditEntries();
        var result = await base.SaveChangesAsync(cancellationToken);
        if (auditEntries.Count > 0)
        {
            AuditLog.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }
        return result;
    }

    private List<AuditLog> BuildAuditEntries()
    {
        var entries = new List<AuditLog>();
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var ip = httpContext?.Connection?.RemoteIpAddress?.ToString();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.Entity is UserActivityLog)
                continue;

            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var operationType = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Modified => "Update",
                EntityState.Deleted => "Delete",
                _ => null
            };

            if (operationType == null) continue;

            var entityId = GetEntityId(entry);
            var entityType = entry.Entity.GetType().Name;

            string? oldValues = null;
            string? newValues = null;

            if (entry.State == EntityState.Modified)
            {
                var old = new Dictionary<string, object?>();
                var @new = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified)
                    {
                        old[prop.Metadata.Name] = prop.OriginalValue;
                        @new[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }
                oldValues = JsonSerializer.Serialize(old);
                newValues = JsonSerializer.Serialize(@new);
            }
            else if (entry.State == EntityState.Added)
            {
                var @new = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                    @new[prop.Metadata.Name] = prop.CurrentValue;
                newValues = JsonSerializer.Serialize(@new);
            }
            else if (entry.State == EntityState.Deleted)
            {
                var old = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties)
                    old[prop.Metadata.Name] = prop.OriginalValue;
                oldValues = JsonSerializer.Serialize(old);
            }

            entries.Add(new AuditLog
            {
                Id = Guid.NewGuid().ToString(),
                EntityType = entityType,
                EntityId = entityId,
                OperationType = operationType,
                OldValues = oldValues,
                NewValues = newValues,
                UserId = userId,
                IpAddress = ip,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        return entries;
    }

    private static string? GetEntityId(EntityEntry entry)
    {
        try
        {
            var keyProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            return keyProp?.CurrentValue?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
