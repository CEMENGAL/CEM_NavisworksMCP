---
title: "CEM_NavisworksMCP wiki — index"
type: index
updated: 2026-10-21
sources: [README.md, MCP_Server/README.md, MCP_Client/README.md]
related: [overview.md, log.md]
tags: [index]
---

# CEM_NavisworksMCP wiki

## Start here

**1. Read [overview.md](overview.md).** CEM_NavisworksMCP is a **fork of
`kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026`**, ported to Navisworks Manage 2027 and renamed **CEM_NavisIAModeler** (the AI Modeler). Its server
(`MCP_Server/CEM_NavisIAModeler`) runs an HTTP/RPC server on `127.0.0.1:8765` inside Navisworks, started
from the Cemengal tab's **AI → AI Modeler** button (CEM_NavisworksAPI hosts it); a Node MCP client
(`MCP_Client/server/index.js`, `cem-navis-aimodeler`) lets an AI agent call it. Upstream vs our parts:
[upstream and fork](concepts/upstream-and-fork.md).

**2. Hard rules.** Each links to its reason.

1. Change upstream code only for the 2027 port or a recorded Cemengal need; mark it with a `Cemengal`
   comment → [0004](decisions/0004-minimal-divergence-from-upstream.md)
2. A tool's name agrees in the client tool list, its RPC call, `RpcMap.cs` and `mcp_manifest.json`;
   `npm test` checks it → [0003](decisions/0003-rpc-contract-tests.md), [RPC contract](concepts/rpc-contract.md)
3. SDK-style `net48`, `Debug N27`, the API from NuGet compile-only; no install-folder `HintPath`s
   → [0001](decisions/0001-sdk-style-2027-build.md)
4. The bundle is deployed by one MSBuild target, `Contents\v24` / `Nw24`
   → [0002](decisions/0002-msbuild-bundle-deploy.md)
5. CEM_NavisworksAPI's `CEM_RibbonUI` hosts and deploys the server; this repo deploys its own bundle only
   without that sibling. Both halves default to port 8765 → [0006](decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)
6. Loopback only, no CORS, browser `Origin`s refused, and every request but the health check carries the
   per-start access token; never widen the binding or open a route without the token
   → [0005](decisions/0005-no-cors-refuse-browser-origins.md), [0009](decisions/0009-local-access-token.md)
7. Any CEM_API call goes through CEM_CSharpExtended's `Api`: never spell a CEM_API route or build its URL
   here. Check its existing methods first; a missing call is added to the library first, then used. Today
   this repo makes no CEM_API request; its `localhost` URLs are its own MCP server
   → root [0023](../../../docs/decisions/0023-one-shared-request-layer.md) (C# side),
   [0019](../../../docs/decisions/0019-cem-csharpextended-shared-csharp-library.md)
8. One request at a time, as a job: never queue work on the UI thread behind other work, never give it
   a fixed timeout (the job's token), and make every scan over model items pause with `UiSlice`; the
   client ends each tool call inside the host's 60 s timeout → [0008](decisions/0008-one-job-at-a-time.md)

**3. Before you change X, read Y.**

| Task / area | Read |
|---|---|
| Add or change a tool | [add a tool](workflows/add-a-tool.md), [RPC contract](concepts/rpc-contract.md), [server](modules/rpc-server.md), [client](modules/node-client.md) |
| Running code in Navisworks, screenshots, the access token | [0010](decisions/0010-send-code-and-screenshot.md), [0009](decisions/0009-local-access-token.md), [server](modules/rpc-server.md) (`CodeRunner`, `ScreenCapture`, `AccessToken`) |
| Anything slow, a scan over model items, timeouts, "Navisworks is not answering" | [0008](decisions/0008-one-job-at-a-time.md) (one job at a time, scans pause for the UI, the client's time budgets), [server](modules/rpc-server.md) (`RpcJobs`, `UiSlice`) |
| Starting / stopping, the host button, the port | [0006](decisions/0006-renamed-and-hosted-by-cem-ribbonui.md), [server](modules/rpc-server.md) (`Core/MCPServiceConnection`), [standalone ribbon](modules/addin-ribbon.md) |
| csproj, build, bundle, a new Navisworks year | [build and deploy](workflows/build-and-deploy.md), [0001](decisions/0001-sdk-style-2027-build.md), [0002](decisions/0002-msbuild-bundle-deploy.md) |
| Pulling from upstream | [upstream and fork](concepts/upstream-and-fork.md), [0004](decisions/0004-minimal-divergence-from-upstream.md) |
| Connecting Claude | [connect an agent](workflows/connect-an-agent.md) |
| Call CEM_API from this repo | root [0023](../../../docs/decisions/0023-one-shared-request-layer.md) (C# side), [0019](../../../docs/decisions/0019-cem-csharpextended-shared-csharp-library.md), [0021](../../../docs/decisions/0021-generic-data-endpoints-app-shapes.md), [0025](../../../docs/decisions/0025-desktop-client-deprecation-window.md), CEM_CSharpExtended's [core `Api`](../../../CEM_CSharpExtended/docs/modules/cem-csharpextended.md) |
| Anything | [known gaps](concepts/known-gaps.md): item ids still scanned from the whole document, scripts that cannot pause, four tools not run live, stale prebuilt artifacts |

**4. Build, test, verify:** [build and deploy](workflows/build-and-deploy.md) with `-c "Debug N27"`,
[test](workflows/test.md) with `npm test` in `MCP_Client/server`, then the [smoke test](workflows/smoke-test.md)
in Navisworks for anything that runs there.

**5. Finish:** [finish a change](workflows/finish-a-change.md). Every commit carries its `docs/`
update. Lint with `python ../../tools/wiki/wiki_lint.py .`. The message is one line,
`TYPE - Description. <issue URL>`, committed on `main` after the user sends the issue link.

## Catalog

### Hub

- [overview.md](overview.md): what the fork is, its parts, the request path, place in the family.
- [log.md](log.md): append-only history of ingests, lints and queries.

### Modules

- [rpc-server](modules/rpc-server.md): `CEM_NavisIAModeler`, the host entry point, HTTP listener, router, controllers and backends.
- [shared](modules/shared.md): `CEM_NavisIAModeler_Shared`, the service registry and logging.
- [addin-ribbon](modules/addin-ribbon.md): `CEM_NavisIAModeler_Ribbon`, upstream's ribbon tab, for standalone use only.
- [node-client](modules/node-client.md): `MCP_Client/server/index.js` (`cem-navis-aimodeler`), the MCP tools, the `.dxt` package.

### Concepts

- [upstream-and-fork](concepts/upstream-and-fork.md): what upstream is, what we changed, how to sync.
- [rpc-contract](concepts/rpc-contract.md): the method names shared by the client, the server and the manifest.
- [known-gaps](concepts/known-gaps.md): what is missing, unverified or risky.

### Workflows

- [build-and-deploy](workflows/build-and-deploy.md): `Debug N27` builds; hosted vs standalone deployment.
- [test](workflows/test.md): the contract tests and what they cannot cover.
- [smoke-test](workflows/smoke-test.md): checking the AI Modeler and the tools in Navisworks, with the recorded results.
- [connect-an-agent](workflows/connect-an-agent.md): start the AI Modeler and register the client.
- [add-a-tool](workflows/add-a-tool.md): the four places a tool lives.
- [finish-a-change](workflows/finish-a-change.md): commit with the wiki ingest.

### Decisions

- [0001](decisions/0001-sdk-style-2027-build.md): SDK-style `net48` build for Navisworks 2027 from NuGet (current).
- [0002](decisions/0002-msbuild-bundle-deploy.md): one MSBuild target deploys the bundle (current).
- [0003](decisions/0003-rpc-contract-tests.md): `node:test` contract tests across the client, server and manifest (current).
- [0004](decisions/0004-minimal-divergence-from-upstream.md): minimal, marked divergence from upstream (current).
- [0005](decisions/0005-no-cors-refuse-browser-origins.md): no CORS, refuse browser origins (current).
- [0006](decisions/0006-renamed-and-hosted-by-cem-ribbonui.md): renamed CEM_NavisIAModeler, hosted by the Cemengal ribbon, port 8765 (current).
- [0007](decisions/0007-per-user-home-no-writes-next-to-the-dll.md): no writes next to the DLL; settings and log in `CemUserHome` (current).
- [0008](decisions/0008-one-job-at-a-time.md): one request at a time, as a job; long scans pause for Navisworks; the client ends every tool call inside the host's timeout (current).
- [0009](decisions/0009-local-access-token.md): a per-start access token in the user's settings folder on every request (current).
- [0010](decisions/0010-send-code-and-screenshot.md): `send_code_to_navisworks` (Roslyn, the escape hatch) and `take_screenshot` (current).

### Sources

- [CEM_NavisworksAPI#1](sources/issues/CEMENGAL-CEM_NavisworksAPI-1.md): the umbrella issue this fork's work belongs to (open).
- [CEM_NavisworksAPI#3](sources/issues/CEMENGAL-CEM_NavisworksAPI-3.md): the IFC Comparator work; this repo's part is 0007, no writes next to the DLL.
- [CEM_NavisworksAPI#16](sources/issues/CEMENGAL-CEM_NavisworksAPI-16.md): "Mejorar NavisworksMCP": no more freezes (0008), the access token (0009), code and screenshots (0010).
- [CEM_API#68](sources/issues/CEM-EnriqueRuiz-CEM_API-68.md): the wiki for every repo; here, qmd through the machine's shared server (open).
- [CEM_API#74](sources/issues/CEM-EnriqueRuiz-CEM_API-74.md): one shared request layer per language; here, the C# rule and the 2026-10-11 review (open).
- [commits 2025-09](sources/commits/2025-09.md): the upstream proof of concept (29 commits, 2025-09-22).
- [commits 2026-10](sources/commits/2026-10.md): the Cemengal fork, the 2027 port, the rename and the hosting.
