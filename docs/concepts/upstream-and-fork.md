---
title: "Upstream and fork — what is theirs, what is ours"
type: concept
updated: 2026-10-07
sources: [README.md, LICENSE, "git log upstream/main"]
related: [../decisions/0004-minimal-divergence-from-upstream.md, ../sources/commits/2025-09.md, ../sources/commits/2026-10.md]
tags: [fork, upstream]
---

# Upstream and fork

| | |
|---|---|
| Upstream | `https://github.com/kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026`, remote `upstream` |
| Fork | `https://github.com/CEMENGAL/CEM_NavisworksMCP`, remote `origin` (a public GitHub fork, like CEM_RevitMCP) |
| Licence | MIT, © 2025 kikki. Keep `LICENSE` and the attributions |
| Upstream state | a proof of concept for Navisworks Manage 2026: 29 commits on 2025-09-22, no activity since ([sources](../sources/commits/2025-09.md)) |

## What we changed (2026-10-07)

- **Build:** three old-style csproj with `HintPath`s into `C:\Program Files\Autodesk\Navisworks Manage 2026`
  became SDK-style projects with a shared `MCP_Server/Directory.Build.props` and the NuGet API
  ([0001](../decisions/0001-sdk-style-2027-build.md)). The solution's configurations became `Debug N27`/`Release N27`.
- **Deploy:** three German `xcopy` post-build events became one MSBuild target
  ([0002](../decisions/0002-msbuild-bundle-deploy.md)).
- **2027 API:** `FallbackBackend.RunSimpleClash` uses `TestsData.Value.TestsRoot` and
  `TestsAddCopy(parent, test)` (2026 had `TestsData.Tests` and `TestsAddCopy(test)`).
- **Version strings:** `v23`→`v24`, `Nw23`→`Nw24`, "2026"→"2027" in `PackageContents.xml`,
  `ManifestBuilder`, `mcp_manifest.json`, the client manifest and the info dialog (which now names the fork).
- **Cleanup:** `PackageContents.xml` no longer lists two DLLs the solution does not build
  (`waabe_navi_chatfenster`, `waabe_navi_mcpserver`). `mcp_manifest.json` lists exactly the routed
  methods (it advertised a commented-out `ping` and missed four).
- **Security:** `MCPServer` no longer sends `Access-Control-Allow-Origin: *` and refuses requests
  with an `Origin` header, so a web page cannot drive Navisworks
  ([0005](../decisions/0005-no-cors-refuse-browser-origins.md)).
- **Added:** `MCP_Client/server/tests/` and `npm test` ([0003](../decisions/0003-rpc-contract-tests.md)),
  `docs/`, `CLAUDE.md`, `AGENTS.md`, the `cem-navismcp-dev` agent and skill, hook and qmd settings.

Every code change carries a `Cemengal` comment or is in a Cemengal-only file
([0004](../decisions/0004-minimal-divergence-from-upstream.md)).

### Renamed and hosted (later the same day)

At the user's request the fork got CEM_RevitMCP's treatment
([0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)): `waabe_navi_mcp_server` →
`CEM_NavisIAModeler`, `waabe_navi_shared` → `CEM_NavisIAModeler_Shared`, `waabe_navi_mcp` →
`CEM_NavisIAModeler_Ribbon` (standalone only), `waabe_navi_mcp.sln` → `CEM_NavisworksMCP.sln`, the client
→ `cem-navis-aimodeler`, namespaces included. CEM_NavisworksAPI's ribbon hosts the server; both halves
default to port 8765; the clash snapshot bug was fixed.

## Syncing from upstream

```
git fetch upstream
git log --oneline main..upstream/main      # anything new?
git merge upstream/main                    # expect conflicts in the csproj files and version strings
```

Upstream has been silent since 2025-09-22, so a merge is unlikely. Since the rename every namespace and
project path differs, so a sync is a manual port of upstream's change, not a merge.
