using Fcg.Identity.Domain.Abstractions;
using Fcg.Identity.Domain.Shared.Results;
using Fcg.Identity.Domain.Shared.ValueObjects;

namespace Fcg.Identity.Domain.DonorProfiles;

public sealed class DonorProfile : BaseEntity
{
    private DonorProfile()
    {
    }

    private DonorProfile(
        Guid id,
        string keycloakUserId,
        string fullName,
        Email email,
        Cpf cpf)
        : base(id)
    {
        KeycloakUserId = keycloakUserId;
        FullName = fullName;
        Email = email;
        Cpf = cpf;
    }

    public string KeycloakUserId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public Email Email { get; private set; }
    public Cpf Cpf { get; private set; }

    public static Result<DonorProfile> Create(string keycloakUserId, string fullName, string email, string cpf)
    {
        var normalizedKeycloakUserId = keycloakUserId?.Trim() ?? string.Empty;
        var normalizedFullName = fullName?.Trim() ?? string.Empty;
        var normalizedEmail = email?.Trim() ?? string.Empty;
        var normalizedCpf = cpf?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedKeycloakUserId))
        {
            return Error.Validation("DonorProfile.KeycloakUserIdRequired", "Keycloak user id is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            return Error.Validation("DonorProfile.FullNameRequired", "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Error.Validation("DonorProfile.EmailRequired", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(normalizedCpf))
        {
            return Error.Validation("DonorProfile.CpfRequired", "CPF is required.");
        }

        var emailResult = Email.Create(normalizedEmail);
        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        var cpfResult = Cpf.Create(normalizedCpf);
        if (cpfResult.IsFailure)
        {
            return cpfResult.Error;
        }

        return new DonorProfile(
            Guid.NewGuid(),
            normalizedKeycloakUserId,
            normalizedFullName,
            emailResult.Value,
            cpfResult.Value);
    }

    public Result<DonorProfile> Update(string fullName, string email)
    {
        var normalizedFullName = fullName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            return Error.Validation("DonorProfile.FullNameRequired", "Full name is required.");
        }

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            return emailResult.Error;
        }

        FullName = normalizedFullName;
        Email = emailResult.Value;
        UpdatedAt = DateTime.UtcNow;

        return this;
    }
}
