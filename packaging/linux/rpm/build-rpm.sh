#!/usr/bin/env bash
# ============================================================================
# build-rpm.sh — Build .rpm packages for cowsay-dotnet
#
# Prerequisites:
#   - rpmbuild (from rpm-build package)
#   - Pre-built binaries from build/publish.ps1 in build/output/
#
# Usage:
#   ./packaging/linux/rpm/build-rpm.sh [RID]
#   ./packaging/linux/rpm/build-rpm.sh              # Build all Linux RIDs
#   ./packaging/linux/rpm/build-rpm.sh linux-x64    # Build only linux-x64
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Read version from Directory.Build.props
VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/Directory.Build.props")
PACKAGE_NAME="cowsay-dotnet"

# RID to RPM architecture mapping
declare -A RID_TO_ARCH=(
    ["linux-x64"]="x86_64"
    ["linux-arm"]="armv7hl"
    ["linux-arm64"]="aarch64"
)

LINUX_RIDS=("linux-x64" "linux-arm" "linux-arm64")

if [[ $# -ge 1 ]]; then
    LINUX_RIDS=("$1")
    if [[ -z "${RID_TO_ARCH[$1]+x}" ]]; then
        echo "ERROR: Unknown RID '$1'. Valid: linux-x64, linux-arm, linux-arm64"
        exit 1
    fi
fi

BUILD_OUTPUT="$REPO_ROOT/build/output"
SPEC_FILE="$SCRIPT_DIR/cowsay-dotnet.spec"

for RID in "${LINUX_RIDS[@]}"; do
    ARCH="${RID_TO_ARCH[$RID]}"
    RPM_OUTPUT="$SCRIPT_DIR/$RID"

    echo "=== Building .rpm for $RID ($ARCH) ==="

    # Verify published binaries exist
    COWSAY_BIN="$BUILD_OUTPUT/cowsay/$RID"
    COWTHINK_BIN="$BUILD_OUTPUT/cowthink/$RID"

    if [[ ! -d "$COWSAY_BIN" ]]; then
        echo "ERROR: Published binaries not found at $COWSAY_BIN"
        echo "Run 'build/publish.ps1 -Rid $RID' first."
        exit 1
    fi

    # Set up rpmbuild directory structure
    RPM_BUILDROOT="$RPM_OUTPUT/rpmbuild"
    rm -rf "$RPM_BUILDROOT"
    mkdir -p "$RPM_BUILDROOT"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}

    # Stage source files
    cp "$COWSAY_BIN/cowsay" "$RPM_BUILDROOT/SOURCES/cowsay"
    if [[ -d "$COWTHINK_BIN" ]]; then
        cp "$COWTHINK_BIN/cowthink" "$RPM_BUILDROOT/SOURCES/cowthink"
    fi

    # Copy cow files
    mkdir -p "$RPM_BUILDROOT/SOURCES/assets/cows"
    if [[ -d "$COWSAY_BIN/assets/cows" ]]; then
        cp -r "$COWSAY_BIN/assets/cows/"* "$RPM_BUILDROOT/SOURCES/assets/cows/"
    elif [[ -d "$REPO_ROOT/assets/cows" ]]; then
        cp -r "$REPO_ROOT/assets/cows/"* "$RPM_BUILDROOT/SOURCES/assets/cows/"
    fi

    # Copy license
    if [[ -f "$REPO_ROOT/LICENSE" ]]; then
        cp "$REPO_ROOT/LICENSE" "$RPM_BUILDROOT/SOURCES/LICENSE"
    fi

    # Copy spec file
    cp "$SPEC_FILE" "$RPM_BUILDROOT/SPECS/"

    # Build the RPM
    rpmbuild \
        --define "_topdir $RPM_BUILDROOT" \
        --define "_version $VERSION" \
        --target "$ARCH" \
        -bb "$RPM_BUILDROOT/SPECS/cowsay-dotnet.spec"

    # Move the built RPM to the output directory
    mkdir -p "$RPM_OUTPUT"
    find "$RPM_BUILDROOT/RPMS" -name "*.rpm" -exec mv {} "$RPM_OUTPUT/" \;

    # Clean up
    rm -rf "$RPM_BUILDROOT"

    echo "  -> $RPM_OUTPUT/"
    echo ""
done

echo "=== .rpm build complete ==="
