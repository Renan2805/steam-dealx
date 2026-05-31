# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first — camada de cache para dotnet restore
COPY src/DealsAggregator.Core/DealsAggregator.Core.csproj                       src/DealsAggregator.Core/
COPY src/DealsAggregator.Clients/DealsAggregator.Clients.csproj                 src/DealsAggregator.Clients/
COPY src/DealsAggregator.Infrastructure/DealsAggregator.Infrastructure.csproj   src/DealsAggregator.Infrastructure/
COPY src/DealsAggregator.Api/DealsAggregator.Api.csproj                         src/DealsAggregator.Api/

RUN dotnet restore src/DealsAggregator.Api/DealsAggregator.Api.csproj

COPY src/ src/

RUN dotnet publish src/DealsAggregator.Api/DealsAggregator.Api.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage — imagem menor sem o SDK
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "DealsAggregator.Api.dll"]
