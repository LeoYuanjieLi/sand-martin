#!/bin/bash

# Exit on error
set -e

# Version and paths (Use first argument or default to 0.4.0)
VERSION="${1:-0.4.0}"
PROJECT_ROOT="$(pwd)"
BIN_DIR="src/SandMartin.Host/bin"
DIST_DIR="dist"
RELEASE_NAME="SandMartin_v$VERSION"
RELEASE_FOLDER="$DIST_DIR/$RELEASE_NAME"

echo "📦 Packaging Sand Martin Host v$VERSION..."

# 1. Create clean distribution directory
rm -rf "$RELEASE_FOLDER"
mkdir -p "$RELEASE_FOLDER"

# 2. Copy and rename binaries
if [ -f "$BIN_DIR/SandMartin.Host.dll" ]; then
    cp "$BIN_DIR/SandMartin.Host.dll" "$RELEASE_FOLDER/SandMartin.Host.gha"
    echo "  - Added SandMartin.Host.gha"
else
    echo "❌ Error: SandMartin.Host.dll not found in $BIN_DIR. Please build the project first."
    exit 1
fi

if [ -f "$BIN_DIR/Newtonsoft.Json.dll" ]; then
    cp "$BIN_DIR/Newtonsoft.Json.dll" "$RELEASE_FOLDER/"
    echo "  - Added Newtonsoft.Json.dll"
else
    echo "  - Warning: Newtonsoft.Json.dll not found in $BIN_DIR."
fi

# 3. Copy License
if [ -f "LICENSE" ]; then
    cp "LICENSE" "$RELEASE_FOLDER/"
    echo "  - Added LICENSE"
fi

# 4. Generate README.txt for the plugin
cat <<EOF > "$RELEASE_FOLDER/README.txt"
# Sand Martin (v$VERSION) 🐦

Sand Martin is a real-time LLM orchestration host for Grasshopper. 
It enables Large Language Models to interact directly with the Grasshopper canvas via the Model Context Protocol (MCP).

## Installation
1. Close Rhino and Grasshopper.
2. Place 'SandMartin.Host.gha' and 'Newtonsoft.Json.dll' into your Grasshopper Libraries folder:
   - macOS: ~/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries
   - Windows: %AppData%\\Grasshopper\\Libraries
3. Restart Rhino and Grasshopper.

## Usage
- Find the "Sand Martin Server" component under the "Sand Martin > Server" tab.
- Set the 'Run' input to 'True' to start the internal HTTP server.

## Security
- The server generates a unique Bearer Token every time it starts.
- Check the Rhino Command Line for the token or use the auto-discovery file in your system temp directory.

## License
Licensed under the Apache License, Version 2.0.
EOF
echo "  - Generated README.txt"

# 5. Create Zip Archive
cd "$DIST_DIR"
zip -r "$RELEASE_NAME.zip" "$RELEASE_NAME/" > /dev/null
cd "$PROJECT_ROOT"

echo ""
echo "✅ Release package created: $DIST_DIR/$RELEASE_NAME.zip"
echo "   Ready for upload to GitHub and Food4Rhino."
