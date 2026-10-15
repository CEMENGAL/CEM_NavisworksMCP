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
