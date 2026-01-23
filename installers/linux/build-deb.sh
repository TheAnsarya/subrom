#!/bin/bash
# Build DEB package for Subrom

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$(dirname "$SCRIPT_DIR")")"
VERSION="${VERSION:-1.2.0}"
ARCH="amd64"

echo "Building Subrom DEB package v$VERSION"

# Create build directory
BUILD_DIR="$SCRIPT_DIR/build/subrom_${VERSION}_${ARCH}"
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

# Create directory structure
mkdir -p "$BUILD_DIR/DEBIAN"
mkdir -p "$BUILD_DIR/opt/subrom"
mkdir -p "$BUILD_DIR/lib/systemd/system"
mkdir -p "$BUILD_DIR/usr/share/applications"
mkdir -p "$BUILD_DIR/usr/share/icons/hicolor/256x256/apps"

# Copy DEBIAN control files
cp "$SCRIPT_DIR/debian/control" "$BUILD_DIR/DEBIAN/"
cp "$SCRIPT_DIR/debian/postinst" "$BUILD_DIR/DEBIAN/"
cp "$SCRIPT_DIR/debian/prerm" "$BUILD_DIR/DEBIAN/"
cp "$SCRIPT_DIR/debian/postrm" "$BUILD_DIR/DEBIAN/"

# Set script permissions
chmod 755 "$BUILD_DIR/DEBIAN/postinst"
chmod 755 "$BUILD_DIR/DEBIAN/prerm"
chmod 755 "$BUILD_DIR/DEBIAN/postrm"

# Update version in control file
sed -i "s/^Version:.*/Version: $VERSION/" "$BUILD_DIR/DEBIAN/control"

# Build and publish .NET app
echo "Publishing Subrom.Server..."
dotnet publish "$ROOT_DIR/src/Subrom.Server/Subrom.Server.csproj" \
	-c Release \
	-r linux-x64 \
	--self-contained true \
	-p:PublishSingleFile=true \
	-p:Version=$VERSION \
	-o "$BUILD_DIR/opt/subrom"

# Copy systemd service
cp "$SCRIPT_DIR/debian/subrom.service" "$BUILD_DIR/lib/systemd/system/"

# Copy desktop entry
cp "$SCRIPT_DIR/debian/subrom.desktop" "$BUILD_DIR/usr/share/applications/"

# Copy icon (if exists)
if [ -f "$SCRIPT_DIR/../common/assets/icon.png" ]; then
	cp "$SCRIPT_DIR/../common/assets/icon.png" "$BUILD_DIR/usr/share/icons/hicolor/256x256/apps/subrom.png"
fi

# Build DEB package
echo "Building DEB package..."
mkdir -p "$SCRIPT_DIR/dist"
dpkg-deb --build "$BUILD_DIR" "$SCRIPT_DIR/dist/subrom_${VERSION}_${ARCH}.deb"

echo "Successfully built: $SCRIPT_DIR/dist/subrom_${VERSION}_${ARCH}.deb"
