---
title: "Test — the contract and security tests"
type: workflow
updated: 2026-10-24
sources: [MCP_Client/server/tests/, MCP_Client/server/package.json, ../.claude/run-tests.ps1]
related: [../decisions/0003-rpc-contract-tests.md, ../decisions/0008-one-job-at-a-time.md, ../decisions/0009-local-access-token.md, ../decisions/0010-send-code-and-screenshot.md, ../decisions/0011-logs-in-local-temp-nothing-under-acc.md, ../concepts/rpc-contract.md, smoke-test.md]
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
| `tests/no-dll-adjacent-writes.test.mjs` | the manifest is not written to disk, settings resolve through `CemUserHome` and the log through `CemTempFolder` ([0007](../decisions/0007-per-user-home-no-writes-next-to-the-dll.md)) |
| `tests/file-writes.test.mjs` | every file write accounted for ([0011](../decisions/0011-logs-in-local-temp-nothing-under-acc.md)): no shipped source names an `ACCDocs` path; the only C# writers are `SettingsManager` and `AccessToken`, through `CemUserHome`, never from the DLL, temp or a special folder; no hand-made temp path; screenshot and script in memory only; the log through `CemTempFolder`; settings never fall back to a file; the client imports no `fs` write; the standalone bundle ships `CEM_CSharpExtended.dll` |
| `tests/access-token.test.mjs` | both halves name the same token file in the user's settings folder; the token is random, issued at start and revoked at stop; every route but the health check is checked before routing; constant-time comparison ([0009](../decisions/0009-local-access-token.md)) |
| `tests/code-and-screenshot.test.mjs` | `DependencyResolver` installed before Roslyn loads, compile before the UI-thread run, scripts never logged or written, the screenshot a bounded PNG in memory ([0010](../decisions/0010-send-code-and-screenshot.md)) |
| `tests/job-protocol.test.mjs` | the client's job protocol against a fake AI Modeler that demands the token (an immediate reply, a job followed to its end, a job past the budget, cancelled, busy, nothing listening, no answer, a stale token), and that the time budgets nest from `RpcJobs.cs` to the host's 60 s ([0008](../decisions/0008-one-job-at-a-time.md)) |
| `tests/job-server.test.mjs` | read from the C# sources: only the job tools skip the one-job rule, every request goes through `RpcJobs`, no controller uses a fixed timeout, a UI-thread failure is not re-run, stopping cancels the job ([0008](../decisions/0008-one-job-at-a-time.md)) |

51 tests since [0011](../decisions/0011-logs-in-local-temp-nothing-under-acc.md) (2026-10-24), all green (`rpc-contract` also checks that `index.js` parses: a comment
that swallowed a comma got past the text-only checks once, and only a live handshake caught it). They say nothing about behaviour inside Navisworks: use the
[smoke test](smoke-test.md) for that. Also build the solution ([build](build-and-deploy.md)): the
C# side has no unit tests ([0003](../decisions/0003-rpc-contract-tests.md)). C# that needs no Navisworks can
still be run once by a throwaway harness outside the repo: on 2026-10-24 a `net48` console compiled the real
`SettingsManager.cs` against a stand-in `CemUserHome` that throws, with `TMP` pointed at an empty folder; the
HEAD version wrote `CEM_NavisIAModeler_settings.xml` there (red), the new one kept the port in memory and wrote
only its log line (green) ([0011](../decisions/0011-logs-in-local-temp-nothing-under-acc.md)).

New logic is test-first: when adding a tool, add it to the client and see the contract test fail
until the route and the manifest exist ([add a tool](add-a-tool.md)).
