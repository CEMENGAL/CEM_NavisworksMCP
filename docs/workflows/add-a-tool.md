---
title: "Add a tool — the four places it lives"
type: workflow
updated: 2026-10-07
sources: [MCP_Client/server/index.js, MCP_Server/CEM_NavisIAModeler/Mapping/RpcMap.cs, MCP_Server/CEM_NavisIAModeler/mcp_manifest.json, MCP_Client/manifest.json]
related: [../concepts/rpc-contract.md, ../modules/rpc-server.md, ../modules/node-client.md, test.md]
tags: [workflow, tool]
---

# Add a tool

A tool named `get_x` (snake_case; it is both the MCP tool name and the RPC method name):

1. **Client first** (test-first): add `{ name: 'get_x', description, inputSchema }` to the tool list
   in `MCP_Client/server/index.js` and a `case 'get_x'` that calls `this.rpc('get_x', params)` (or
   `this.t_simple('get_x')` without parameters). Run `npm test`: the contract test fails, because no
   route exists.
2. **Server:** a controller method (in the matching `Controllers/*Controller.cs`, with its parameter type
   in `Contracts/Queries.cs` and checks from `Validation/Validators.cs`), a backend call in
   `Services/Backends/` that runs on the UI thread, and the route
   `routes["get_x"] = wrap(controller.GetX);` in `Mapping/RpcMap.cs`.
3. **Manifest:** add `get_x` to its group in `mcp_manifest.json`. `npm test` is green again.
4. **The `.dxt` tool list** in `MCP_Client/manifest.json` (shown before the server starts; not checked).
5. Build ([build](build-and-deploy.md)), then call it in Navisworks ([smoke test](smoke-test.md)).
6. Mark upstream-file changes with a `Cemengal` comment ([0004](../decisions/0004-minimal-divergence-from-upstream.md)),
   then [finish a change](finish-a-change.md).

A tool that changes the document should be idempotent where it can be, and its result should say
what it changed.
