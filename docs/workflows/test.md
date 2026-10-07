---
title: "Test — the contract and security tests"
type: workflow
updated: 2026-10-07
sources: [MCP_Client/server/tests/, MCP_Client/server/package.json, ../.claude/run-tests.ps1]
related: [../decisions/0003-rpc-contract-tests.md, ../concepts/rpc-contract.md, smoke-test.md]
tags: [testing]
---

# Test

```powershell
cd MCP_Client\server; npm test
.\.claude\run-tests.ps1 -Repos navismcp      # from the Cemengal root
```

No `npm install` is needed: the tests use only `node:test` and read files. Green means `fail 0`.

| File | Checks |
|---|---|
| `tests/rpc-contract.test.mjs` | client calls and tools are routed; manifest = routes; the parsers ([RPC contract](../concepts/rpc-contract.md)) |
| `tests/server-security.test.mjs` | no CORS, browser `Origin`s refused, loopback-only prefixes ([0005](../decisions/0005-no-cors-refuse-browser-origins.md)) |
| `tests/default-port.test.mjs` | every default-port literal on both halves is 8765, not CEM_RevitMCP's 8080 ([0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)) |

After the rename: 14 tests, all green (`rpc-contract` also checks that `index.js` parses: a comment
that swallowed a comma got past the text-only checks once, and only a live handshake caught it). They say nothing about behaviour inside Navisworks: use the
[smoke test](smoke-test.md) for that. Also build the solution ([build](build-and-deploy.md)): the
C# side has no unit tests ([0003](../decisions/0003-rpc-contract-tests.md)).

New logic is test-first: when adding a tool, add it to the client and see the contract test fail
until the route and the manifest exist ([add a tool](add-a-tool.md)).
