#!/bin/bash
# LAN Chat Avalonia Linux Launcher
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec dotnet "$SCRIPT_DIR/LanChat.Desktop.dll" "$@"
