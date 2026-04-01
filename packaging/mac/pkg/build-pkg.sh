#!/usr/bin/env bash
# ============================================================================
# build-pkg.sh — Build macOS .pkg installers for cowsay-dotnet
#
# Prerequisites:
#   - pkgbuild and productbuild (included with Xcode Command Line Tools)
#   - Pre-built binaries from build/publish.ps1 in build/output/
#
# Usage:
#   ./packaging/mac/pkg/build-pkg.sh [RID]
#   ./packaging/mac/pkg/build-pkg.sh              # Build all macOS RIDs
#   ./packaging/mac/pkg/build-pkg.sh osx-arm64    # Build only osx-arm64
# ============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Read version from Directory.Build.props
VERSION=$(grep -oP '(?<=<Version>)[^<]+' "$REPO_ROOT/Directory.Build.props")
PACKAGE_NAME="cowsay-dotnet"
PKG_IDENTIFIER="com.cowsay-dotnet.pkg"

MAC_RIDS=("osx-x64" "osx-arm64")

if [[ $# -ge 1 ]]; then
    MAC_RIDS=("$1")
    if [[ "$1" != "osx-x64" && "$1" != "osx-arm64" ]]; then
        echo "ERROR: Unknown RID '$1'. Valid: osx-x64, osx-arm64"
        exit 1
    fi
fi

BUILD_OUTPUT="$REPO_ROOT/build/output"

for RID in "${MAC_RIDS[@]}"; do
    PKG_OUTPUT="$SCRIPT_DIR/$RID"

    echo "=== Building .pkg for $RID ==="

    # Verify published binaries exist
    COWSAY_BIN="$BUILD_OUTPUT/cowsay/$RID"
    COWTHINK_BIN="$BUILD_OUTPUT/cowthink/$RID"

    if [[ ! -d "$COWSAY_BIN" ]]; then
        echo "ERROR: Published binaries not found at $COWSAY_BIN"
        echo "Run 'build/publish.ps1 -Rid $RID' first."
        exit 1
    fi

    # Create staging area (FHS-like for macOS: /usr/local/)
    STAGING="$PKG_OUTPUT/staging"
    rm -rf "$STAGING"
    mkdir -p "$STAGING/usr/local/bin"
    mkdir -p "$STAGING/usr/local/share/$PACKAGE_NAME/cows"

    # Copy binaries
    cp "$COWSAY_BIN/cowsay" "$STAGING/usr/local/bin/cowsay"
    chmod 755 "$STAGING/usr/local/bin/cowsay"

    if [[ -d "$COWTHINK_BIN" ]]; then
        cp "$COWTHINK_BIN/cowthink" "$STAGING/usr/local/bin/cowthink"
        chmod 755 "$STAGING/usr/local/bin/cowthink"
    fi

    # Copy cow files
    if [[ -d "$COWSAY_BIN/assets/cows" ]]; then
        cp -r "$COWSAY_BIN/assets/cows/"* "$STAGING/usr/local/share/$PACKAGE_NAME/cows/"
    elif [[ -d "$REPO_ROOT/assets/cows" ]]; then
        cp -r "$REPO_ROOT/assets/cows/"* "$STAGING/usr/local/share/$PACKAGE_NAME/cows/"
    fi

    # Create the component package
    mkdir -p "$PKG_OUTPUT"
    COMPONENT_PKG="$PKG_OUTPUT/${PACKAGE_NAME}-component.pkg"

    pkgbuild \
        --root "$STAGING" \
        --identifier "$PKG_IDENTIFIER" \
        --version "$VERSION" \
        --install-location "/" \
        "$COMPONENT_PKG"

    # Create Distribution.xml for productbuild
    DIST_XML="$PKG_OUTPUT/Distribution.xml"
    cat > "$DIST_XML" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<installer-gui-script minSpecVersion="2">
    <title>cowsay-dotnet $VERSION</title>
    <organization>com.cowsay-dotnet</organization>
    <welcome language="en" mime-type="text/plain"><![CDATA[
Welcome to the cowsay-dotnet installer.

This will install the cowsay and cowthink command-line tools
to /usr/local/bin/ and cow art files to /usr/local/share/cowsay-dotnet/cows/.
    ]]></welcome>
    <license file="LICENSE" mime-type="text/plain"/>
    <options customize="never" require-scripts="false"/>
    <choices-outline>
        <line choice="default">
            <line choice="$PKG_IDENTIFIER"/>
        </line>
    </choices-outline>
    <choice id="default"/>
    <choice id="$PKG_IDENTIFIER" visible="false">
        <pkg-ref id="$PKG_IDENTIFIER"/>
    </choice>
    <pkg-ref id="$PKG_IDENTIFIER" version="$VERSION">${PACKAGE_NAME}-component.pkg</pkg-ref>
</installer-gui-script>
EOF

    # Build the product package (final .pkg)
    FINAL_PKG="$PKG_OUTPUT/${PACKAGE_NAME}-${VERSION}-${RID}.pkg"

    productbuild \
        --distribution "$DIST_XML" \
        --package-path "$PKG_OUTPUT" \
        --resources "$REPO_ROOT" \
        "$FINAL_PKG"

    # Clean up intermediate files
    rm -rf "$STAGING" "$COMPONENT_PKG" "$DIST_XML"

    echo "  -> $FINAL_PKG"
    echo ""
done

echo "=== .pkg build complete ==="
