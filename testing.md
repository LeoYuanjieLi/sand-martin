# Sand Martin Host: Integration Testing Guide

This document provides a set of `curl` commands to manually test the functionality of the Sand Martin Host server running inside Rhino/Grasshopper.

**Prerequisites:**
1. Rhino and Grasshopper are running.
2. The `SandMartin.Host.gha` plugin is loaded, and the server is active (listening on `http://localhost:8081`).
3. You have a command-line terminal with `curl` installed.

---

### 1. Test the `/state` Endpoint

This command fetches the current state of the Grasshopper canvas, listing all components and their properties.

```bash
# Request
curl -X GET http://localhost:8081/state
```

**Expected Response (Example):**
If the canvas is empty, you will receive an empty list of nodes.
```json
{
  "nodes": []
}
```
If there are components on the canvas, they will be listed in the JSON response.

---

### 2. Test the `/create` Endpoint

This command creates a new "Panel" component on the canvas at coordinates (100, 100) and sets its initial text value.

```bash
# Request
curl -X POST http://localhost:8081/create \
-H "Content-Type: application/json" \
-d '{
  "type": "Panel",
  "name": "MyTestPanel",
  "canvasX": 100,
  "canvasY": 100,
  "parameters": {
    "Value": "Hello, Sand Martin!"
  }
}'
```

**Expected Response (Example):**
You should receive a success status and the unique ID of the newly created component.
```json
{
  "status": "success",
  "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef"
}
```
After running this, you should see a new Panel named "MyTestPanel" appear on your Grasshopper canvas.

---

### 3. Test the `/update/{nodeId}` Endpoint

This command updates the component created in the previous step. It changes its name (nickname) and moves it to a new position on the canvas.

**Important:** Replace `"YOUR_NODE_ID"` with the actual `id` you received from the `/create` response.

```bash
# Request
# Remember to replace YOUR_NODE_ID with the actual ID from the /create step.
curl -X PATCH http://localhost:8081/update/YOUR_NODE_ID \
-H "Content-Type: application/json" \
-d '{
  "name": "UpdatedPanel",
  "canvasX": 300,
  "canvasY": 150
}'
```

**Expected Response (Example):**
```json
{
  "status": "success",
  "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef"
}
```
After running this, the panel on your canvas should move to the new coordinates (300, 150) and its nickname should change to "UpdatedPanel".

---

### 4. Advanced Example: Creating a C# Script Component

This command creates a **C# Script** component and injects code into it. The script generates a 5x5x5 grid of spheres with slightly varying random radii.

**Note:** This uses the modern Rhino 8 C# Script format. The code must be properly escaped as a single-line JSON string.

```bash
# Request
curl -X POST http://localhost:8081/create \
-H "Content-Type: application/json" \
-d '{
  "type": "CSharpComponent",
  "name": "Random Spheres Grid",
  "canvasX": 250,
  "canvasY": 250,
  "parameters": {
    "Code": "using System;\nusing System.Collections.Generic;\nusing Rhino.Geometry;\n\nint seed = 42;\nif (x != null) { Int32.TryParse(x.ToString(), out seed); }\n\nvar rand = new Random(seed);\nvar spheres = new List<Sphere>();\n\ndouble spacing = 5.0;\ndouble baseRadius = 2.0;\ndouble variation = 0.5;\n\nfor (int i = 0; i < 5; i++)\n{\n    for (int j = 0; j < 5; j++)\n    {\n        for (int k = 0; k < 5; k++)\n        {\n            var pt = new Point3d(i * spacing, j * spacing, k * spacing);\n            // Radius varies between (baseRadius - variation) and (baseRadius + variation)\n            double currentRadius = baseRadius + (rand.NextDouble() * 2 * variation - variation);\n            spheres.Add(new Sphere(pt, currentRadius));\n        }\n    }\n}\n\na = spheres;"
  }
}'
```

After creating this component, you can manually add a "Number Slider" component and connect it to the default `x` input of the new C# script to control the random seed.

---

### 5. Advanced Example: Updating the C# Script Component

This command uses the `/update/{nodeId}` endpoint to completely overwrite the C# code in the component you just created. Instead of drawing a grid of spheres, it updates the code to draw a grid of slightly varying **BoundingBoxes (Cubes)**.

**Important:** Replace `"YOUR_SCRIPT_NODE_ID"` with the actual `id` you received from the previous `/create` response.

```bash
# Request
curl -X PATCH http://localhost:8081/update/4c62be40-99ef-422f-a417-f54ca8556142 \
-H "Content-Type: application/json" \
-d '{
  "name": "Random Cubes Grid",
  "parameters": {
    "Code": "using System;\nusing System.Collections.Generic;\nusing Rhino.Geometry;\n\nint seed = 42;\nif (x != null) { Int32.TryParse(x.ToString(), out seed); }\n\nvar rand = new Random(seed);\nvar cubes = new List<Box>();\n\ndouble spacing = 5.0;\ndouble baseSize = 2.0;\ndouble variation = 0.5;\n\nfor (int i = 0; i < 5; i++)\n{\n    for (int j = 0; j < 5; j++)\n    {\n        for (int k = 0; k < 5; k++)\n        {\n            // Center point of the cube\n            var center = new Point3d(i * spacing, j * spacing, k * spacing);\n            \n            // Size varies between (baseSize - variation) and (baseSize + variation)\n            double halfSize = (baseSize + (rand.NextDouble() * 2 * variation - variation)) / 2.0;\n            \n            // Create a BoundingBox from the corner points\n            var minPt = new Point3d(center.X - halfSize, center.Y - halfSize, center.Z - halfSize);\n            var maxPt = new Point3d(center.X + halfSize, center.Y + halfSize, center.Z + halfSize);\n            var bbox = new BoundingBox(minPt, maxPt);\n            \n            cubes.Add(new Box(bbox));\n        }\n    }\n}\n\na = cubes;"
  }
}'
```

After running this, the script node's name will change to "Random Cubes Grid", the script editor will immediately update, the component will re-calculate, and the spheres in your Rhino viewport will magically transform into varying cubes!

---

### 6. Test the `/node/{nodeId}` DELETE Endpoint

This command removes a component from the canvas.

**Important:** Replace `"YOUR_NODE_ID"` with the actual `id` of a component you want to delete.

```bash
# Request
# Remember to replace YOUR_NODE_ID with the actual ID.
curl -X DELETE http://localhost:8081/node/YOUR_NODE_ID
```

**Expected Response (Example):**
```json
{
  "status": "success",
  "id": "a1b2c3d4-e5f6-7890-1234-567890abcdef"
}
```
After running this, the component with the specified ID should disappear from your Grasshopper canvas.

---

### 7. Test the `/connection` Endpoint

This command wires two components together. It connects a source output to a target input.

**Important:** Replace `"SOURCE_ID"` and `"TARGET_ID"` with actual GUIDs from your canvas.

```bash
# Request
curl -X POST http://localhost:8081/connection \
-H "Content-Type: application/json" \
-d '{
  "source_id": "2edf0867-8559-47b6-91a7-e50cb8fa0d7b",
  "source_output_index": 0,
  "target_id": "5f96b403-8126-4301-bfee-814a7debf295",
  "target_input_index": 0
}'
```

**Expected Response:**
```json
{
  "status": "success"
}
```

---

### 8. Test the `/disconnect` Endpoint

This command removes all wires between two specific components.

**Important:** Replace `"SOURCE_ID"` and `"TARGET_ID"` with actual GUIDs.

```bash
# Request
curl -X POST http://localhost:8081/disconnect \
-H "Content-Type: application/json" \
-d '{
  "source_id": "2edf0867-8559-47b6-91a7-e50cb8fa0d7b",
  "target_id": "5f96b403-8126-4301-bfee-814a7debf295"
}'
```

**Expected Response:**
```json
{
  "status": "success"
}
```

---

## Unit Testing

In addition to manual integration testing, the project includes automated unit tests for both the C# Host and the Python MCP Server.

### Unified Test Runner

You can run both C# and Python unit tests using the provided shell script:

```bash
./run_tests.sh
```

### 1. C# Host Unit Tests

These tests verify the logic of the `SandMartin.Host` library in isolation (without needing a live Rhino instance).

```bash
# Run all C# tests
dotnet test tests/SandMartin.Host.Tests/SandMartin.Host.Tests.csproj
```

### 2. Python MCP Server Unit Tests

These tests verify the MCP tool logic and HTTP request formatting using mocks for the Host API.

**Prerequisites:**
Setup a virtual environment and install the dependencies:
```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

**Run the tests:**
```bash
# Set PYTHONPATH to include the src directory and run pytest
PYTHONPATH=src pytest tests/sand_martin/test_server.py
```

*Note: The `./run_tests.sh` script will automatically activate `.venv` or `venv` if they exist.*
