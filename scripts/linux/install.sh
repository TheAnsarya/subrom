#!/bin/bash
# Subrom Installation Script for Linux
# Run with: sudo ./install.sh

set -e

INSTALL_DIR="/opt/subrom"
SERVICE_USER="subrom"
SERVICE_FILE="/etc/systemd/system/subrom.service"

echo "=== Subrom Linux Installation ==="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "Error: Please run as root (sudo ./install.sh)"
    exit 1
fi

# Create service user if not exists
if ! id "$SERVICE_USER" &>/dev/null; then
    echo "Creating service user: $SERVICE_USER"
    useradd --system --no-create-home --shell /usr/sbin/nologin "$SERVICE_USER"
fi

# Create install directory
echo "Creating installation directory: $INSTALL_DIR"
mkdir -p "$INSTALL_DIR"

# Copy files (assumes script is run from release directory)
echo "Copying application files..."
cp -r ./* "$INSTALL_DIR/"
rm -f "$INSTALL_DIR/install.sh"  # Don't copy this script

# Set permissions
echo "Setting permissions..."
chown -R "$SERVICE_USER:$SERVICE_USER" "$INSTALL_DIR"
chmod +x "$INSTALL_DIR/Subrom.Server"

# Create data directory for user
echo "Creating data directory..."
mkdir -p "/home/$SERVICE_USER/.config/subrom"
chown -R "$SERVICE_USER:$SERVICE_USER" "/home/$SERVICE_USER"

# Install systemd service
echo "Installing systemd service..."
cp "$INSTALL_DIR/subrom.service" "$SERVICE_FILE"
systemctl daemon-reload

echo ""
echo "=== Installation Complete ==="
echo ""
echo "To start Subrom:"
echo "  sudo systemctl start subrom"
echo ""
echo "To enable auto-start on boot:"
echo "  sudo systemctl enable subrom"
echo ""
echo "To check status:"
echo "  sudo systemctl status subrom"
echo ""
echo "To view logs:"
echo "  sudo journalctl -u subrom -f"
echo ""
echo "Web UI available at: http://localhost:52100"
echo ""
