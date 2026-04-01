#!/usr/bin/env bash
# ============================================================================
# build-snap.sh — Build .snap packages for cowsay-dotnet
#
# Prerequisites:
#   - snapcraft (snap install snapcraft --classic)
#   - Pre-built binaries from build/publish.ps1 in build/output/
#
# Usage:
#   ./packaging/linux/snap/build-snap.sh [RID]
#   ./packaging/linux/snap/build-snap.sh              # Build all Linux RIDs
#   ./packaging/linux/snap/build-snap.sh linux-x64    # Build only linux-x64
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Read version from Directory.Build.props
VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/Directory.Build.props")
PACKAGE_NAME="cowsay-dotnet"

# RID to snap architecture mapping
declare -A RID_TO_ARCH=(
    ["linux-x64"]="amd64"
    ["linux-arm"]="armhf"
    ["linux-arm64"]="arm64"
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

for RID in "${LINUX_RIDS[@]}"; do
    ARCH="${RID_TO_ARCH[$RID]}"
    SNAP_OUTPUT="$SCRIPT_DIR/$RID"

    echo "=== Building .snap for $RID ($ARCH) ==="

    # Verify published binaries exist
    COWSAY_BIN="$BUILD_OUTPUT/cowsay/$RID"
    COWTHINK_BIN="$BUILD_OUTPUT/cowthink/$RID"

    if [[ ! -d "$COWSAY_BIN" ]]; then
        echo "ERROR: Published binaries not found at $COWSAY_BIN"
        echo "Run 'build/publish.ps1 -Rid $RID' first."
        exit 1
    fi

    # Create staging area with binaries and snapcraft.yaml
    STAGING="$SNAP_OUTPUT/staging"
    rm -rf "$STAGING"
    mkdir -p "$STAGING/snap"

    # Copy binaries
    cp "$COWSAY_BIN/cowsay" "$STAGING/cowsay"
    chmod 755 "$STAGING/cowsay"

    if [[ -d "$COWTHINK_BIN" ]]; then
        cp "$COWTHINK_BIN/cowthink" "$STAGING/cowthink"
        chmod 755 "$STAGING/cowthink"
    fi

    # Copy cow files
    mkdir -p "$STAGING/assets/cows"
    if [[ -d "$COWSAY_BIN/assets/cows" ]]; then
        cp -r "$COWSAY_BIN/assets/cows/"* "$STAGING/assets/cows/"
    elif [[ -d "$REPO_ROOT/assets/cows" ]]; then
        cp -r "$REPO_ROOT/assets/cows/"* "$STAGING/assets/cows/"
    fi

    # Generate snapcraft.yaml with correct version and architecture
    cat > "$STAGING/snap/snapcraft.yaml" <<EOF
name: $PACKAGE_NAME
base: core22
version: '$VERSION'
summary: A .NET reimplementation of the classic cowsay/cowthink utility
description: |
  cowsay-dotnet is a faithful .NET reimplementation of the classic Unix
  cowsay and cowthink programs. It generates ASCII art of a cow (or other
  characters) saying or thinking a given message.

grade: stable
confinement: strict
license: GPL-3.0-or-later

architectures:
  - build-on: $ARCH

apps:
  cowsay:
    command: usr/bin/cowsay
    plugs:
      - home
      - removable-media
  cowthink:
    command: usr/bin/cowthink
    plugs:
      - home
      - removable-media

parts:
  cowsay-dotnet:
    plugin: dump
    source: .
    organize:
      cowsay: usr/bin/cowsay
      cowthink: usr/bin/cowthink
      "assets/cows/*": usr/share/cowsay-dotnet/cows/
EOF

    # Build the snap
    pushd "$STAGING" > /dev/null
    snapcraft --build-for="$ARCH"
    popd > /dev/null

    # Move the built snap to the output directory
    mkdir -p "$SNAP_OUTPUT"
    find "$STAGING" -maxdepth 1 -name "*.snap" -exec mv {} "$SNAP_OUTPUT/" \;

    # Clean up staging
    rm -rf "$STAGING"

    echo "  -> $SNAP_OUTPUT/"
    echo ""
done

echo "=== .snap build complete ==="
