---
title: "MCP_Client — the Node MCP server agents run (cem-navis-aimodeler)"
type: module
updated: 2026-10-07
sources: [MCP_Client/server/index.js, MCP_Client/server/package.json, MCP_Client/manifest.json, MCP_Client/.dxtignore, MCP_Client/server/tests/]
related: [../concepts/rpc-contract.md, rpc-server.md, ../workflows/connect-an-agent.md, ../workflows/test.md]
tags: [module, client, mcp, node]
---

# MCP_Client

A Node (>= 18) MCP server over stdio, using `@modelcontextprotocol/sdk`. An agent host (Claude Desktop,
Claude Code, …) starts it; it forwards each tool call to the AI Modeler's RPC server. It reports itself
as **`cem-navis-aimodeler`** (upstream: `waabe-navisworks-mcp`, renamed by
[0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)).

- `server/index.js`, class `NavisworksMCPServer`:
  - `getNavisworksApiUrl()`: `http://localhost:${NAVISWORKS_API_PORT || 8765}`; 8765 is the server's
    default too, so no setting is needed out of the box.
  - `rpc(method, params)`: `POST /rpc`, throws on HTTP errors, missing `ok`, or `ok: false`
    (`RPC-Fehler <code>: <msg>`).
  - `ListTools`: the ten tools with their `inputSchema`s. `CallTool`: a `switch` per tool, using
    `rpc(...)` or the `t_simple(method)` helper, and formatting `data` as text content.
  - `normalizeTokens`: comma/semicolon/newline lists from strings or arrays.
- `server/package.json` (`cem-navis-aimodeler`): `npm start`, `npm run inspector` (MCP inspector), and
  `npm test` ([test](../workflows/test.md)).
- `manifest.json`: the `.dxt` (Desktop Extension) manifest, name `cem-navis-aimodeler`: runs
  `node ${__dirname}/server/index.js` with `NAVISWORKS_API_PORT` from the user setting `api_port`
  (default `"8765"`), and lists the tools.
- `.dxtignore`: leaves `server/tests/` and `*.test.mjs` out of the `.dxt`, so test code never ships.

Packaging uses the `dxt` CLI, as `MCP_Client/README.md` describes: `dxt validate manifest.json`, then
`dxt pack . cem-navis-aimodeler.dxt` (optionally `dxt sign … --self-signed`). The committed
`generated_MCP_Client/*.dxt` is upstream's stale 2026 build ([known gaps](../concepts/known-gaps.md)).
