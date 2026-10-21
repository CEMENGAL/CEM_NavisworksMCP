---
title: "MCP_Client — the Node MCP server agents run (cem-navis-aimodeler)"
type: module
updated: 2026-10-21
sources: [MCP_Client/server/index.js, MCP_Client/server/navis-rpc.js, MCP_Client/server/package.json, MCP_Client/manifest.json, MCP_Client/.dxtignore, MCP_Client/server/tests/]
related: [../concepts/rpc-contract.md, ../decisions/0008-one-job-at-a-time.md, ../decisions/0009-local-access-token.md, ../decisions/0010-send-code-and-screenshot.md, rpc-server.md, ../workflows/connect-an-agent.md, ../workflows/test.md]
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
  - `rpc(method, params)`: calls `callRpc` in `navis-rpc.js` (below) and returns `data`.
  - `ListTools`: fifteen tools with their `inputSchema`s: the ten upstream ones, the job tools
    `get_server_status`, `get_job_status`, `cancel_job` ([0008](../decisions/0008-one-job-at-a-time.md)),
    and `send_code_to_navisworks` and `take_screenshot`, whose image comes back as MCP image content
    ([0010](../decisions/0010-send-code-and-screenshot.md)).
    `CallTool`: a `switch` per tool, using `rpc(...)` or the `t_simple(method)` helper, and formatting
    `data` as text content. A request still running when its budget ends is answered as text, not as
    an error: the job id, and "do not send the request again".
- `server/navis-rpc.js` (Cemengal, no SDK import, so `node:test` loads it): the client's half of the
  job protocol. `postRpc` times each HTTP request out after 30 s and turns a refused connection into
  "press Cemengal > AI > AI Modeler" (`NOT_RUNNING`) and a timeout into `NO_ANSWER`. `callRpc` follows an
  `NVX_JOB_RUNNING` reply with `get_job_status` until the 45 s tool budget runs out, then throws
  `JobPending`; other `ok: false` replies throw `NavisRpcError` (`RPC error <code>: <msg>`, with the job
  for `NVX_BUSY`). `awaitJob` serves the `get_job_status` tool. Every request carries
  `Authorization: Bearer <token>`, read again each time from the AI Modeler's token file
  (`defaultTokenFile`, `NAVISWORKS_TOKEN_FILE` overrides it); a 401 becomes `UNAUTHORIZED`, naming the file
  ([0009](../decisions/0009-local-access-token.md)).
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
