---
title: "0005 — No CORS, and refuse requests from browser origins"
type: decision
status: current
updated: 2026-10-07
sources: [MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs, MCP_Client/server/tests/server-security.test.mjs]
related: [../concepts/known-gaps.md, ../modules/rpc-server.md, ../../../CEM_RevitMCP/docs/decisions/0021-loopback-only-socket.md]
tags: [security, http]
---

# 0005 — No CORS, and refuse requests from browser origins

## Context

The upstream server listens on loopback only (`127.0.0.1`, `localhost`) with no authentication, and
answered every request with `Access-Control-Allow-Origin: *` (plus `content-type` headers and the
`GET, POST, OPTIONS` methods). Loopback keeps other machines out, but not the user's own browser: with
the server running, any web page could `POST /rpc`, read model data (`list_properties_for_item`, …) and
change the document (`apply_selection`, `run_simple_clash`). Removing the CORS headers alone is not
enough: a browser still sends a "simple" cross-origin POST (for example `text/plain`) without a
preflight; it only hides the reply, and the side effects still happen. CEM_RevitMCP made its socket
loopback-only for the same class of risk ([its 0021](../../../CEM_RevitMCP/docs/decisions/0021-loopback-only-socket.md)).

## Decision

- `MCPServer.HandleRequestAsync` sends no CORS headers.
- It answers **403** to any request that carries an `Origin` header, before routing. Browsers attach
  `Origin` to cross-origin requests; the Node MCP client (`fetch` in Node) does not.
- `server-security.test.mjs` fails if an `Access-Control-Allow-Origin` line comes back, if the Origin
  check disappears, or if a listener prefix stops being loopback (`+`, `*`, `0.0.0.0`).

**Rejected:**
- An allow-list of origins: no legitimate browser client exists.
- Authentication only: it is the real fix for local processes and is still open
  ([known gaps](../concepts/known-gaps.md)), but it is a larger change across both halves. The Origin
  check closes the browser path now, with three lines.

## Consequences

- A browser-based client (an MCP inspector page, say) cannot call the server. The Node inspector
  (`npm run inspector`) is unaffected.
- Local processes can still call `/rpc` until a shared secret is added.
