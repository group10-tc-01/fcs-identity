FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Fcs.Identity.Application/Fcs.Identity.Application.csproj", "src/Fcs.Identity.Application/"]
COPY ["src/Fcs.Identity.Domain/Fcs.Identity.Domain.csproj", "src/Fcs.Identity.Domain/"]
COPY ["src/Fcs.Identity.Resources/Fcs.Identity.Resources.csproj", "src/Fcs.Identity.Resources/"]
COPY ["src/Fcs.Identity.Infrastructure.Kafka/Fcs.Identity.Infrastructure.Kafka.csproj", "src/Fcs.Identity.Infrastructure.Kafka/"]
COPY ["src/Fcs.Identity.Infrastructure.Keycloak/Fcs.Identity.Infrastructure.Keycloak.csproj", "src/Fcs.Identity.Infrastructure.Keycloak/"]
COPY ["src/Fcs.Identity.Infrastructure.SqlServer/Fcs.Identity.Infrastructure.SqlServer.csproj", "src/Fcs.Identity.Infrastructure.SqlServer/"]
COPY ["src/Fcs.Identity.WebApi/Fcs.Identity.WebApi.csproj", "src/Fcs.Identity.WebApi/"]

RUN dotnet restore "./src/Fcs.Identity.WebApi/Fcs.Identity.WebApi.csproj"

COPY . .
WORKDIR "/src/src/Fcs.Identity.WebApi"
RUN dotnet publish "./Fcs.Identity.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fcs.Identity.WebApi.dll"]
