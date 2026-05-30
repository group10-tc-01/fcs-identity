using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Identity.Infrastructure.SqlServer.Persistence.Repositories;

public sealed class DonorProfileRepository : IDonorProfileRepository
{
    private readonly FcgIdentityDbContext _dbContext;

    public DonorProfileRepository(FcgIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(DonorProfile donorProfile, CancellationToken cancellationToken = default)
    {
        return _dbContext.DonorProfiles.AddAsync(donorProfile, cancellationToken).AsTask();
    }

    public Task<DonorProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.DonorProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(donorProfile => donorProfile.Id == id, cancellationToken);
    }

    public Task<DonorProfile?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DonorProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(donorProfile => donorProfile.KeycloakUserId == keycloakUserId, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            return Task.FromResult(false);
        }

        return _dbContext.DonorProfiles.AnyAsync(donorProfile => donorProfile.Email == emailResult.Value, cancellationToken);
    }

    public Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var cpfResult = Cpf.Create(cpf);
        if (cpfResult.IsFailure)
        {
            return Task.FromResult(false);
        }

        return _dbContext.DonorProfiles.AnyAsync(donorProfile => donorProfile.Cpf == cpfResult.Value, cancellationToken);
    }
}
