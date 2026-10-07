---
title: "0003 — node:test contract tests pin the method names across client, server and manifest"
type: decision
status: current
updated: 2026-10-07
sources: [MCP_Client/server/tests/rpc-contract.test.mjs, MCP_Client/server/package.json, MCP_Client/.dxtignore, ../../docs/decisions/0015-clean-cutover-guarded-contracts.md]
related: [../concepts/rpc-contract.md, ../workflows/test.md, 0005-no-cors-refuse-browser-origins.md]
tags: [testing, contract, node]
---

# 0003 — Contract tests across the client, the server and the manifest

## Context

Every owned repo needs a real test suite, and new logic is test-first
([root 0007](../../../../docs/decisions/0007-tdd-and-one-test-runner.md)). Upstream had no tests. The
C# server cannot be unit-tested without Navisworks: its controllers reach the API through the router's
constructor. The most likely failure is a name that drifts between the Node client and the C# routes,
which only shows at run time inside Navisworks. The root pins cross-boundary contracts with tests
([root 0015](../../../../docs/decisions/0015-clean-cutover-guarded-contracts.md)).

## Decision

- `MCP_Client/server/tests/*.test.mjs`, run by `npm test` (`node --test "tests/*.test.mjs"`), using only
  Node's built-in `node:test`: no new dependencies.
- `rpc-contract.test.mjs` parses `index.js`, `RpcMap.cs` and `mcp_manifest.json` and checks that every
  client call and tool is routed, and that the manifest lists exactly the routes. Its parsers have
  their own tests (commented-out routes, quote styles, the `t_simple` helper).
- Written first: the manifest check failed on arrival (`ping` advertised but not routed; four routes
  missing), and the manifest was then fixed.
- `server-security.test.mjs` guards [0005](0005-no-cors-refuse-browser-origins.md) the same way.
- `.dxtignore` excludes `server/tests/` and `*.test.mjs`, so tests never ship in the `.dxt`.

**Rejected:**
- An xunit project over the C# server: the router cannot be built without Navisworks types at run time.
- Generating the client's tool list from the routes: that is a bigger change to upstream code
  ([0004](0004-minimal-divergence-from-upstream.md)).

## Consequences

- Parameter shapes are not checked ([RPC contract](../concepts/rpc-contract.md)).
- Behaviour inside Navisworks is verified only by the [smoke test](../workflows/smoke-test.md).
