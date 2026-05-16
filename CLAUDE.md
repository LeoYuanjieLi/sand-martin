# Sand Martin Project Instructions (Claude Code)

## Authentication
The Sand Martin Host in Rhino requires a Bearer token.

- **Token Discovery**: Check `os.path.join(tempfile.gettempdir(), 'sand_martin.token')`.
- **Handling 401**: If you get a 401 Unauthorized, **immediately ask the user** for the "SAND MARTIN SECURITY TOKEN" from the Rhino Command History.
- **Persistence**: Write the token to the system temp directory as `sand_martin.token`.

## Build & Test Commands
- **Install Dependencies**: `pip install -e ".[test]"`
- **Run Tests**: `./run_tests.sh`
- **Build**: `python3 -m build`
- **MCP Server**: `python3 src/sand_martin/server.py`
