# syntax=docker/dockerfile:1

# Build stage: full SDK image for compiling and publishing
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for layer-cached restore.
# Changes to source code won't invalidate the restore layer.
COPY ["MinimalApi/MinimalApi.csproj", "MinimalApi/"]
COPY ["MinimalApi.Core/MinimalApi.Core.csproj", "MinimalApi.Core/"]
COPY ["MinimalApi.Data/MinimalApi.Data.csproj", "MinimalApi.Data/"]
COPY ["MinimalApi.ServiceDefaults/MinimalApi.ServiceDefaults.csproj", "MinimalApi.ServiceDefaults/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Build.targets", "."]
COPY ["Directory.Packages.props", "."]
COPY ["NuGet.config", "."]
RUN dotnet restore "MinimalApi/MinimalApi.csproj"

# Copy remaining source and publish in Release mode
COPY . .
RUN dotnet publish "MinimalApi/MinimalApi.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish

# Runtime stage: Ubuntu Noble Chiseled - distroless-like, minimal attack surface.
# No shell, no package manager, no root; runs as UID 65532 (app) by default.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

# Copy published output from build stage
COPY --from=build /app/publish .

# Explicit non-root user. Chiseled images default to 'app' (UID 65532),
# but declaring it here makes the intent visible in the Dockerfile itself.
USER app

ENTRYPOINT ["dotnet", "MinimalApi.dll"]
