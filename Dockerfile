# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore — copy project files first for layer caching
COPY src/PrivatePayDirectory.Core/PrivatePayDirectory.Core.csproj               src/PrivatePayDirectory.Core/
COPY src/PrivatePayDirectory.Infrastructure/PrivatePayDirectory.Infrastructure.csproj  src/PrivatePayDirectory.Infrastructure/
COPY src/PrivatePayDirectory.Web/PrivatePayDirectory.Web.csproj                 src/PrivatePayDirectory.Web/
RUN dotnet restore src/PrivatePayDirectory.Web/PrivatePayDirectory.Web.csproj

# Copy the rest and publish
COPY src/ src/
RUN dotnet publish src/PrivatePayDirectory.Web/PrivatePayDirectory.Web.csproj \
    -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PrivatePayDirectory.Web.dll"]
