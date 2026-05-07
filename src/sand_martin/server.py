import asyncio
import sys
from mcp.server.fastmcp import FastMCP

# Initialize FastMCP server
mcp = FastMCP("sand-martin")

@mcp.tool()
async def get_canvas_state() -> str:
    """Returns the current state of the Grasshopper canvas."""
    # This will eventually call the C# Host Server
    return "Canvas state placeholder"

def main():
    """Entry point for the sand-martin MCP server."""
    mcp.run()

if __name__ == "__main__":
    main()
