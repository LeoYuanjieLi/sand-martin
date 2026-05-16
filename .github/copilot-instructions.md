# GitHub Copilot Instructions for Sand Martin

## Security Token Handling
This project uses an MCP server to communicate with Rhino. Authentication is handled via a Bearer token.

1. **Check for Token**: Look for a file at `sand_martin.token` in the system's temporary directory.
2. **Request Token**: If the token is missing or if requests to `http://127.0.0.1:8081` return a `401 Unauthorized`, ask the user to provide the "SAND MARTIN SECURITY TOKEN" from the Rhino Command History.
3. **Save Token**: Encourage the user to save the token to the system temp directory as `sand_martin.token` so that automated tools can find it.
