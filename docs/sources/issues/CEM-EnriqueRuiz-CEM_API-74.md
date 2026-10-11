---
title: "CEM-EnriqueRuiz/CEM_API#74: one shared request layer per language; here, the C# rule and the 2026-10-11 review"
type: source
updated: 2026-10-11
sources: [https://github.com/CEM-EnriqueRuiz/CEM_API/issues/74, MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs, MCP_Client/server/index.js]
related: [../../index.md, ../../overview.md, ../../concepts/known-gaps.md, ../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-74.md, ../../../../../docs/decisions/0023-one-shared-request-layer.md, ../../../../../docs/decisions/0019-cem-csharpextended-shared-csharp-library.md]
tags: [issue, api, cem_api, cem_csharpextended, cross-repo]
---

# CEM-EnriqueRuiz/CEM_API#74

**Issue:** https://github.com/CEM-EnriqueRuiz/CEM_API/issues/74 · open. It lives in CEM_API's tracker: generic
endpoints only, and one shared request layer for every app
([feature page](../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-74.md)). This repo got docs only.

## The rule (2026-10-11)
Root decision [0023](../../../../../docs/decisions/0023-one-shared-request-layer.md) now covers C#: every app
talks to CEM_API through one shared request layer per language. For C# that layer is CEM_CSharpExtended's
`Api` namespace (`CemApiClient`, `AuthorizedRequests`, `CemApiEndpoints`, `IfcPairJobsClient`/`IfcPairsClient`,
the models, the auth and the socket), the twin of CEM_WEB_Library's `util/api`
([0019](../../../../../docs/decisions/0019-cem-csharpextended-shared-csharp-library.md)). So:
- an add-in never spells a CEM_API route or builds its URL;
- it checks the library's existing methods first;
- a missing call is added to the library first, then used.

Related root decisions: [0021](../../../../../docs/decisions/0021-generic-data-endpoints-app-shapes.md) (routes
are generic, never shaped for one tool) and [0025](../../../../../docs/decisions/0025-desktop-client-deprecation-window.md)
(installed add-ins get a deprecation window before a route they use changes).

## What this repo got
- Hard rule 7 in the [index](../../index.md) "Start here", and a "Call CEM_API from this repo" row in its
  "before you change" table, linking 0023, 0019, 0021, 0025 and CEM_CSharpExtended's
  [core `Api`](../../../../../CEM_CSharpExtended/docs/modules/cem-csharpextended.md) page.
- A line in the [overview](../../overview.md)'s place in the family, and a "checked, not a gap" note in
  [known gaps](../../concepts/known-gaps.md).

## The review (2026-10-11)
- **Compliant.** This repo makes no CEM_API request. Its `127.0.0.1`/`localhost` URLs are its own MCP server:
  `MCPServer.ServerUrl` and the listener prefixes, the registrar's and the standalone button's display URL,
  and the Node client's `http://localhost:${port}`. There is no CEM_API route string and no CEM_CSharpExtended
  reference.
- CEM_RevitAPI, CEM_TeklaAPI and CEM_RevitMCP were found compliant too. The open items belong to other repos
  and are recorded in root 0023: CEM_NavisworksAPI's `ViewerRequests.ResultPath`, and in CEM_CSharpExtended no
  contract replay, no consumer guard, and the desktop-shaped routes.
- No guard test fails on a CEM_API route here, and none exists in any C# consumer yet (root 0023). There is
  nothing to guard here today; if this repo ever references CEM_CSharpExtended, it gets the guard the other
  consumers get then.

## Skill update (2026-10-11)

At the user's direct request, the dev skill's hard rules now carry the one-request-layer rule (root decisions 0021-0025), so an agent meets it before reading the wiki.
