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
