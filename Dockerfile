# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY StemCellsPro.ERP.sln .
COPY StemCellsPro.Domain/StemCellsPro.Domain.csproj StemCellsPro.Domain/
COPY StemCellsPro.Application/StemCellsPro.Application.csproj StemCellsPro.Application/
COPY StemCellsPro.Infrastructure/StemCellsPro.Infrastructure.csproj StemCellsPro.Infrastructure/
COPY StemCellsPro.Shared/StemCellsPro.Shared.csproj StemCellsPro.Shared/
COPY StemCellsPro.Api/StemCellsPro.Api.csproj StemCellsPro.Api/

# Restore dependencies
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish StemCellsPro.Api/StemCellsPro.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "StemCellsPro.Api.dll"]
