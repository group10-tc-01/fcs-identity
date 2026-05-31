using Fcg.Identity.Application.Abstractions.Identity;
using Fcg.Identity.Domain.Shared.Results;

namespace Fcg.Identity.CommomTestsUtilities.TestDoubles;

public sealed class FakeIdentityProvider : IIdentityProvider
{
    private Result<CreateDonorIdentityUserResponse> _createDonorResult =
        new CreateDonorIdentityUserResponse(Guid.NewGuid().ToString());

    private Result<LoginIdentityUserResponse> _loginResult =
        new LoginIdentityUserResponse("access-token", "refresh-token", 300, "Bearer");

    public int CreateDonorCalls { get; private set; }
    public CreateDonorIdentityUserRequest? LastCreateDonorRequest { get; private set; }

    public int LoginCalls { get; private set; }
    public LoginIdentityUserRequest? LastLoginRequest { get; private set; }

    public void Reset()
    {
        _createDonorResult = new CreateDonorIdentityUserResponse(Guid.NewGuid().ToString());
        _loginResult = new LoginIdentityUserResponse("access-token", "refresh-token", 300, "Bearer");
        CreateDonorCalls = 0;
        LastCreateDonorRequest = null;
        LoginCalls = 0;
        LastLoginRequest = null;
    }

    public void ConfigureCreateDonorResult(Result<CreateDonorIdentityUserResponse> result)
    {
        _createDonorResult = result;
    }

    public void ConfigureLoginResult(Result<LoginIdentityUserResponse> result)
    {
        _loginResult = result;
    }

    public Task<Result<CreateDonorIdentityUserResponse>> CreateDonorAsync(
        CreateDonorIdentityUserRequest request,
        CancellationToken cancellationToken = default)
    {
        CreateDonorCalls++;
        LastCreateDonorRequest = request;

        return Task.FromResult(_createDonorResult);
    }

    public Task<Result<LoginIdentityUserResponse>> LoginAsync(
        LoginIdentityUserRequest request,
        CancellationToken cancellationToken = default)
    {
        LoginCalls++;
        LastLoginRequest = request;

        return Task.FromResult(_loginResult);
    }
}
