namespace Fcg.Identity.Domain.DonorProfiles;

public interface IDonorProfileRepository
{
    Task AddAsync(DonorProfile donorProfile, CancellationToken cancellationToken = default);
    Task<DonorProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DonorProfile?> GetByKeycloakUserIdAsync(string keycloakUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCpfAsync(string cpf, CancellationToken cancellationToken = default);
}
