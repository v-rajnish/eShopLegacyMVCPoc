#!/usr/bin/env bash
# Builds and runs the modernized eShopPorted (.NET 8) application locally on Linux/macOS.
set -euo pipefail

CONFIGURATION="${1:-Release}"
URLS="${2:-http://localhost:5080}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/eShopPorted/eShopPorted.csproj"

echo "Restoring & building ($CONFIGURATION)..."
dotnet build "$PROJECT" -c "$CONFIGURATION"

echo "Starting app on $URLS ..."
export ASPNETCORE_URLS="$URLS"
dotnet run --project "$PROJECT" -c "$CONFIGURATION" --no-launch-profile
