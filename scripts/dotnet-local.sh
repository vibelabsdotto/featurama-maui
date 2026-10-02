#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
export DOTNET_ROOT="$root/.hermes/dotnet"
export DOTNET_CLI_HOME="$root/.hermes/dotnet-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
if [[ ! -x "$DOTNET_ROOT/dotnet" ]]; then
  printf '%s\n' 'Run bash scripts/setup-toolchain.sh first.' >&2
  exit 1
fi
cd "$root"
exec "$DOTNET_ROOT/dotnet" "$@"
