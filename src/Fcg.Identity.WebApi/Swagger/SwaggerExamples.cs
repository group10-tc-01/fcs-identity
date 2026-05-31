using System.Diagnostics.CodeAnalysis;

namespace Fcg.Identity.WebApi.Swagger;

[ExcludeFromCodeCoverage]
public static class SwaggerExamples
{
    public static object RegisterDonorRequest => new
    {
        fullName = "Maria Silva",
        email = "maria@email.com",
        cpf = "12345678909",
        password = "StrongPassword123!"
    };

    public static object RegisterDonorSuccess => Success(new
    {
        id = "4a0c6628-15c2-4a5d-9d1c-5cfed8f2630b",
        fullName = "Maria Silva",
        email = "maria@email.com",
        cpf = "***.***.***-09"
    });

    public static object LoginRequest => new
    {
        email = "maria@email.com",
        password = "StrongPassword123!"
    };

    public static object TokenSuccess => Success(new
    {
        accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        refreshToken = "refresh-token",
        expiresIn = 300,
        tokenType = "Bearer"
    });

    public static object RefreshTokenRequest => new
    {
        refreshToken = "refresh-token"
    };

    public static object MeSuccess => Success(new
    {
        id = "4a0c6628-15c2-4a5d-9d1c-5cfed8f2630b",
        keycloakUserId = "7d75e1f5-3d52-45b7-9a0d-f130b3eb1f8d",
        fullName = "Maria Silva",
        email = "maria@email.com",
        role = "Doador"
    });

    public static object ValidationError => Failure("Invalid request data.");

    public static object UnauthorizedError => Failure("Invalid email or password.");

    public static object InvalidRefreshTokenError => Failure("Invalid refresh token.");

    public static object ProfileNotFoundError => Failure("Profile was not found.");

    public static object ConflictError => Failure("A user with this email already exists.");

    private static object Success(object data) => new
    {
        success = true,
        data,
        message = (string?)null
    };

    private static object Failure(string message) => new
    {
        success = false,
        data = (object?)null,
        message
    };
}
