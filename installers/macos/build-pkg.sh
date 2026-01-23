#!/bin/bash
# Build macOS PKG installer for Subrom

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$(dirname "$SCRIPT_DIR")")"
VERSION="${VERSION:-1.2.0}"

echo "Building Subrom macOS PKG v$VERSION"

# Create build directory
BUILD_DIR="$SCRIPT_DIR/build"
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

# Create app bundle structure
APP_DIR="$BUILD_DIR/Subrom.app"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# Copy Info.plist
cp "$SCRIPT_DIR/Subrom.app/Contents/Info.plist" "$APP_DIR/Contents/"

# Update version in Info.plist
sed -i '' "s/<string>1.2.0<\/string>/<string>$VERSION<\/string>/g" "$APP_DIR/Contents/Info.plist"

# Build and publish .NET app
echo "Publishing Subrom.Server for macOS..."
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
	RID="osx-arm64"
else
	RID="osx-x64"
fi

dotnet publish "$ROOT_DIR/src/Subrom.Server/Subrom.Server.csproj" \
	-c Release \
	-r $RID \
	--self-contained true \
	-p:PublishSingleFile=true \
	-p:Version=$VERSION \
	-o "$APP_DIR/Contents/MacOS"

# Copy icon if exists
if [ -f "$SCRIPT_DIR/../common/assets/icon.icns" ]; then
	cp "$SCRIPT_DIR/../common/assets/icon.icns" "$APP_DIR/Contents/Resources/AppIcon.icns"
fi

# Create component packages
echo "Creating component packages..."

# Server package
pkgbuild \
	--root "$APP_DIR" \
	--install-location "/Applications/Subrom.app" \
	--scripts "$SCRIPT_DIR/Scripts" \
	--identifier "com.subrom.server" \
	--version "$VERSION" \
	"$BUILD_DIR/server.pkg"

# LaunchAgent package
LAUNCHAGENT_DIR="$BUILD_DIR/launchagent"
mkdir -p "$LAUNCHAGENT_DIR/Library/LaunchAgents"
cp "$SCRIPT_DIR/Resources/com.subrom.server.plist" "$LAUNCHAGENT_DIR/Library/LaunchAgents/"

pkgbuild \
	--root "$LAUNCHAGENT_DIR" \
	--install-location "/" \
	--identifier "com.subrom.launchagent" \
	--version "$VERSION" \
	"$BUILD_DIR/launchagent.pkg"

# Create distribution package
echo "Creating distribution package..."
mkdir -p "$SCRIPT_DIR/dist"

# Create resources for installer UI
RESOURCES_DIR="$BUILD_DIR/resources"
mkdir -p "$RESOURCES_DIR"

cat > "$RESOURCES_DIR/welcome.html" << EOF
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>Welcome</title></head>
<body>
<h1>Welcome to Subrom $VERSION</h1>
<p>This installer will guide you through the installation of Subrom, a ROM management and verification toolkit.</p>
<p>Features:</p>
<ul>
<li>DAT file parsing (No-Intro, TOSEC, GoodTools)</li>
<li>Multi-format archive support</li>
<li>Real-time scanning with progress</li>
<li>Web-based user interface</li>
</ul>
</body>
</html>
EOF

cat > "$RESOURCES_DIR/license.html" << EOF
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>License</title></head>
<body>
<h1>MIT License</h1>
<p>Copyright © 2026 Subrom Project</p>
<p>Permission is hereby granted, free of charge, to any person obtaining a copy of this software...</p>
</body>
</html>
EOF

cat > "$RESOURCES_DIR/readme.html" << EOF
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>Read Me</title></head>
<body>
<h1>Subrom Installation</h1>
<p>Subrom will be installed to /Applications/Subrom.app</p>
<p>A LaunchAgent will be installed to start Subrom automatically on login.</p>
<p>Access the web interface at <a href="http://localhost:5678">http://localhost:5678</a></p>
</body>
</html>
EOF

cat > "$RESOURCES_DIR/conclusion.html" << EOF
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>Installation Complete</title></head>
<body>
<h1>Installation Complete!</h1>
<p>Subrom has been installed successfully.</p>
<p>The server will start automatically. Access the web interface at:</p>
<p><a href="http://localhost:5678">http://localhost:5678</a></p>
</body>
</html>
EOF

# Copy distribution XML
cp "$SCRIPT_DIR/Distribution.xml" "$BUILD_DIR/"
sed -i '' "s/1.2.0/$VERSION/g" "$BUILD_DIR/Distribution.xml"

productbuild \
	--distribution "$BUILD_DIR/Distribution.xml" \
	--resources "$RESOURCES_DIR" \
	--package-path "$BUILD_DIR" \
	"$SCRIPT_DIR/dist/Subrom-$VERSION.pkg"

echo "Successfully built: $SCRIPT_DIR/dist/Subrom-$VERSION.pkg"

# Optionally create DMG
if command -v create-dmg &> /dev/null; then
	echo "Creating DMG..."
	create-dmg \
		--volname "Subrom $VERSION" \
		--window-size 600 400 \
		--icon-size 128 \
		--icon "Subrom.app" 150 200 \
		--app-drop-link 450 200 \
		"$SCRIPT_DIR/dist/Subrom-$VERSION.dmg" \
		"$APP_DIR"
	echo "Successfully built: $SCRIPT_DIR/dist/Subrom-$VERSION.dmg"
fi

echo "Done!"
