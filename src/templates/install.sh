#!/bin/bash
# LAN Chat Avalonia Linux Installer
echo "Menginstall LAN Chat Avalonia..."
INSTALL_DIR="/opt/lanchat-avalonia"
sudo mkdir -p "$INSTALL_DIR"
sudo cp -r ./* "$INSTALL_DIR/"
sudo chmod +x "$INSTALL_DIR/run.sh"
sudo ln -sf "$INSTALL_DIR/run.sh" /usr/local/bin/lanchat
echo "[SUKSES] LAN Chat terinstall. Jalankan perintah 'lanchat' di terminal."
