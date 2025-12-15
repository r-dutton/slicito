#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
TOOLS_DIR="${REPO_ROOT}/.tools"
Z3_ROOT="${TOOLS_DIR}/z3"
WRAPPER_DIR="${TOOLS_DIR}/bin"
Z3_VERSION="${Z3_VERSION:-4.12.2}"
Z3_ARCHIVE="z3-${Z3_VERSION}-x64-glibc-2.31.zip"
Z3_URL="https://github.com/Z3Prover/z3/releases/download/z3-${Z3_VERSION}/${Z3_ARCHIVE}"

mkdir -p "${Z3_ROOT}" "${WRAPPER_DIR}"
Z3_BIN="${Z3_ROOT}/bin/z3"

if [[ ! -x "${Z3_BIN}" ]]; then
  echo "Downloading Z3 ${Z3_VERSION} from ${Z3_URL}"
  temp_dir="$(mktemp -d)"
  curl -sSL "${Z3_URL}" -o "${temp_dir}/${Z3_ARCHIVE}"
  unzip -q "${temp_dir}/${Z3_ARCHIVE}" -d "${temp_dir}"
  extracted_dir=$(find "${temp_dir}" -maxdepth 1 -type d -name "z3-*" | head -n 1)
  if [[ -z "${extracted_dir}" ]]; then
    echo "Failed to locate extracted Z3 directory" >&2
    exit 1
  fi
  cp -R "${extracted_dir}"/* "${Z3_ROOT}/"
  chmod +x "${Z3_BIN}"
  rm -rf "${temp_dir}"
else
  echo "Z3 already present at ${Z3_BIN}"
fi

cat > "${WRAPPER_DIR}/z3" <<'WRAP'
#!/usr/bin/env bash
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export PATH="${ROOT}/z3/bin:${PATH}"
exec "${ROOT}/z3/bin/z3" "$@"
WRAP
chmod +x "${WRAPPER_DIR}/z3"

echo "Z3 ready: ${WRAPPER_DIR}/z3"
echo "Add to PATH for this session with: export PATH=\"${WRAPPER_DIR}:$PATH\""
