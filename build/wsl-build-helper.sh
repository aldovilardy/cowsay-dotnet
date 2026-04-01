#!/usr/bin/env bash
# ============================================================================
# wsl-build-helper.sh — WSL helper for building Linux packages
#
# Solves the /mnt/c/ permissions problem by staging inside /tmp/ (native Linux
# filesystem with proper permissions) and copying results back.
#
# Called by build/package-all.ps1 — NOT intended for standalone use.
#
# Usage:
#   wsl -e bash build/wsl-build-helper.sh <format> <rid> <repo_root_wsl_path>
#
# Formats: deb, rpm
# ============================================================================

set -euo pipefail

FORMAT="${1:?Usage: wsl-build-helper.sh <format> <rid> <repo_root_wsl_path>}"
RID="${2:?Usage: wsl-build-helper.sh <format> <rid> <repo_root_wsl_path>}"
REPO_ROOT="${3:?Usage: wsl-build-helper.sh <format> <rid> <repo_root_wsl_path>}"

VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/Directory.Build.props")
PACKAGE_NAME="cowsay-dotnet"
BUILD_OUTPUT="$REPO_ROOT/build/output"

COWSAY_BIN="$BUILD_OUTPUT/cowsay/$RID"
COWTHINK_BIN="$BUILD_OUTPUT/cowthink/$RID"

if [[ ! -d "$COWSAY_BIN" ]]; then
    echo "ERROR: Published binaries not found at $COWSAY_BIN"
    exit 1
fi

# Create a temp directory on the native Linux filesystem (proper permissions)
WORK_DIR=$(mktemp -d "/tmp/cowsay-pkg-XXXXXX")
trap 'rm -rf "$WORK_DIR"' EXIT

# ════════════════════════════════════════════════════════════════════════════
# DEB
# ════════════════════════════════════════════════════════════════════════════
if [[ "$FORMAT" == "deb" ]]; then
    # Architecture mapping
    declare -A RID_TO_ARCH=(
        ["linux-x64"]="amd64"
        ["linux-arm"]="armhf"
        ["linux-arm64"]="arm64"
    )
    ARCH="${RID_TO_ARCH[$RID]}"
    DEB_FILE="${PACKAGE_NAME}_${VERSION}_${ARCH}.deb"

    echo "=== Building .deb for $RID ($ARCH) ==="

    STAGING="$WORK_DIR/staging"
    mkdir -p "$STAGING/DEBIAN"
    mkdir -p "$STAGING/usr/bin"
    mkdir -p "$STAGING/usr/share/$PACKAGE_NAME/cows"
    mkdir -p "$STAGING/usr/share/doc/$PACKAGE_NAME"

    # Proper permissions for DEBIAN control directory
    chmod 755 "$STAGING/DEBIAN"

    # Create DEBIAN/control
    INSTALLED_SIZE=$(du -sk "$COWSAY_BIN" | cut -f1)
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
Installed-Size: $INSTALLED_SIZE
EOF

    # Create DEBIAN/postinst
    cat > "$STAGING/DEBIAN/postinst" <<'POSTINST'
#!/bin/sh
set -e
chmod +x /usr/bin/cowsay /usr/bin/cowthink
POSTINST
    chmod 755 "$STAGING/DEBIAN/postinst"

    # Copy binaries
    cp "$COWSAY_BIN/cowsay" "$STAGING/usr/bin/cowsay"
    chmod 755 "$STAGING/usr/bin/cowsay"

    if [[ -d "$COWTHINK_BIN" && -f "$COWTHINK_BIN/cowthink" ]]; then
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
    dpkg-deb --build --root-owner-group "$STAGING" "$WORK_DIR/$DEB_FILE"

    # Copy result back to Windows filesystem
    OUTPUT_DIR="$REPO_ROOT/packaging/linux/deb/$RID"
    mkdir -p "$OUTPUT_DIR"
    cp "$WORK_DIR/$DEB_FILE" "$OUTPUT_DIR/$DEB_FILE"

    echo "  -> $OUTPUT_DIR/$DEB_FILE"

# ════════════════════════════════════════════════════════════════════════════
# RPM
# ════════════════════════════════════════════════════════════════════════════
elif [[ "$FORMAT" == "rpm" ]]; then
    declare -A RID_TO_ARCH=(
        ["linux-x64"]="x86_64"
        ["linux-arm"]="armv7hl"
        ["linux-arm64"]="aarch64"
    )
    ARCH="${RID_TO_ARCH[$RID]}"

    echo "=== Building .rpm for $RID ($ARCH) ==="

    # Set up rpmbuild directory structure in temp
    RPM_BUILDROOT="$WORK_DIR/rpmbuild"
    mkdir -p "$RPM_BUILDROOT"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}

    # Stage source files
    cp "$COWSAY_BIN/cowsay" "$RPM_BUILDROOT/SOURCES/cowsay"
    if [[ -d "$COWTHINK_BIN" && -f "$COWTHINK_BIN/cowthink" ]]; then
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
    SPEC_FILE="$REPO_ROOT/packaging/linux/rpm/cowsay-dotnet.spec"
    cp "$SPEC_FILE" "$RPM_BUILDROOT/SPECS/"

    # Build the RPM
    rpmbuild \
        --define "_topdir $RPM_BUILDROOT" \
        --define "_version $VERSION" \
        --target "$ARCH" \
        -bb "$RPM_BUILDROOT/SPECS/cowsay-dotnet.spec"

    # Copy results back to Windows filesystem
    OUTPUT_DIR="$REPO_ROOT/packaging/linux/rpm/$RID"
    mkdir -p "$OUTPUT_DIR"
    find "$RPM_BUILDROOT/RPMS" -name "*.rpm" -exec cp {} "$OUTPUT_DIR/" \;

    echo "  -> $OUTPUT_DIR/"

else
    echo "ERROR: Unknown format '$FORMAT'. Supported: deb, rpm"
    exit 1
fi

echo ""
echo "=== $FORMAT build complete ==="
