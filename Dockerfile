# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first so NuGet restore is cached separately
COPY ["Core/Domain/Domain.csproj",                           "Core/Domain/"]
COPY ["Core/Application/Application.csproj",                 "Core/Application/"]
COPY ["Infrastructure/Infrastructure/Infrastructure.csproj", "Infrastructure/Infrastructure/"]
COPY ["Presentation/ASPNET/ASPNET.csproj",                   "Presentation/ASPNET/"]

RUN dotnet restore "Presentation/ASPNET/ASPNET.csproj"

# Copy everything and publish
COPY . .
WORKDIR "/src/Presentation/ASPNET"
RUN dotnet publish "ASPNET.csproj" -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Non-root user for security
RUN groupadd --system --gid 1001 appgroup && \
    useradd  --system --uid 1001 --gid appgroup appuser

# Persistent data directories (images, docs uploaded by users)
RUN mkdir -p /app/wwwroot/app_data/images \
             /app/wwwroot/app_data/docs   && \
    chown -R appuser:appgroup /app

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

# Port 8080 is the standard .NET container port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ASPNET.dll"]
