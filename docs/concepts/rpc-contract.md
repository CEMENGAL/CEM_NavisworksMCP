---
title: "The RPC contract — method names shared by client, server and manifest"
type: concept
updated: 2026-10-07
sources: [MCP_Client/server/index.js, MCP_Server/CEM_NavisIAModeler/Mapping/RpcMap.cs, MCP_Server/CEM_NavisIAModeler/mcp_manifest.json, MCP_Client/server/tests/rpc-contract.test.mjs]
related: [../decisions/0003-rpc-contract-tests.md, ../workflows/add-a-tool.md, ../modules/rpc-server.md, ../modules/node-client.md]
tags: [contract, rpc, tests]
---

# The RPC contract

The two halves run in different processes and meet only at `POST /rpc` with
`{ "id": "...", "method": "<name>", "params": { ... } }`, answered by
`{ "ok": true, "data": ..., "meta": {...} }` or `{ "ok": false, "error": { "code", "msg" } }`.
A method name is spelled in four places, and they must agree byte for byte:

| Where | Form |
|---|---|
| `MCP_Client/server/index.js`, tool list | `{ name: 'get_model_overview', ... }`: the MCP tool name equals the RPC method name |
| `MCP_Client/server/index.js`, handler | `this.rpc('get_model_overview', …)` or `this.t_simple('get_model_overview')` |
| `MCP_Server/CEM_NavisIAModeler/Mapping/RpcMap.cs` | `routes["get_model_overview"] = wrap(model.GetModelOverview);` (commented-out lines do not count) |
| `MCP_Server/CEM_NavisIAModeler/mcp_manifest.json` | the method listed under its group in `"rpc"` |

Also listed, but not checked: the `.dxt` manifest's `tools` array (`MCP_Client/manifest.json`),
which Claude Desktop shows before the server starts.

A mismatch fails only at run time, inside Navisworks, as `NVX_NOT_FOUND "Unknown method"`. So
`npm test` (`MCP_Client/server/tests/rpc-contract.test.mjs`) reads the files and checks:

- every client `rpc`/`t_simple` call is a server route;
- every client tool name is a server route;
- the manifest lists exactly the server routes;
- `index.js` still parses (`node --check`), since the other checks read it as text.

At the fork, the manifest check failed: it listed `ping` (commented out in `RpcMap.cs`) and missed
`get_property_distribution_by_category`, `list_items_to_property`, `apply_selection` and
`run_simple_clash`. The manifest was fixed. At run time `ManifestBuilder` regenerates the deployed
manifest from the routes on the first `/manifest` request, so the committed file is the seed the
bundle ships with.

Parameter shapes are not checked: they live in the client's `inputSchema`s and the server's
`Contracts/Queries.cs`. Keep them in step by hand when adding a parameter.
