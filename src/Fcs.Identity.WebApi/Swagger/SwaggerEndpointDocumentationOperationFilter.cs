using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fcs.Identity.WebApi.Swagger;

[ExcludeFromCodeCoverage]
public sealed class SwaggerEndpointDocumentationOperationFilter : IOperationFilter
{
    private static readonly IReadOnlyDictionary<string, EndpointDocumentation> EndpointDocumentationByOperationId =
        new Dictionary<string, EndpointDocumentation>
        {
            [nameof(Controllers.v1.AuthController.RegisterDonor)] = new(
                "Registrar doador",
                "Cadastra um usuario com perfil Doador. A API cria o usuario no Keycloak, atribui a role Doador e persiste o perfil local no IdentityDb.",
                SwaggerExamples.RegisterDonorRequest,
                new Dictionary<string, ResponseDocumentation>
                {
                    ["201"] = new("Doador registrado com sucesso.", SwaggerExamples.RegisterDonorSuccess),
                    ["400"] = new("Payload invalido ou campos obrigatorios ausentes.", SwaggerExamples.ValidationError),
                    ["409"] = new("E-mail, CPF ou usuario ja existe.", SwaggerExamples.ConflictError)
                }),
            [nameof(Controllers.v1.AuthController.Login)] = new(
                "Autenticar usuario",
                "Autentica e-mail e senha pela fachada da API. A validacao de credenciais e a emissao dos tokens sao delegadas ao Keycloak.",
                SwaggerExamples.LoginRequest,
                new Dictionary<string, ResponseDocumentation>
                {
                    ["200"] = new("Login realizado com sucesso.", SwaggerExamples.TokenSuccess),
                    ["400"] = new("Payload invalido ou campos obrigatorios ausentes.", SwaggerExamples.ValidationError),
                    ["401"] = new("Credenciais invalidas.", SwaggerExamples.UnauthorizedError)
                }),
            [nameof(Controllers.v1.AuthController.Refresh)] = new(
                "Renovar token",
                "Renova o access token usando um refresh token emitido pelo Keycloak.",
                SwaggerExamples.RefreshTokenRequest,
                new Dictionary<string, ResponseDocumentation>
                {
                    ["200"] = new("Token renovado com sucesso.", SwaggerExamples.TokenSuccess),
                    ["400"] = new("Payload invalido ou refresh token ausente.", SwaggerExamples.ValidationError),
                    ["401"] = new("Refresh token invalido ou expirado.", SwaggerExamples.InvalidRefreshTokenError)
                }),
            [nameof(Controllers.v1.AuthController.Logout)] = new(
                "Encerrar sessao",
                "Remove os cookies de autenticacao emitidos pela API para encerrar a sessao do usuario.",
                null,
                new Dictionary<string, ResponseDocumentation>
                {
                    ["204"] = new("Sessao encerrada com sucesso.")
                }),
            [nameof(Controllers.v1.MeController.Get)] = new(
                "Consultar meu perfil",
                "Retorna o perfil local do usuario autenticado. Requer Bearer token valido com role Doador ou GestorONG.",
                null,
                new Dictionary<string, ResponseDocumentation>
                {
                    ["200"] = new("Perfil autenticado encontrado.", SwaggerExamples.MeSuccess),
                    ["401"] = new("Bearer token ausente, invalido ou expirado."),
                    ["404"] = new("Perfil local nao encontrado para o usuario autenticado.", SwaggerExamples.ProfileNotFoundError)
                })
        };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!EndpointDocumentationByOperationId.TryGetValue(context.MethodInfo.Name, out var documentation))
        {
            return;
        }

        operation.Summary = documentation.Summary;
        operation.Description = documentation.Description;
        operation.OperationId = context.MethodInfo.Name;

        ApplyRequestExample(operation, documentation.RequestExample);
        ApplyResponseDocumentation(operation, documentation.Responses);
    }

    private static void ApplyRequestExample(OpenApiOperation operation, object? example)
    {
        if (example is null || operation.RequestBody?.Content is null)
        {
            return;
        }

        if (!operation.RequestBody.Content.TryGetValue(SwaggerConstants.JsonContentType, out var mediaType))
        {
            return;
        }

        mediaType.Example = OpenApiExampleFactory.Create(example);
    }

    private static void ApplyResponseDocumentation(
        OpenApiOperation operation,
        IReadOnlyDictionary<string, ResponseDocumentation> responses)
    {
        if (operation.Responses is null)
        {
            return;
        }

        foreach (var (statusCode, documentation) in responses)
        {
            if (!operation.Responses.TryGetValue(statusCode, out var response))
            {
                continue;
            }

            response.Description = documentation.Description;

            if (documentation.Example is not null &&
                response.Content is not null &&
                response.Content.TryGetValue(SwaggerConstants.JsonContentType, out var mediaType))
            {
                mediaType.Example = OpenApiExampleFactory.Create(documentation.Example);
            }
        }
    }
}
