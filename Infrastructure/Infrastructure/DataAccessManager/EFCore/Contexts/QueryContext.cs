using Application.Common.CQS.Queries;
using Application.Common.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Contexts;

public class QueryContext : DataContext, IQueryContext
{
    public QueryContext(DbContextOptions<DataContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public new IQueryable<T> Set<T>() where T : class
    {
        return base.Set<T>();
    }
}
