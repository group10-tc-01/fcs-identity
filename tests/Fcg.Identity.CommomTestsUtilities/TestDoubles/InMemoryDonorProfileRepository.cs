using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.Shared.ValueObjects;

namespace Fcg.Identity.CommomTestsUtilities.TestDoubles;

public sealed class InMemoryDonorProfileRepository : IDonorProfileRepository
{
    private readonly List<DonorProfile> _donorProfiles = [];

    public IReadOnlyCollection<DonorProfile> DonorProfiles => _donorProfiles;

    public void Reset()
    {
        _donorProfiles.Clear();
    }

    public Task AddAsync(DonorProfile donorProfile, CancellationToken cancellationToken = default)
    {
        _donorProfiles.Add(donorProfile);
        return Task.CompletedTask;
    }

    public Task<DonorProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_donorProfiles.FirstOrDefault(donorProfile => donorProfile.Id == id));
    }

    public Task<DonorProfile?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_donorProfiles.FirstOrDefault(donorProfile => donorProfile.KeycloakUserId == keycloakUserId));
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_donorProfiles.Any(donorProfile => donorProfile.Email == emailResult.Value));
    }

    public Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var cpfResult = Cpf.Create(cpf);
        if (cpfResult.IsFailure)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_donorProfiles.Any(donorProfile => donorProfile.Cpf == cpfResult.Value));
    }
}
