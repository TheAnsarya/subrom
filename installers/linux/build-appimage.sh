#!/bin/bash
# Build AppImage for Subrom

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$(dirname "$SCRIPT_DIR")")"
VERSION="${VERSION:-1.2.0}"

echo "Building Subrom AppImage v$VERSION"

# Create AppDir structure
APPDIR="$SCRIPT_DIR/build/Subrom.AppDir"
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"

# Publish .NET app
echo "Publishing Subrom.Server..."
dotnet publish "$ROOT_DIR/src/Subrom.Server/Subrom.Server.csproj" \
	-c Release \
	-r linux-x64 \
	--self-contained true \
	-p:PublishSingleFile=true \
	-p:Version=$VERSION \
	-o "$APPDIR/usr/bin"

# Create AppRun script
cat > "$APPDIR/AppRun" << 'EOF'
#!/bin/bash
APPDIR="$(dirname "$(readlink -f "$0")")"
export SUBROM_DATA_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/subrom"
export SUBROM_LOG_DIR="$SUBROM_DATA_DIR/logs"
mkdir -p "$SUBROM_DATA_DIR" "$SUBROM_LOG_DIR"
exec "$APPDIR/usr/bin/Subrom.Server" "$@"
EOF
chmod +x "$APPDIR/AppRun"

# Create desktop file
cat > "$APPDIR/subrom.desktop" << EOF
[Desktop Entry]
Name=Subrom
Comment=ROM management and verification toolkit
Exec=Subrom.Server
Icon=subrom
Terminal=false
Type=Application
Categories=Utility;System;
Keywords=ROM;emulation;DAT;verification;
StartupNotify=true
EOF
cp "$APPDIR/subrom.desktop" "$APPDIR/usr/share/applications/"

# Copy icon (create placeholder if not exists)
if [ -f "$SCRIPT_DIR/../common/assets/icon.png" ]; then
	cp "$SCRIPT_DIR/../common/assets/icon.png" "$APPDIR/subrom.png"
	cp "$SCRIPT_DIR/../common/assets/icon.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/subrom.png"
else
	# Create a simple placeholder icon (1x1 transparent PNG)
	echo "Warning: No icon found, creating placeholder"
	touch "$APPDIR/subrom.png"
fi

# Download appimagetool if not present
APPIMAGETOOL="$SCRIPT_DIR/appimagetool-x86_64.AppImage"
if [ ! -f "$APPIMAGETOOL" ]; then
	echo "Downloading appimagetool..."
	wget -q "https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage" -O "$APPIMAGETOOL"
	chmod +x "$APPIMAGETOOL"
fi

# Build AppImage
echo "Building AppImage..."
mkdir -p "$SCRIPT_DIR/dist"
ARCH=x86_64 "$APPIMAGETOOL" "$APPDIR" "$SCRIPT_DIR/dist/Subrom-$VERSION-x86_64.AppImage"

echo "Successfully built: $SCRIPT_DIR/dist/Subrom-$VERSION-x86_64.AppImage"
