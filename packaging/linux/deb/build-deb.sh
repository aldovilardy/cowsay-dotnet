#!/usr/bin/env bash
# ============================================================================
# build-deb.sh — Build .deb packages for cowsay-dotnet
#
# Prerequisites:
#   - dpkg-deb (from dpkg package)
#   - Pre-built binaries from build/publish.ps1 in build/output/
#
# Usage:
#   ./packaging/linux/deb/build-deb.sh [RID]
#   ./packaging/linux/deb/build-deb.sh              # Build all Linux RIDs
#   ./packaging/linux/deb/build-deb.sh linux-x64    # Build only linux-x64
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Read version from Directory.Build.props
VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/Directory.Build.props")
PACKAGE_NAME="cowsay-dotnet"

# RID to Debian architecture mapping
declare -A RID_TO_ARCH=(
    ["linux-x64"]="amd64"
    ["linux-arm"]="armhf"
    ["linux-arm64"]="arm64"
)

LINUX_RIDS=("linux-x64" "linux-arm" "linux-arm64")

# If a specific RID is provided, build only that one
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
    DEB_OUTPUT="$SCRIPT_DIR/$RID"
    DEB_FILE="${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"

    echo "=== Building .deb for $RID ($ARCH) ==="

    # Verify published binaries exist
    COWSAY_BIN="$BUILD_OUTPUT/cowsay/$RID"
    COWTHINK_BIN="$BUILD_OUTPUT/cowthink/$RID"

    if [[ ! -d "$COWSAY_BIN" ]]; then
        echo "ERROR: Published binaries not found at $COWSAY_BIN"
        echo "Run 'build/publish.ps1 -Rid $RID' first."
        exit 1
    fi

    # Clean and create staging directory
    STAGING="$DEB_OUTPUT/staging"
    rm -rf "$STAGING"
    mkdir -p "$STAGING/DEBIAN"
    mkdir -p "$STAGING/usr/bin"
    mkdir -p "$STAGING/usr/share/$PACKAGE_NAME/cows"
    mkdir -p "$STAGING/usr/share/doc/$PACKAGE_NAME"

    # Create DEBIAN/control
    cat > "$STAGING/DEBIAN/control" <<EOF
Package: $PACKAGE_NAME
Version: $VERSION
Section: games
Priority: optional
Architecture: $ARCH
Maintainer: cowsay-dotnet contributors <cowsay-dotnet@users.noreply.github.com>
Description: A .NET reimplementation of the classic cowsay/cowthink utility
 cowsay-dotnet is a faithful .NET reimplementation of the classic Unix
 cowsay and cowthink programs. It generates ASCII art of a cow (or other
 characters) saying or thinking a given message.
 .
 This package provides the cowsay and cowthink command-line tools as
 self-contained binaries with no external runtime dependencies.
Homepage: https://github.com/cowsay-dotnet/cowsay-dotnet
Installed-Size: $(du -sk "$COWSAY_BIN" | cut -f1)
EOF

    # Create DEBIAN/postinst (set executable permissions)
    cat > "$STAGING/DEBIAN/postinst" <<'EOF'
#!/bin/sh
set -e
chmod +x /usr/bin/cowsay /usr/bin/cowthink
EOF
    chmod 755 "$STAGING/DEBIAN/postinst"

    # Copy binaries
    cp "$COWSAY_BIN/cowsay" "$STAGING/usr/bin/cowsay"
    chmod 755 "$STAGING/usr/bin/cowsay"

    if [[ -d "$COWTHINK_BIN" ]]; then
        cp "$COWTHINK_BIN/cowthink" "$STAGING/usr/bin/cowthink"
        chmod 755 "$STAGING/usr/bin/cowthink"
    fi

    # Copy cow files
    if [[ -d "$COWSAY_BIN/assets/cows" ]]; then
        cp -r "$COWSAY_BIN/assets/cows/"* "$STAGING/usr/share/$PACKAGE_NAME/cows/"
    elif [[ -d "$REPO_ROOT/assets/cows" ]]; then
        cp -r "$REPO_ROOT/assets/cows/"* "$STAGING/usr/share/$PACKAGE_NAME/cows/"
    fi

    # Copy license
    if [[ -f "$REPO_ROOT/LICENSE" ]]; then
        cp "$REPO_ROOT/LICENSE" "$STAGING/usr/share/doc/$PACKAGE_NAME/copyright"
    fi

    # Build the .deb
    mkdir -p "$DEB_OUTPUT"
    dpkg-deb --build --root-owner-group "$STAGING" "$DEB_OUTPUT/$DEB_FILE"

    # Clean up staging
    rm -rf "$STAGING"

    echo "  -> $DEB_OUTPUT/$DEB_FILE"
    echo ""
done

echo "=== .deb build complete ==="
