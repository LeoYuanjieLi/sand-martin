#!/bin/bash

# Exit immediately if a command exits with a non-zero status
set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting Sand Martin Test Suite...${NC}\n"

# 1. Run C# Host Unit Tests
echo -e "${GREEN}Running C# Host Unit Tests...${NC}"
dotnet test tests/SandMartin.Host.Tests/SandMartin.Host.Tests.csproj

# 2. Run Python MCP Server Unit Tests
echo -e "\n${GREEN}Running Python MCP Server Unit Tests...${NC}"

# Check for virtual environment and activate if it exists
if [ -d ".venv" ]; then
    echo -e "${GREEN}Activating virtual environment (.venv)...${NC}"
    source .venv/bin/activate
elif [ -d "venv" ]; then
    echo -e "${GREEN}Activating virtual environment (venv)...${NC}"
    source venv/bin/activate
fi

# Diagnostic check
if ! python3 -c "import httpx, pytest, respx, pytest_asyncio" &> /dev/null
then
    echo -e "${RED}Error: One or more Python dependencies are missing.${NC}"
    echo -e "Please install dependencies from requirements.txt:"
    echo -e "  pip install -r requirements.txt"
    exit 1
fi

# Use the python3 -m pytest approach for better path resolution
python3 -m pytest tests/sand_martin/test_server.py

echo -e "\n${GREEN}All tests passed successfully!${NC}"
