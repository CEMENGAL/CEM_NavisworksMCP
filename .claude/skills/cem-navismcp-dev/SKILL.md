---
name: cem-navismcp-dev
description: Develop CEM_NavisworksMCP, the Navisworks Manage 2027 MCP fork — the in-Navisworks HTTP/RPC server (controllers, RpcMap, backends), the Node MCP client tools, the RPC contract tests, the SDK-style 2027 build and the ApplicationPlugins bundle. Triggers "Navisworks MCP", "CEM_NavisworksMCP", "Navisworks AI", "MCP tool for Navisworks".
---

# CEM_NavisworksMCP development

A fork of `kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026`, ported to Navisworks Manage 2027 and renamed
**CEM_NavisIAModeler**. Inside Navisworks, `MCP_Server/CEM_NavisIAModeler` runs an HTTP server on `127.0.0.1:8765`
that answers JSON-RPC at `/rpc`; CEM_NavisworksAPI's Cemengal ribbon starts it (**AI → AI Modeler**) through
`Core/MCPServiceConnection`. Outside, `MCP_Client/server/index.js` is a Node MCP
server over stdio (packaged as a `.dxt` for Claude Desktop) that turns each MCP tool into one RPC call.

**Before planning or editing, read [docs/index.md](../../../docs/index.md) ("Start here"), then the pages it names.**
Search with qmd when the index is not enough (`qmd query -c cem_navismcp "..."`).

## Hard rules

- It is a fork: change upstream code only for the 2027 port or a recorded Cemengal need, and mark each change with a
  `Cemengal` comment ([0004](../../../docs/decisions/0004-minimal-divergence-from-upstream.md)).
- A tool's name is the same in four places: the client's tool list, its `rpc`/`t_simple` call, `RpcMap.cs`, and
  `mcp_manifest.json`. `npm test` fails if they disagree ([0003](../../../docs/decisions/0003-rpc-contract-tests.md)).
- The build is SDK-style `net48`, `Debug N27`, with the Navisworks API from NuGet compile-only; never reintroduce
  `HintPath`s to an install folder or ship Autodesk DLLs ([0001](../../../docs/decisions/0001-sdk-style-2027-build.md)).
- In a Cemengal checkout CEM_NavisworksAPI's `CEM_RibbonUI` hosts and deploys the server; this repo's standalone bundle
  (`CEM_NavisIAModeler.bundle`, from `CEM_NavisIAModeler_Ribbon`) deploys only without that sibling. Both halves default to
  port 8765 ([0006](../../../docs/decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)). Keep `MCPServiceConnection`'s
  API stable: CEM_RibbonUI calls it.
- The server listens on loopback only, sends no CORS headers and refuses requests with an `Origin` header; it has no
  authentication yet, so never widen the binding or add CORS back
  ([0005](../../../docs/decisions/0005-no-cors-refuse-browser-origins.md)); `npm test` guards both.
- Test code never ships: `MCP_Client/.dxtignore` excludes `server/tests/`.

## Verify

```powershell
cd MCP_Client\server; npm test                                           # the RPC contract
dotnet build MCP_Server\CEM_NavisworksMCP.sln -c "Debug N27" -v minimal   # 0 errors
.\.claude\run-tests.ps1 -Repos navismcp                                  # from the Cemengal root
```

Anything inside Navisworks is checked by hand ([smoke test](../../../docs/workflows/smoke-test.md)).

## Finish: commit-time ingest

Every commit carries its own wiki update (CROSS_REPO.md §4/§9): the source page in `docs/sources/` with each decision,
its reason and the rejected alternatives; the affected pages; `docs/index.md` for new pages; one `docs/log.md` entry.
Run `python ../../tools/wiki/wiki_lint.py .` to 0 errors and commit one line: `TYPE - Description. <issue URL>`
([finish a change](../../../docs/workflows/finish-a-change.md)).
