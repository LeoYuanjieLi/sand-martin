# Sand Martin Project Instructions

## Authentication & Security
The Sand Martin Host in Rhino requires a Bearer token for all requests.

### Token Acquisition Flow
- **Proactive Check**: Before performing any canvas operations, check if the `sand_martin.token` file exists in the system temp directory.
- **Handling 401 Unauthorized**: If a tool returns a `401 Unauthorized`, do not attempt multiple retries or complex discovery.
- **Fastest Path**: If the token is missing or invalid, **immediately ask the user** for the token using the `ask_user` tool.
- **Persistence**: Once the user provides the token, write it to the system's temporary directory in a file named `sand_martin.token` to ensure the MCP server can use it for subsequent requests.

### Tool Verification
You can verify the connection manually using `curl` if needed:
```bash
curl -H "Authorization: Bearer <TOKEN>" http://127.0.0.1:8081/state
```
