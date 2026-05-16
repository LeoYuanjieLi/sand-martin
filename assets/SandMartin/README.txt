# Sand Martin (v0.2.1) 🐦

Sand Martin is a real-time LLM orchestration host for Grasshopper. It enables Large Language Models to interact directly with the Grasshopper canvas via the Model Context Protocol (MCP).

## Installation
1. Close Rhino and Grasshopper.
2. Place `SandMartin.Host.gha` and `Newtonsoft.Json.dll` into your Grasshopper Libraries folder:
   - **Windows**: `%AppData%\Grasshopper\Libraries`
   - **macOS**: `~/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/Grasshopper (b45a29b1-4343-4035-989e-044e8580d9cf)/Libraries`
3. Restart Rhino and Grasshopper.

## Usage
- Find the **Sand Martin Server** component under the `Sand Martin > Server` tab.
- Set the `Run` input to `True` to start the internal HTTP server.
- The server defaults to port `8081`.

## Security
- The server generates a unique **Bearer Token** every time it starts.
- Check the Rhino Command Line for the token or use the auto-discovery file in your system temp directory.

## License
Licensed under the Apache License, Version 2.0. See the included `LICENSE` file for details.
