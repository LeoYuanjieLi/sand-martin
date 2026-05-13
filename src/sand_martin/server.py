import asyncio
import httpx
import json
import logging
import sys
from typing import Optional, Dict, Any
from mcp.server.fastmcp import FastMCP

# Configure logging to stderr (required for MCP servers as stdout is used for communication)
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    stream=sys.stderr
)
logger = logging.getLogger("sand-martin")

# Initialize FastMCP server
mcp = FastMCP("sand-martin")

HOST_URL = "http://localhost:8081"

async def _make_request(method: str, endpoint: str, data: Optional[Dict[str, Any]] = None) -> str:
    """Helper to make requests to the Sand Martin C# Host."""
    logger.info(f"Making {method} request to {endpoint}")
    async with httpx.AsyncClient(timeout=30.0) as client:
        try:
            url = f"{HOST_URL}{endpoint}"
            if method == "GET":
                response = await client.get(url)
            elif method == "POST":
                response = await client.post(url, json=data)
            elif method == "PATCH":
                response = await client.patch(url, json=data)
            elif method == "DELETE":
                response = await client.delete(url)
            else:
                msg = f"Unsupported method: {method}"
                logger.error(msg)
                return json.dumps({"status": "error", "message": msg})

            logger.info(f"Received response: {response.status_code}")
            response.raise_for_status()
            return response.text
        except httpx.HTTPError as e:
            msg = f"HTTP error occurred: {str(e)}"
            logger.error(msg)
            return json.dumps({"status": "error", "message": msg})
        except Exception as e:
            msg = f"An error occurred: {str(e)}"
            logger.error(msg)
            return json.dumps({"status": "error", "message": msg})

@mcp.tool()
async def get_canvas_state() -> str:
    """Returns the current state of the Grasshopper canvas, including all nodes and their parameters."""
    logger.info("Tool called: get_canvas_state")
    return await _make_request("GET", "/state")

@mcp.tool()
async def create_node(
    type: str, 
    name: str, 
    canvas_x: int, 
    canvas_y: int, 
    parameters: Optional[Dict[str, Any]] = None
) -> str:
    """
    Creates a new node (component) on the Grasshopper canvas.
    
    Args:
        type: The type of component to create (e.g., 'Circle', 'Panel', 'CSharpComponent').
        name: The name/nickname for the new node.
        canvas_x: The X coordinate on the canvas.
        canvas_y: The Y coordinate on the canvas.
        parameters: Optional dictionary of parameters to set (e.g., {'Code': '...'} for CSharpComponent).
    """
    logger.info(f"Tool called: create_node (type={type}, name={name})")
    data = {
        "type": type,
        "name": name,
        "canvasX": canvas_x,
        "canvasY": canvas_y,
        "parameters": parameters or {}
    }
    return await _make_request("POST", "/create", data)

@mcp.tool()
async def update_node(
    node_id: str,
    name: Optional[str] = None,
    canvas_x: Optional[int] = None,
    canvas_y: Optional[int] = None,
    parameters: Optional[Dict[str, Any]] = None
) -> str:
    """
    Updates an existing node on the Grasshopper canvas.
    
    Args:
        node_id: The unique ID (GUID) of the node to update.
        name: Optional new name/nickname.
        canvas_x: Optional new X coordinate.
        canvas_y: Optional new Y coordinate.
        parameters: Optional dictionary of parameters to update.
    """
    logger.info(f"Tool called: update_node (node_id={node_id})")
    data = {}
    if name is not None: data["name"] = name
    if canvas_x is not None: data["canvasX"] = canvas_x
    if canvas_y is not None: data["canvasY"] = canvas_y
    if parameters is not None: data["parameters"] = parameters
    
    return await _make_request("PATCH", f"/update/{node_id}", data)

@mcp.tool()
async def delete_node(node_id: str) -> str:
    """
    Deletes a node from the Grasshopper canvas.
    
    Args:
        node_id: The unique ID (GUID) of the node to delete.
    """
    logger.info(f"Tool called: delete_node (node_id={node_id})")
    return await _make_request("DELETE", f"/node/{node_id}")

@mcp.tool()
async def connect_nodes(
    source_id: str,
    target_id: str,
    source_output_index: int = 0,
    target_input_index: int = 0
) -> str:
    """
    Creates a connection (wire) between two nodes on the Grasshopper canvas.
    
    Args:
        source_id: The ID of the node providing the output.
        target_id: The ID of the node receiving the input.
        source_output_index: The index of the output parameter on the source node (default 0).
        target_input_index: The index of the input parameter on the target node (default 0).
    """
    logger.info(f"Tool called: connect_nodes (from={source_id}, to={target_id})")
    data = {
        "source_id": source_id,
        "source_output_index": source_output_index,
        "target_id": target_id,
        "target_input_index": target_input_index
    }
    return await _make_request("POST", "/connection", data)

@mcp.tool()
async def disconnect_nodes(source_id: str, target_id: str) -> str:
    """
    Removes all connections between two specific nodes on the Grasshopper canvas.
    
    Args:
        source_id: The ID of the source node.
        target_id: The ID of the target node.
    """
    logger.info(f"Tool called: disconnect_nodes (from={source_id}, to={target_id})")
    data = {
        "source_id": source_id,
        "target_id": target_id
    }
    return await _make_request("POST", "/disconnect", data)

def main():
    """Entry point for the sand-martin MCP server."""
    mcp.run()

if __name__ == "__main__":
    main()
