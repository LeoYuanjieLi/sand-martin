# Plan: Canvas Description Tool

## Objective
Implement an AI-driven description feature for Grasshopper canvases (addressing [Issue #11](https://github.com/LeoYuanjieLi/sand-martin/issues/11)). The goal is to add a consolidated descriptive `GH_Scribble` component to the canvas so that users without a deep Grasshopper background can easily understand the script's logic and purpose.

## Key Files & Context
*   **Target File:** `src/sand_martin/server.py` (MCP Tool implementation)
*   **Host Logic:** `src/SandMartin.Host/Services/NodeManager.cs` (Reflection-based property setting)
*   **Data Models:** `src/SandMartin.Host/Models/ApiModels.cs` (Includes Width/Height metadata)

## Implementation Steps

### 1. Enable Clear Description Placement
To avoid overlap with existing components, the description tool places a single master `GH_Scribble` above the script using the minimum Y position of the described nodes and a fixed vertical offset.

### 2. Implement Description Logic
The `add_description` tool follows these steps:
1.  **State Retrieval:** Calls `_make_request("GET", "/state")` to fetch the complete canvas graph, including component dimensions.
2.  **Semantic Clustering:** The agent identifies logical modules (e.g., "Setup", "Processing", "Visualization").
3.  **Cleanup:** Automatically deletes previous "Section Label" and "Canvas Description" scribbles to prevent clutter.
4.  **Description Consolidation:** Combines the module names into a single `SCRIPT DOCUMENTATION` block.
5.  **Safe Placement:** Places one `GH_Scribble` named "Canvas Description" above the top-most described node with a fixed safety margin.
6.  **Explanatory Content:** Uses reflection to write the consolidated description into the `Text` property of the scribble.

### 3. MCP Prompt Integration
A `describe_canvas` prompt is provided to guide AI agents in acting as "Technical Writers." It instructs them to analyze the graph topology and formulate clear, concise descriptions for each major logic block, then call `add_description`.

## Verification & Testing
1.  **Manual Trigger:** Call `add_description(semantic_clusters=...)` via an MCP client.
2.  **Validation:** 
    *   A descriptive "Canvas Description" scribble appears above the script.
    *   The scribble is fully populated with text (no "Doubleclick Me!").
    *   The scribble does not overlap with components.
    *   The overall purpose of the script is clearly documented for non-experts.
