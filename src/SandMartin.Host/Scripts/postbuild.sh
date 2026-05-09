#!/bin/bash

# Define the exact Grasshopper components path provided by the user for Rhino 8
RHINO_LIBRARIES_DIR="$HOME/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries"

mkdir -p "$RHINO_LIBRARIES_DIR"

# Copy the built GHA and dependencies
cp bin/SandMartin.Host.dll "$RHINO_LIBRARIES_DIR/SandMartin.Host.gha"
cp bin/Newtonsoft.Json.dll "$RHINO_LIBRARIES_DIR/"

echo "SandMartin.Host deployed to $RHINO_LIBRARIES_DIR"
