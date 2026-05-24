using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.CQS.Commands;

public interface ICommandContext
{
    DbSet<UserActivityLog> UserActivityLog { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
