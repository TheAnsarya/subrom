#!/bin/bash
# Subrom Installation Script for macOS
# Run with: sudo ./install.sh

set -e

INSTALL_DIR="/Applications/Subrom"
PLIST_FILE="$HOME/Library/LaunchAgents/com.subrom.server.plist"

echo "=== Subrom macOS Installation ==="
echo ""

# Create install directory
echo "Creating installation directory: $INSTALL_DIR"
sudo mkdir -p "$INSTALL_DIR"

# Copy files (assumes script is run from release directory)
echo "Copying application files..."
sudo cp -r ./* "$INSTALL_DIR/"
sudo rm -f "$INSTALL_DIR/install.sh"  # Don't copy this script

# Set permissions
echo "Setting permissions..."
sudo chmod +x "$INSTALL_DIR/Subrom.Server"

# Create data directory
echo "Creating data directory..."
mkdir -p "$HOME/Library/Application Support/Subrom"
mkdir -p "$HOME/Library/Logs/Subrom"

# Install launchd plist (user agent, not system daemon)
echo "Installing launchd agent..."
mkdir -p "$HOME/Library/LaunchAgents"
cp "$INSTALL_DIR/com.subrom.server.plist" "$PLIST_FILE"

echo ""
echo "=== Installation Complete ==="
echo ""
echo "To start Subrom:"
echo "  launchctl load $PLIST_FILE"
echo ""
echo "To stop Subrom:"
echo "  launchctl unload $PLIST_FILE"
echo ""
echo "To start automatically on login (already configured):"
echo "  The service will start automatically on next login."
echo ""
echo "To view logs:"
echo "  tail -f /tmp/subrom.log"
echo "  tail -f ~/Library/Logs/Subrom/subrom-*.log"
echo ""
echo "Web UI available at: http://localhost:52100"
echo ""
