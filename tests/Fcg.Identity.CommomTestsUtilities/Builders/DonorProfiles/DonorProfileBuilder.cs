using Bogus;
using Fcg.Identity.CommomTestsUtilities.Fakers.Shared;
using Fcg.Identity.Domain.DonorProfiles;
using Fcg.Identity.Domain.Shared.Results;

namespace Fcg.Identity.CommomTestsUtilities.Builders.DonorProfiles;

public sealed class DonorProfileBuilder
{
    private readonly Faker _faker = new("pt_BR");
    private string _keycloakUserId = Guid.NewGuid().ToString();
    private string _fullName;
    private string _email = EmailFaker.Generate();
    private string _cpf = CpfFaker.Generate();

    public DonorProfileBuilder()
    {
        _fullName = _faker.Person.FullName;
    }

    public DonorProfileBuilder WithKeycloakUserId(string keycloakUserId)
    {
        _keycloakUserId = keycloakUserId;
        return this;
    }

    public DonorProfileBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public DonorProfileBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public DonorProfileBuilder WithCpf(string cpf)
    {
        _cpf = cpf;
        return this;
    }

    public DonorProfile Build()
    {
        return BuildResult().Value;
    }

    public Result<DonorProfile> BuildResult()
    {
        return DonorProfile.Create(_keycloakUserId, _fullName, _email, _cpf);
    }
}
