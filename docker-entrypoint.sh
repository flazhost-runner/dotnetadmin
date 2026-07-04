#!/bin/sh
# DotNetAdmin container entrypoint (FlazHost / CapRover).
#   1) Map CapRover's $PORT → ASPNETCORE_URLS (Kestrel binds 0.0.0.0:$PORT).
#   2) Ensure App__SessionSecret / App__JwtSecret exist (generate + persist).
#   3) Compose the EF Core connection string from the platform's DB_* env
#      (Database__Type / Database__MySql / Database__Postgres /
#       Database__ConnectionString — .NET double-underscore config keys).
#   4) Exec the app. Migrate + seed happen inside Program.cs at startup
#      (gated on ASPNETCORE_ENVIRONMENT=Development|Test — the Dockerfile
#      defaults to Development so both always run; they are idempotent).
set -eu

DATA_DIR=/app/data
SECRETS_FILE="$DATA_DIR/.runtime-secrets"
mkdir -p "$DATA_DIR"

# ── 1. Port: CapRover injects $PORT (default 80). ────────────────────────────
: "${PORT:=80}"
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://0.0.0.0:${PORT}}"

# Keep auto-migrate + seed enabled; tame the Development.json debug logging.
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export Logging__LogLevel__Default="${Logging__LogLevel__Default:-Information}"
export Logging__LogLevel__Microsoft_AspNetCore="${Logging__LogLevel__Microsoft_AspNetCore:-Warning}"

# ── 2. Secrets: honour env, else generate once and persist across restarts. ──
gen_secret() {
    head -c 32 /dev/urandom | od -An -tx1 | tr -d ' \n'
}

[ -f "$SECRETS_FILE" ] && . "$SECRETS_FILE"

if [ -z "${App__SessionSecret:-}" ]; then
    App__SessionSecret="$(gen_secret)"
    echo "App__SessionSecret=$App__SessionSecret" >> "$SECRETS_FILE"
    echo "[entrypoint] Generated App__SessionSecret (persisted in $SECRETS_FILE)"
fi
if [ -z "${App__JwtSecret:-}" ]; then
    App__JwtSecret="$(gen_secret)"
    echo "App__JwtSecret=$App__JwtSecret" >> "$SECRETS_FILE"
    echo "[entrypoint] Generated App__JwtSecret (persisted in $SECRETS_FILE)"
fi
export App__SessionSecret App__JwtSecret

# ── 3. Database: compose provider config from platform DB_* env. ─────────────
# DB_TYPE empty/sqlite → zero-config SQLite under /app/data.
case "${DB_TYPE:-}" in
    mysql|mariadb)
        export Database__Type="mysql"
        export Database__MySql="Server=${DB_HOST:-127.0.0.1};Port=${DB_PORT:-3306};Database=${DB_DATABASE:-dotnetadmin};User=${DB_USERNAME:-root};Password=${DB_PASSWORD:-}"
        echo "[entrypoint] Database: mysql @ ${DB_HOST:-127.0.0.1}:${DB_PORT:-3306}/${DB_DATABASE:-dotnetadmin}"
        ;;
    postgres|postgresql|pgsql)
        export Database__Type="postgres"
        export Database__Postgres="Host=${DB_HOST:-127.0.0.1};Port=${DB_PORT:-5432};Database=${DB_DATABASE:-dotnetadmin};Username=${DB_USERNAME:-postgres};Password=${DB_PASSWORD:-}"
        echo "[entrypoint] Database: postgres @ ${DB_HOST:-127.0.0.1}:${DB_PORT:-5432}/${DB_DATABASE:-dotnetadmin}"
        ;;
    ""|sqlite)
        export Database__Type="sqlite"
        export Database__ConnectionString="${Database__ConnectionString:-Data Source=$DATA_DIR/dotnetadmin.db}"
        echo "[entrypoint] Database: sqlite ($Database__ConnectionString)"
        ;;
    *)
        echo "[entrypoint] WARN: unknown DB_TYPE='$DB_TYPE' — falling back to sqlite"
        export Database__Type="sqlite"
        export Database__ConnectionString="${Database__ConnectionString:-Data Source=$DATA_DIR/dotnetadmin.db}"
        ;;
esac

# ── 4. Redis (OPTIONAL): only consulted when Session__Driver=redis. ──────────
# Default Session__Driver=database → in-process cache, no Redis required.
# If the platform provides REDIS_URL, wire it up and switch the session driver.
if [ -n "${REDIS_URL:-}" ]; then
    export Redis__Url="$REDIS_URL"
    export Session__Driver="${Session__Driver:-redis}"
    echo "[entrypoint] Redis: $REDIS_URL (Session__Driver=$Session__Driver)"
fi

# ── 5. Start (PID 1 for clean SIGTERM/graceful shutdown). ────────────────────
# Program.cs then runs: Database.Migrate() → DbSeeder.SeedAsync() (creates
# admin@admin.com / 12345678 + Administrator role) → permission sync.
echo "[entrypoint] Starting DotNetAdmin on $ASPNETCORE_URLS (env=$ASPNETCORE_ENVIRONMENT)"
exec dotnet /app/DotNetAdmin.dll
