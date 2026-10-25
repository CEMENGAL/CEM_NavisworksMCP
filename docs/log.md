---
title: "Wiki log"
type: log
updated: 2026-10-11
sources: []
related:
  - index.md
tags: [log]
---

# Wiki log

This log is append-only. Each entry uses the heading `## [YYYY-MM-DD] ingest|decision|lint|query | title`.

## [2026-10-07] ingest | Fork bootstrap: 2027 port, contract and security tests, cem-navismcp-dev skill and the wiki

Forked `kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026` to `CEMENGAL/CEM_NavisworksMCP` and ported it
to Navisworks Manage 2027. Recorded in [sources/commits/2026-10](sources/commits/2026-10.md), with the
upstream history in [2025-09](sources/commits/2025-09.md). Decisions
[0001](decisions/0001-sdk-style-2027-build.md)–[0005](decisions/0005-no-cors-refuse-browser-origins.md),
four modules, three concepts, six workflows. [Known gaps](concepts/known-gaps.md) lists what was not
verified and the missing authentication. The same day it ran in Navisworks 2027: bundle, server,
seven tools and the Origin block confirmed; whole-model clash scopes freeze the UI in upstream's id
lookup ([smoke test](workflows/smoke-test.md)).

## [2026-10-08] ingest | Renamed to CEM_NavisIAModeler, hosted by CEM_NavisworksAPI's ribbon, one port 8765
At the user's request the fork got CEM_RevitMCP's treatment: projects and namespaces renamed (`CEM_NavisIAModeler`, `_Shared`, `_Ribbon`, `CEM_NavisworksMCP.sln`, client `cem-navis-aimodeler`), the server hosted by the Cemengal tab's AI Modeler button through `Core/MCPServiceConnection`, the standalone bundle only without the host, port 8765 on both halves, the clash snapshot bug fixed. New decision [0006](decisions/0006-renamed-and-hosted-by-cem-ribbonui.md); 0001/0002/0004 refined; modules, workflows and [known gaps](concepts/known-gaps.md) rewritten; live results in the [smoke test](workflows/smoke-test.md). Recorded in [sources/commits/2026-10](sources/commits/2026-10.md).

## [2026-10-08] ingest | CEMENGAL/CEM_NavisworksAPI#1 Create the repository and set up the menu
The user tested and supplied the umbrella issue. Added [sources/issues/CEMENGAL-CEM_NavisworksAPI-1](sources/issues/CEMENGAL-CEM_NavisworksAPI-1.md), linking the decisions and the detailed record in [sources/commits/2026-10](sources/commits/2026-10.md); this commit carries the work under that link.

## [2026-10-11] ingest | qmd MCP through the machine's shared server (root decision 0020)
`.mcp.json`'s `qmd` entry no longer starts a stdio `qmd mcp` per session; it is `{ "type": "http", "url": "http://127.0.0.1:8181/mcp" }`, the machine's one shared server (root decision 0020: per-session servers loaded 2 to 3.5 GB of models each and exhausted RAM). The name stays `qmd`, so `enabledMcpjsonServers` is unchanged. The fork's bootstrap had copied the old entry and missed the other repos' 2026-10-08 switch. Recorded in the new [CEM_API#68 source](sources/issues/CEM-EnriqueRuiz-CEM_API-68.md), [upstream and fork](concepts/upstream-and-fork.md) and the index.

## [2026-10-11] ingest | CEM-EnriqueRuiz/CEM_API#74 the C# side of the request-layer rule
Root decision 0023 now covers C#: every CEM_API call goes through CEM_CSharpExtended's `Api` (with 0019), never a route string or URL in an add-in, and a missing call is added to the library first. Added as hard rule 7 in the index's Start here, with a "Call CEM_API from this repo" row (0023, 0019, 0021, 0025, CEM_CSharpExtended's core `Api` page). The 2026-10-11 review found this repo compliant: no CEM_API request, its `localhost` URLs are its own MCP server; recorded in the new [CEM_API#74 source](sources/issues/CEM-EnriqueRuiz-CEM_API-74.md), the [overview](overview.md) and a "checked, not a gap" note in [known gaps](concepts/known-gaps.md). The [CEM_API#68 source](sources/issues/CEM-EnriqueRuiz-CEM_API-68.md) no longer quotes an unverified issue title.

## [2026-10-11] ingest | CEM-EnriqueRuiz/CEM_API#74 the request-layer rule in the cem-navismcp-dev skill
At the user's request, the skill carries the line proposed on 2026-10-11: any CEM_API call through CEM_CSharpExtended's `Api` (root 0023).

## [2026-10-15] ingest | The AI Modeler writes nothing next to its DLL
ACC will host the menu, so [0007](decisions/0007-per-user-home-no-writes-next-to-the-dll.md): the `mcp_manifest.json` disk write is deleted (nothing read it), settings and log go through CEM_CSharpExtended's `CemUserHome`, covered by `no-dll-adjacent-writes.test.mjs`.

## [2026-10-15] ingest | CEMENGAL/CEM_NavisworksAPI#3 the IFC Comparator work, this repo's part
New [issue source](sources/issues/CEMENGAL-CEM_NavisworksAPI-3.md) with the decisions and rejected alternatives for 0007. Drift fixed: [0002](decisions/0002-msbuild-bundle-deploy.md) no longer lists `mcp_manifest.json` as shipped or `GetDefaultContentDirectory`, [shared](modules/shared.md) names `CemUserHome` for the log, the [smoke test](workflows/smoke-test.md) no longer expects Hello World (deleted 2026-10-15), and [known gaps](concepts/known-gaps.md) item 10 records that the standalone bundle does not ship `CEM_CSharpExtended.dll`. Index lists the source.

## [2026-10-21] ingest | One request at a time, as a job; long scans pause for Navisworks
After an agent's night of "Navisworks is not answering" on the 1.4-million-item STC01 federated model, the AI Modeler's log showed why: requests queued on the UI thread behind a 49-minute `list_items_to_property` that ignored its scope, a 10 s token that returned partial scans as complete and re-ran a cancelled call off the UI thread, model ids found by scanning every item, and a clash count of 0 read from a disposed snapshot. New [0008](decisions/0008-one-job-at-a-time.md): one request at a time as a job (`NVX_BUSY`, `NVX_JOB_RUNNING`, the tools `get_server_status`, `get_job_status`, `cancel_job`), scans that pause every 100 ms and stop on cancel, a client that ends every tool call within 45 s, the scope honoured and a 200-item cap, model ids from the model list, whole-model clash scopes, the count read after the run. Updated the [server](modules/rpc-server.md) and [client](modules/node-client.md) modules, the [RPC contract](concepts/rpc-contract.md), [known gaps](concepts/known-gaps.md), the [overview](overview.md), [test](workflows/test.md) (two new test files, 33 tests; also lists the 2026-10-15 `no-dll-adjacent-writes` file it missed), [add a tool](workflows/add-a-tool.md), the [smoke test](workflows/smoke-test.md) (a jobs step, not run yet), [connect an agent](workflows/connect-an-agent.md) (Claude Desktop by its config file, in the MSIX data folder), hard rule 8 in the index and the dev skill.

## [2026-10-21] ingest | A local access token; send_code_to_navisworks and take_screenshot
At the user's request, the tools CEM_RevitMCP has and this fork lacked: [0010](decisions/0010-send-code-and-screenshot.md) adds `send_code_to_navisworks` (C# compiled with Roslyn 4.8.0 on the job's thread, run on the UI thread, bounded JSON result, scripts never persisted) and `take_screenshot` (the 3D view by `View.GenerateImage`, or the main window). Running code needed authentication first: [0009](decisions/0009-local-access-token.md) adds a per-start token in the user's settings folder, required on every request but the health check, and closes known gap 2 (CEM_RevitMCP's proposed 0022, built here). [0005](decisions/0005-no-cors-refuse-browser-origins.md) marked as refined. Updated the index (hard rule 6, a lookup row, the catalog), the dev skill, the [server](modules/rpc-server.md) and [client](modules/node-client.md) modules, the [RPC contract](concepts/rpc-contract.md), [known gaps](concepts/known-gaps.md) (2 rewritten, 10 extended, new 11 and 12), the [overview](overview.md), the [smoke test](workflows/smoke-test.md) (token in the curl steps, a code and screenshot step), [connect an agent](workflows/connect-an-agent.md) and [test](workflows/test.md) (two new test files, 42 tests). Live the same day on the STC01 federated model ([smoke test](workflows/smoke-test.md)): token, code, screenshots, `NVX_BUSY`, progress with Navisworks responsive, and `cancel_job` in 96 ms confirmed; after a stop for low memory, the scoped list (0.1 s), the model-id selection (0.2 s) and the pilot clash (2.3 s, 62 results, as Clash Detective counts; the morning's 0 was the count bug) passed too. A list of anonymous objects now reports its type as `List<anonymous object>`.

## [2026-10-21] ingest | CEMENGAL/CEM_NavisworksAPI#16 Mejorar NavisworksMCP
The user supplied the issue for the day's work. Added [sources/issues/CEMENGAL-CEM_NavisworksAPI-16](sources/issues/CEMENGAL-CEM_NavisworksAPI-16.md) (what was asked, what the log showed, each decision with its reason and the rejected alternatives, what was verified live, what stays open) and listed it in the index. No root feature page: the work is in this repo only; CEM_NavisworksAPI was only rebuilt to deploy it.

## [2026-10-24] ingest | Nothing under ACC; the log in a self-cleaning local temp folder (root 0031)
At the user's request (no writes under ACC, logs in a local temp folder that cleans itself), every file write of the AI Modeler and the client was audited against root decision 0031. New [0011](decisions/0011-logs-in-local-temp-nothing-under-acc.md): `LogHelper` writes a daily `%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-yyyyMMdd.log` through CEM_CSharpExtended's `CemTempFolder` (it was a Markdown file in the roaming per-user home that never shrank, plus two `%TEMP%` fallbacks); `SettingsManager` keeps its settings in memory when `CemUserHome` fails (it fell back to `%TEMP%`); the token stays in `CemUserHome`; screenshots and scripts stay in memory; the client writes nothing; the standalone bundle copies `CEM_CSharpExtended.dll`. New guard `file-writes.test.mjs` (51 tests, red first), plus a throwaway harness for the settings fallback (HEAD red, new green). Updated [0007](decisions/0007-per-user-home-no-writes-next-to-the-dll.md) (refined), [0006](decisions/0006-renamed-and-hosted-by-cem-ribbonui.md) (file name), [shared](modules/shared.md), the [server](modules/rpc-server.md), [test](workflows/test.md), the [smoke test](workflows/smoke-test.md) (a files step), [known gaps](concepts/known-gaps.md) (5, 10), hard rule 9 and a lookup row in the index, and the dev skill. Live the same day through `index.js` over stdio ([smoke test](workflows/smoke-test.md)): 17 calls on the STC01 drainage IFCs, only the daily log changed, no watcher event under ACC or beside the DLLs. Not committed yet: the source page comes with the issue link.

## [2026-10-25] ingest | CEM-EnriqueRuiz/CEM_API#82 Nothing of the AI Modeler under ACC; the log in local temp
The user tested the 2026-10-24 work, sent the issue (empty body) and kept every change; only the MCPs were in question, and root 0031 now gives them no exception. Added [sources/issues/CEM-EnriqueRuiz-CEM_API-82](sources/issues/CEM-EnriqueRuiz-CEM_API-82.md): what was asked, the audit, each decision with its reason and the rejected alternatives, the user's decisions, the files changed, the tests (red first, 51/51), the live calls and the watcher result, and what stays open (the stale old logs, Navisworks' `.nwc` cache, the reported doc drift). [0011](decisions/0011-logs-in-local-temp-nothing-under-acc.md) cites the issue and the user's acceptance; the index lists the source page. The ACC publish of the Navisworks menu stays with the coordinator.
