#!/bin/bash

# Define common Rhino/Grasshopper library paths for macOS
RHINO_PLUGINS_DIR="$HOME/Library/Application Support/McNeel/Rhinoceros/7.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries"

mkdir -p "$RHINO_PLUGINS_DIR"

# Copy the built GHA and dependencies
cp bin/SandMartin.Host.dll "$RHINO_PLUGINS_DIR/SandMartin.Host.gha"
cp bin/Newtonsoft.Json.dll "$RHINO_PLUGINS_DIR/"

echo "SandMartin.Host deployed to $RHINO_PLUGINS_DIR"
