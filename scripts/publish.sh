#!/usr/bin/env bash
# ==============================================================================
# Script di pubblicazione post-build per Valheim Mod Manager (Linux / Steam Deck / CI)
# ==============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
APP_PROJ="${REPO_ROOT}/src/ValheimModManager.App/ValheimModManager.App.csproj"
SOLUTION="${REPO_ROOT}/ValheimModManager.slnx"

RUNTIME="all"
CONFIG="Release"
VERSION=""
OUTPUT_DIR="dist"
SKIP_TESTS=false
FRAMEWORK_DEP=false
NO_ARCHIVE=false
CLEAN=false

print_usage() {
    echo "Uso: $0 [-r runtime] [-c config] [-v version] [-o outdir] [-t] [-f] [-n] [-x]"
    echo "  -r  Target runtime: 'all', 'win-x64', 'linux-x64' (default: all)"
    echo "  -c  Configurazione: 'Release' o 'Debug' (default: Release)"
    echo "  -v  Versione applicazione (default: letta da csproj)"
    echo "  -o  Cartella di destinazione (default: dist)"
    echo "  -t  Salta l'esecuzione dei test"
    echo "  -f  Framework-dependent (non include runtime .NET 10)"
    echo "  -n  Non creare archivi compressi"
    echo "  -x  Pulisce la cartella di output prima del publish"
}

while getopts "r:c:v:o:tfnxh" opt; do
    case "$opt" in
        r) RUNTIME="$OPTARG" ;;
        c) CONFIG="$OPTARG" ;;
        v) VERSION="$OPTARG" ;;
        o) OUTPUT_DIR="$OPTARG" ;;
        t) SKIP_TESTS=true ;;
        f) FRAMEWORK_DEP=true ;;
        n) NO_ARCHIVE=true ;;
        x) CLEAN=true ;;
        h) print_usage; exit 0 ;;
        *) print_usage; exit 1 ;;
    esac
done

DIST_PATH="${REPO_ROOT}/${OUTPUT_DIR}"

echo "=========================================================="
echo "  Valheim Mod Manager - Automated Post-Build Publisher    "
echo "=========================================================="
echo "Repo Root:      ${REPO_ROOT}"
echo "Configurazione: ${CONFIG}"

# 1. Recupero Versione
if [ -z "${VERSION}" ] && [ -n "${GITHUB_REF_NAME:-}" ]; then
    VERSION="${GITHUB_REF_NAME}"
fi

if [ -n "${VERSION}" ]; then
    # Rimuovi eventuale prefisso 'v' o 'V' (es. v1.0.3 -> 1.0.3)
    VERSION="${VERSION#[vV]}"
else
    if [ -f "${APP_PROJ}" ]; then
        VERSION=$(grep -oPm1 "(?<=<Version>)[^<]+" "${APP_PROJ}" || echo "1.0.0")
    else
        VERSION="1.0.0"
    fi
fi
echo "Versione:       ${VERSION}"

# 2. Pulizia
if [ "${CLEAN}" = true ] && [ -d "${DIST_PATH}" ]; then
    echo -e "\n[Pulizia] Rimozione cartella ${DIST_PATH}..."
    rm -rf "${DIST_PATH}"
fi
mkdir -p "${DIST_PATH}"

# 3. Test preventivi
if [ "${SKIP_TESTS}" = false ]; then
    echo -e "\n[1/4] Esecuzione suite di test automatizzati..."
    dotnet test "${SOLUTION}" -c "${CONFIG}" --nologo
    echo "Tutti i test sono stati superati con successo!"
else
    echo -e "\n[1/4] Test automatizzati ignorati (-t specificato)."
fi

# 4. Runtimes
TARGET_RUNTIMES=()
if [ "${RUNTIME}" = "all" ]; then
    TARGET_RUNTIMES=("win-x64" "linux-x64")
else
    TARGET_RUNTIMES=("${RUNTIME}")
fi

SELF_CONTAINED=true
if [ "${FRAMEWORK_DEP}" = true ]; then
    SELF_CONTAINED=false
fi

GENERATED_PACKAGES=()

echo -e "\n[2/4] Ripristino dipendenze e Pubblicazione pacchetti..."

for rid in "${TARGET_RUNTIMES[@]}"; do
    echo -e "\n--> Ripristino dipendenze per runtime: ${rid}..."
    dotnet restore "${SOLUTION}" -r "${rid}" --verbosity normal

    echo -e "\n--> Pubblicazione target: ${rid} (SelfContained=${SELF_CONTAINED}, SingleFile=True)..."
    TARGET_DIST="${DIST_PATH}/${rid}"
    rm -rf "${TARGET_DIST}"
    mkdir -p "${TARGET_DIST}"

    dotnet publish "${APP_PROJ}" \
        -c "${CONFIG}" \
        -r "${rid}" \
        --self-contained "${SELF_CONTAINED}" \
        --no-restore \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:PublishReadyToRun=false \
        -p:PublishTrimmed=false \
        -p:EnableCompressionInSingleFile=true \
        -p:Version="${VERSION}" \
        -o "${TARGET_DIST}" \
        --nologo

    # Rimuovi file debug (.pdb)
    rm -f "${TARGET_DIST}"/*.pdb || true

    # Rendi eseguibile il binario Linux
    if [[ "${rid}" == linux* ]]; then
        chmod +x "${TARGET_DIST}/ValheimModManager" || true
    fi

    echo "Target ${rid} generato in: ${TARGET_DIST}"

    # 5. Creazione archivi
    if [ "${NO_ARCHIVE}" = false ]; then
        echo "[3/4] Creazione archivio compresso per ${rid}..."
        if [[ "${rid}" == win* ]]; then
            ZIP_NAME="ValheimModManager-${VERSION}-${rid}.zip"
            ZIP_PATH="${DIST_PATH}/${ZIP_NAME}"
            rm -f "${ZIP_PATH}"
            (cd "${TARGET_DIST}" && zip -q -r "${ZIP_PATH}" ./*)
            GENERATED_PACKAGES+=("${ZIP_PATH}")
            echo "Archivio creato: ${ZIP_PATH}"
        elif [[ "${rid}" == linux* ]]; then
            TAR_NAME="ValheimModManager-${VERSION}-${rid}.tar.gz"
            TAR_PATH="${DIST_PATH}/${TAR_NAME}"
            rm -f "${TAR_PATH}"
            (cd "${TARGET_DIST}" && tar -czf "${TAR_PATH}" ./*)
            GENERATED_PACKAGES+=("${TAR_PATH}")
            echo "Archivio creato: ${TAR_PATH}"
        fi
    fi
done

# 6. Checksum
echo -e "\n[4/4] Calcolo Checksum SHA-256..."
CHECKSUM_FILE="${DIST_PATH}/SHA256SUMS.txt"
rm -f "${CHECKSUM_FILE}"
for pkg in "${DIST_PATH}"/*.zip "${DIST_PATH}"/*.tar.gz; do
    [ -e "${pkg}" ] || continue
    if command -v sha256sum >/dev/null 2>&1; then
        (cd "${DIST_PATH}" && sha256sum "$(basename "${pkg}")" >> "${CHECKSUM_FILE}")
    elif command -v shasum >/dev/null 2>&1; then
        (cd "${DIST_PATH}" && shasum -a 256 "$(basename "${pkg}")" >> "${CHECKSUM_FILE}")
    fi
done
if [ -f "${CHECKSUM_FILE}" ]; then
    echo "Checksum salvati in: ${CHECKSUM_FILE}"
fi

echo -e "\n=========================================================="
echo "  PUBBLICAZIONE COMPLETATA CON SUCCESSO!                  "
echo "=========================================================="
echo "Cartella artefatti: ${DIST_PATH}"
ls -lh "${DIST_PATH}"
