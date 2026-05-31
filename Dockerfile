# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first — camada de cache para dotnet restore
COPY src/SteamDealX.Core/SteamDealX.Core.csproj                       src/SteamDealX.Core/
COPY src/SteamDealX.Clients/SteamDealX.Clients.csproj                 src/SteamDealX.Clients/
COPY src/SteamDealX.Infrastructure/SteamDealX.Infrastructure.csproj   src/SteamDealX.Infrastructure/
COPY src/SteamDealX.Api/SteamDealX.Api.csproj                         src/SteamDealX.Api/

RUN dotnet restore src/SteamDealX.Api/SteamDealX.Api.csproj

COPY src/ src/

RUN dotnet publish src/SteamDealX.Api/SteamDealX.Api.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage — imagem menor sem o SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SteamDealX.Api.dll"]
