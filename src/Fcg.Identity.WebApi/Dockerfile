FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Fcg.Identity.Application/Fcg.Identity.Application.csproj", "src/Fcg.Identity.Application/"]
COPY ["src/Fcg.Identity.Domain/Fcg.Identity.Domain.csproj", "src/Fcg.Identity.Domain/"]
COPY ["src/Fcg.Identity.Infrastructure.Kafka/Fcg.Identity.Infrastructure.Kafka.csproj", "src/Fcg.Identity.Infrastructure.Kafka/"]
COPY ["src/Fcg.Identity.Infrastructure.Keycloak/Fcg.Identity.Infrastructure.Keycloak.csproj", "src/Fcg.Identity.Infrastructure.Keycloak/"]
COPY ["src/Fcg.Identity.Infrastructure.SqlServer/Fcg.Identity.Infrastructure.SqlServer.csproj", "src/Fcg.Identity.Infrastructure.SqlServer/"]
COPY ["src/Fcg.Identity.Messages/Fcg.Identity.Messages.csproj", "src/Fcg.Identity.Messages/"]
COPY ["src/Fcg.Identity.WebApi/Fcg.Identity.WebApi.csproj", "src/Fcg.Identity.WebApi/"]

RUN dotnet restore "./src/Fcg.Identity.WebApi/Fcg.Identity.WebApi.csproj"

COPY . .
WORKDIR "/src/src/Fcg.Identity.WebApi"
RUN dotnet publish "./Fcg.Identity.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fcg.Identity.WebApi.dll"]
