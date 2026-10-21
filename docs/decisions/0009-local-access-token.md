---
title: "0009 — A local access token on every request"
type: decision
status: current
updated: 2026-10-21
sources:
  - MCP_Server/CEM_NavisIAModeler/Infrastructure/AccessToken.cs
  - MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs
  - MCP_Client/server/navis-rpc.js
  - MCP_Client/server/tests/access-token.test.mjs
  - MCP_Client/server/tests/job-protocol.test.mjs
related:
  - 0005-no-cors-refuse-browser-origins.md
  - 0007-per-user-home-no-writes-next-to-the-dll.md
  - 0010-send-code-and-screenshot.md
  - ../concepts/known-gaps.md
  - ../../../CEM_RevitMCP/docs/decisions/0022-local-shared-secret-handshake.md
tags: [security, http, token]
---

# 0009 — A local access token on every request

## Context

[0005](0005-no-cors-refuse-browser-origins.md) kept other machines and the user's browser out, but any
program on the machine, another user's session included, could still call `/rpc`. That was acceptable
for queries; it is not for [`send_code_to_navisworks`](0010-send-code-and-screenshot.md), which runs
arbitrary code as the Navisworks user. CEM_RevitMCP recorded the fix as a proposal for its socket
([its 0022](../../../CEM_RevitMCP/docs/decisions/0022-local-shared-secret-handshake.md)); the user asked
for the code tool on 2026-10-21, so the token comes first, here.

## Decision

- At each start, once the port is bound, the server creates a random token (32 bytes, hex) and writes it
  to `CEM_NavisIAModeler.token` in the user's Navisworks settings folder,
  `CemUserHome.NavisworksSettingsFile` (`%APPDATA%\Autodesk\ApplicationPlugins\CEM_NavisworksAPI.bundle\Settings`,
  the one home of [0007](0007-per-user-home-no-writes-next-to-the-dll.md)). Other Windows users cannot
  read it. If it cannot be written, the server does not start. It deletes the file when it stops, if the
  file still holds its token.
- Every request except `GET /` and `GET /health` must carry `Authorization: Bearer <token>`; otherwise
  **401** `NVX_UNAUTHORIZED`, before anything is routed. The comparison takes constant time.
- The Node client reads the file **for every request** (`navis-rpc.js`), so a restarted AI Modeler, with
  a new token, needs no client restart. `NAVISWORKS_TOKEN_FILE` overrides the path. A 401 tells the agent
  where the client looked.
- `access-token.test.mjs` checks that both halves name the same file, that the check comes before routing
  and leaves the health check open, and that the comparison is constant-time; `job-protocol.test.mjs`
  checks the client against a fake server that demands the token.

## Rejected

| Alternative | Why not |
|---|---|
| Windows authentication on the listener (`HttpListener` Negotiate/NTLM) | Node's `fetch` cannot do it; the client would need a native module |
| A named pipe with an ACL instead of HTTP | Replaces upstream's transport ([0004](0004-minimal-divergence-from-upstream.md)) for the same protection |
| A fixed secret in the settings file | Never rotates, and a copy of the settings would carry it |
| `%LOCALAPPDATA%\Cemengal\…`, as CEM_RevitMCP's 0022 proposed | 0007 keeps one per-user home for the Navisworks add-ins; the token changes at every start, so a roaming copy is harmless |

## Consequences

- Programs that run as the same Windows user can still read the file. They could already run code as
  that user, so the token adds nothing there; it closes the door to other users and to anything that
  cannot read the user's profile.
- The agent host and the client must run as the Windows user who runs Navisworks.
- A client from before 0009 gets 401 from a server after it, and the old curl smoke steps need the
  header ([smoke test](../workflows/smoke-test.md)).
- Known gap 2 (no authentication) is closed. CEM_RevitMCP can take the same design for its 0022.
