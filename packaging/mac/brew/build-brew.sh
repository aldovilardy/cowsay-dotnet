#!/usr/bin/env bash
# ============================================================================
# build-brew.sh — Build Homebrew tarballs for cowsay-dotnet
#
# Creates tar.gz archives suitable for Homebrew formula distribution.
# The formula (cowsay-dotnet.rb) references these tarballs from GitHub Releases.
#
# Prerequisites:
#   - Pre-built binaries from build/publish.ps1 in build/output/
#
# Usage:
#   ./packaging/mac/brew/build-brew.sh [RID]
#   ./packaging/mac/brew/build-brew.sh              # Build all macOS RIDs
#   ./packaging/mac/brew/build-brew.sh osx-arm64    # Build only osx-arm64
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Read version from Directory.Build.props
VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/Directory.Build.props")
PACKAGE_NAME="cowsay-dotnet"

MAC_RIDS=("osx-x64" "osx-arm64")

if [[ $# -ge 1 ]]; then
    MAC_RIDS=("$1")
    if [[ "$1" != "osx-x64" && "$1" != "osx-arm64" ]]; then
        echo "ERROR: Unknown RID '$1'. Valid: osx-x64, osx-arm64"
        exit 1
    fi
fi

BUILD_OUTPUT="$REPO_ROOT/build/output"

echo "=== Building Homebrew tarballs for $PACKAGE_NAME v$VERSION ==="
echo ""

for RID in "${MAC_RIDS[@]}"; do
    BREW_OUTPUT="$SCRIPT_DIR/$RID"

    echo "--- $RID ---"

    # Verify published binaries exist
    COWSAY_BIN="$BUILD_OUTPUT/cowsay/$RID"
    COWTHINK_BIN="$BUILD_OUTPUT/cowthink/$RID"

    if [[ ! -d "$COWSAY_BIN" ]]; then
        echo "ERROR: Published binaries not found at $COWSAY_BIN"
        echo "Run 'build/publish.ps1 -Rid $RID' first."
        exit 1
    fi

    # Create staging area
    STAGING="$BREW_OUTPUT/staging/${PACKAGE_NAME}-${VERSION}"
    rm -rf "$BREW_OUTPUT/staging"
    mkdir -p "$STAGING/assets/cows"

    # Copy binaries
    cp "$COWSAY_BIN/cowsay" "$STAGING/cowsay"
    chmod 755 "$STAGING/cowsay"

    if [[ -d "$COWTHINK_BIN" ]]; then
        cp "$COWTHINK_BIN/cowthink" "$STAGING/cowthink"
        chmod 755 "$STAGING/cowthink"
    fi

    # Copy cow files
    if [[ -d "$COWSAY_BIN/assets/cows" ]]; then
        cp -r "$COWSAY_BIN/assets/cows/"* "$STAGING/assets/cows/"
    elif [[ -d "$REPO_ROOT/assets/cows" ]]; then
        cp -r "$REPO_ROOT/assets/cows/"* "$STAGING/assets/cows/"
    fi

    # Create tarball
    TARBALL="${PACKAGE_NAME}-${VERSION}-${RID}.tar.gz"
    mkdir -p "$BREW_OUTPUT"
    tar -czf "$BREW_OUTPUT/$TARBALL" -C "$BREW_OUTPUT/staging" "${PACKAGE_NAME}-${VERSION}"

    # Compute SHA256 (sha256sum on Linux, shasum on macOS)
    if command -v sha256sum &>/dev/null; then
        SHA256=$(sha256sum "$BREW_OUTPUT/$TARBALL" | awk '{print $1}')
    else
        SHA256=$(shasum -a 256 "$BREW_OUTPUT/$TARBALL" | awk '{print $1}')
    fi
    echo "  Tarball: $BREW_OUTPUT/$TARBALL"
    echo "  SHA256:  $SHA256"

    # Save SHA256 to a file for formula update
    echo "$SHA256" > "$BREW_OUTPUT/${TARBALL}.sha256"

    # Clean up staging
    rm -rf "$BREW_OUTPUT/staging"
    echo ""
done

echo "=== Homebrew tarball build complete ==="
echo ""
echo "Next steps:"
echo "  1. Upload tarballs to GitHub Releases"
echo "  2. Update SHA256 hashes in cowsay-dotnet.rb formula"
echo "  3. Push formula to your Homebrew tap"
