# Sand Martin 🐦

Sand Martin is an MCP (Model Context Protocol) server that enables real-time orchestration of the Grasshopper canvas within Rhino. It allows Large Language Models (like Claude) to create components, inject Python code, and wire nodes together directly in a live Grasshopper session.

## Architecture

Sand Martin uses a **Client-Server model**:

1.  **SandMartin.Host (C#)**: A Grasshopper plugin (`.gha`) that runs an internal HTTP server inside the Rhino process. It has direct access to the `Grasshopper.Kernel` API.
2.  **Sand Martin Bridge (Python)**: A lightweight MCP server that translates LLM requests into commands for the Host server.

## Getting Started

### 1. Requirements
- Rhino 7 or 8 (macOS/Windows)
- .NET SDK 6.0+ (for building the Host)
- Python 3.10+

### 2. Build & Install the Host Plugin
From the root directory:
```bash
dotnet build
```
The build process will automatically attempt to deploy the `.gha` plugin to your Rhino Libraries folder.

### 3. Install the Python Bridge
```bash
pip install -e .
```

## Configuration for Claude Desktop

Add the following to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "sand-martin": {
      "command": "python",
      "args": ["/PATH/TO/YOUR/sand-martin/src/sand_martin/server.py"]
    }
  }
}
```
*Note: Replace `/PATH/TO/YOUR/` with the actual absolute path to this repository.*

## Usage

1.  **Start Rhino** and open **Grasshopper**.
2.  Launch **Claude Desktop**.
3.  You can now ask Claude to:
    - *"Create a Python component that calculates a Fibonacci sequence."*
    - *"Connect a Slider to the input of my component."*
    - *"Show me the current state of my canvas."*

## Project Structure
- `src/SandMartin.Host/`: C# source for the Grasshopper plugin.
- `src/sand_martin/`: Python source for the MCP server.
- `sand-martin.sln`: Visual Studio Solution file.
- `pyproject.toml`: Python project configuration.

## License
This project is licensed under the [Apache License 2.0](LICENSE).
