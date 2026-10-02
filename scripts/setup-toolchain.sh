#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$root/.hermes/toolchain"
export DOTNET_CLI_HOME="$root/.hermes/dotnet-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
curl --fail --silent --show-error --location https://dot.net/v1/dotnet-install.sh \
  --output "$root/.hermes/toolchain/dotnet-install.sh"
bash "$root/.hermes/toolchain/dotnet-install.sh" --version 10.0.401 \
  --install-dir "$root/.hermes/dotnet" --no-path
bash "$root/scripts/dotnet-local.sh" workload install maui-ios maui-android maui-maccatalyst --version 10.0.401
bash "$root/scripts/dotnet-local.sh" workload --info
