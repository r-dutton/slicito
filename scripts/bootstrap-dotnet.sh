#!/usr/bin/env bash
set -euo pipefail

# Install the pinned .NET SDK into ./.dotnet and print environment info.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DOTNET_DIR="${REPO_ROOT}/.dotnet"
DOTNET_INSTALL_SCRIPT="${DOTNET_DIR}/dotnet-install.sh"

# Prefer global.json if present; otherwise fall back to a sensible default.
DEFAULT_VERSION="9.0.100"
if [[ -n "${DOTNET_VERSION:-}" ]]; then
  SDK_VERSION="${DOTNET_VERSION}"
elif [[ -f "${REPO_ROOT}/global.json" ]]; then
  SDK_VERSION="$(python - <<'PY'
import json,sys, pathlib
path = pathlib.Path("global.json")
data = json.load(path.open())
print(data.get("sdk", {}).get("version", ""))
PY
)"
  if [[ -z "${SDK_VERSION}" ]]; then
    SDK_VERSION="${DEFAULT_VERSION}"
  fi
else
  SDK_VERSION="${DEFAULT_VERSION}"
fi

mkdir -p "${DOTNET_DIR}"

if [[ ! -x "${DOTNET_DIR}/dotnet" ]] || ! "${DOTNET_DIR}/dotnet" --version | grep -q "${SDK_VERSION}"; then
  echo "Installing .NET SDK ${SDK_VERSION} into ${DOTNET_DIR}"
  curl -sSL https://dot.net/v1/dotnet-install.sh -o "${DOTNET_INSTALL_SCRIPT}"
  chmod +x "${DOTNET_INSTALL_SCRIPT}"
  "${DOTNET_INSTALL_SCRIPT}" --version "${SDK_VERSION}" --install-dir "${DOTNET_DIR}"
else
  echo ".NET SDK ${SDK_VERSION} already installed in ${DOTNET_DIR}"
fi

export DOTNET_ROOT="${DOTNET_DIR}"
export PATH="${DOTNET_DIR}:${PATH}"

"${DOTNET_DIR}/dotnet" --info
