using System.Diagnostics.CodeAnalysis;
using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.ManagerProfiles;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Identity.Infrastructure.SqlServer.Persistence;

[ExcludeFromCodeCoverage]
public sealed class FcgIdentityDbContext : DbContext, IUnitOfWork
{
    public FcgIdentityDbContext(DbContextOptions<FcgIdentityDbContext> options) : base(options)
    {
    }

    public DbSet<DonorProfile> DonorProfiles => Set<DonorProfile>();
    public DbSet<ManagerProfile> ManagerProfiles => Set<ManagerProfile>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FcgIdentityDbContext).Assembly);
    }
}
