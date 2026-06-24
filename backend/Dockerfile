FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 7218


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/AuthService.Web/AuthService.Web.csproj", "src/AuthService.Web/"]
COPY ["src/AuthService.Core/AuthService.Core.csproj", "src/AuthService.Core/"]
COPY ["src/AuthService.Contracts/AuthService.Contracts.csproj", "src/AuthService.Contracts/"]
COPY ["src/AuthService.Domain/AuthService.Domain.csproj", "src/AuthService.Domain/"]
COPY ["src/AuthService.Infrastructure.Postgres/AuthService.Infrastructure.Postgres.csproj", "src/AuthService.Infrastructure.Postgres/"]
RUN dotnet restore "./src/AuthService.Web/AuthService.Web.csproj"
COPY . .
WORKDIR "/src/src/AuthService.Web"
RUN dotnet build "./AuthService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AuthService.Web.csproj" -c $BUILD_в CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.Web.dll"]