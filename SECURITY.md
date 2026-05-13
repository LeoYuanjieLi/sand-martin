# Security Model: Sand Martin

Sand Martin takes the security of your Rhino and Grasshopper environment seriously. Because this plugin allows for remote orchestration and code injection, we have implemented several layers of protection to prevent unauthorized access, particularly from web-based attacks like Cross-Site Request Forgery (CSRF).

## Security Layers

### 1. Token-Based Authentication
Every time you enable the **Sand Martin Server** component in Grasshopper, it generates a unique, random **32-character authentication token**.
- This token is printed to the **Rhino Command Line**.
- All incoming requests to the server must include this token in the `Authorization: Bearer <token>` header.
- **Why?** This prevents "Simple Requests" from web browsers from executing commands. Browsers require a preflight (OPTIONS) check for requests with custom headers, which the Sand Martin server will reject if unauthorized.

### 2. Localhost-Only Binding
The Sand Martin server explicitly binds to `127.0.0.1` (localhost).
- It is **not** accessible from other machines on your network.
- **Why?** This ensures that only processes running on your own computer can communicate with the plugin.

### 3. Code Injection Gating
The **Sand Martin Server** component includes an `AllowCodeInjection` input parameter.
- **Default: True** (to enable the core AI orchestration experience).
- When set to **False**, the server will reject any request that attempts to create or update a component's code (e.g., C# or Python script blocks).
- **Why?** This allows you to use Sand Martin for canvas orchestration (moving/wiring nodes) while completely disabling the ability for the AI to run arbitrary scripts on your machine.

### 4. Sanity Checks & Sanitization
- Error messages are sanitized to prevent leaking internal stack traces.
- Content-Type is strictly enforced for JSON payloads.

## Best Practices for Users
- **Never share your token**: Treat the token printed in the Rhino console as a temporary password.
- **Disable when not in use**: Set the `Run` toggle to `False` on the Sand Martin component when you are not actively using the AI orchestration features.
- **Use Environment Variables**: The Sand Martin Python bridge expects the token to be set in the `SAND_MARTIN_TOKEN` environment variable. Never hardcode this token in your configuration files.

## Reporting Vulnerabilities
If you discover a security issue, please contact the maintainers privately rather than opening a public issue.
