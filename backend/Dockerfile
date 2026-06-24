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
RUN dotnet publish "./AuthService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM build AS migrations
WORKDIR /src
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project src/AuthService.Infrastructure.Postgres/AuthService.Infrastructure.Postgres.csproj \
    --startup-project src/AuthService.Web/AuthService.Web.csproj \
    --output /efbundle \
    --self-contained \
    -r linux-x64

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrations-final
WORKDIR /app
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
COPY --from=migrations /efbundle ./efbundle
RUN chmod +x ./efbundle
ENTRYPOINT ["./efbundle"]

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AuthService.Web.dll"]