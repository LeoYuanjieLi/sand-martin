# Testing Sand Martin 🧪

Testing a Grasshopper plugin is unique because the core logic depends on the Rhino/Grasshopper UI thread and API, which cannot be easily mocked in a standard CI environment.

## 1. Unit Testing (C#)
We use **xUnit** for logic that is decoupled from the Grasshopper UI.

### How to run:
```bash
dotnet test
```
*Note: These tests currently focus on Request Dispatching and Data Models.*

## 2. Integration Testing (In Grasshopper)
Since the `CanvasManager` needs a live `GH_Document`, the most reliable way to test is inside Rhino.

### Manual Test Procedure:
1.  **Build and Deploy**: Run `dotnet build`.
2.  **Open Rhino & Grasshopper**: Ensure the `SandMartin.Host` plugin is loaded (check the Rhino command line for "SandMartin Server started").
3.  **Smoke Test (Terminal)**:
    Use `curl` to verify the server is responding to the Canvas State request:
    ```bash
    curl http://localhost:8081/state
    ```
4.  **Expected Result**:
    You should receive a JSON object containing a list of nodes currently on your active Grasshopper canvas.
    ```json
    {
      "nodes": [
        { "id": "...", "name": "Circle", "type": "GH_Component", "x": 150.0, "y": 200.0 }
      ]
    }
    ```

## 3. Python MCP Testing
Once the Host is running in Rhino, you can test the Python bridge:

1.  **Run Server**: `python src/sand_martin/server.py`
2.  **Use MCP Inspector**:
    ```bash
    npx @modelcontextprotocol/inspector python src/sand_martin/server.py
    ```
3.  **Call Tool**: Invoke the `get_canvas_state` tool in the inspector to see if it correctly fetches data from Rhino.
