using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.Domain.DonorProfiles;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Identity.Infrastructure.SqlServer.Persistence;

public sealed class FcgIdentityDbContext : DbContext, IUnitOfWork
{
    public FcgIdentityDbContext(DbContextOptions<FcgIdentityDbContext> options) : base(options)
    {
    }

    public DbSet<DonorProfile> DonorProfiles => Set<DonorProfile>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FcgIdentityDbContext).Assembly);
    }
}
