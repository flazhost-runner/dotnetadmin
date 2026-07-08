# syntax=docker/dockerfile:1
# ── DotNetAdmin starter kit · FlazHost PaaS (CapRover) ───────────────────────
# Multi-stage build:
#   1) sdk:10.0    → dotnet restore + publish -c Release
#   2) aspnet:10.0 → slim runtime, entrypoint composes DB config from env
#
# Zero-config boot: SQLite at /app/data/dotnetadmin.db, secrets auto-generated
# and persisted in /app/data/.runtime-secrets. Managed MySQL/Postgres via the
# platform's DB_TYPE/DB_HOST/DB_PORT/DB_USERNAME/DB_PASSWORD/DB_DATABASE env.

# 1) Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Cache NuGet restore on the project file first.
COPY DotNetAdmin.csproj ./
RUN dotnet restore DotNetAdmin.csproj

# App source + publish (tests/ and templates/ are excluded by the csproj).
COPY . .
RUN dotnet publish DotNetAdmin.csproj -c Release -o /out /p:UseAppHost=false

# 2) Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# ── Managed/serverless MySQL TLS compat (OpenSSL 3.x SECLEVEL) ────────────────
# RDS MySQL serverless only offers LEGACY TLS ciphers/protocols. .NET's OpenSSL
# 3.x defaults to SECLEVEL=2 and rejects them → "sslv3 alert handshake failure"
# → crash at ServerVersion.AutoDetect() on first connection. Relax OpenSSL to
# accept legacy ciphers (TLS stays ON — same approach as the proven Rust
# native-tls fix; see FlazCloud bug/dotnet.txt). Written inside the Dockerfile
# (not a standalone file) so the app-code mirror sync can't --delete it.
RUN printf '%s\n' \
    'openssl_conf = default_conf' \
    '[default_conf]' \
    'ssl_conf = ssl_sect' \
    '[ssl_sect]' \
    'system_default = system_default_sect' \
    '[system_default_sect]' \
    'CipherString = DEFAULT@SECLEVEL=0' \
    'MinProtocol = TLSv1' \
    > /etc/ssl/openssl-legacy.cnf

COPY --from=build /out .
COPY docker-entrypoint.sh /app/docker-entrypoint.sh

# /app/data              → SQLite DB + persisted runtime secrets (mount a volume
#                          here to survive redeploys)
# wwwroot/storage/editor → local media uploads (MediaService writes here)
RUN chmod +x /app/docker-entrypoint.sh \
 && mkdir -p /app/data /app/wwwroot/storage/editor

# ── Zero-config defaults (all overridable via env) ───────────────────────────
# ASPNETCORE_ENVIRONMENT=Development is REQUIRED for auto-migrate + seed:
# Program.cs only runs Database.Migrate() + DbSeeder.SeedAsync() when the env
# is Development/Test. The entrypoint overrides the dev-json secrets/DB path
# with strong generated values, and dials logging back to Information.
ENV PORT=80 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_EnableDiagnostics=0 \
    OPENSSL_CONF=/etc/ssl/openssl-legacy.cnf

EXPOSE 80
ENTRYPOINT ["/app/docker-entrypoint.sh"]
