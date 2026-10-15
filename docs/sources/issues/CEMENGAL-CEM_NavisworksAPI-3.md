---
title: "CEMENGAL/CEM_NavisworksAPI#3: the IFC Comparator work, this repo's part (no writes next to the DLL)"
type: source
updated: 2026-10-15
sources: [https://github.com/CEMENGAL/CEM_NavisworksAPI/issues/3]
related: [../../decisions/0007-per-user-home-no-writes-next-to-the-dll.md, ../../concepts/known-gaps.md, ../../workflows/smoke-test.md]
tags: [issue, acc, settings, log, manifest]
---

# CEMENGAL/CEM_NavisworksAPI#3

**Issue:** https://github.com/CEMENGAL/CEM_NavisworksAPI/issues/3 · work 2026-10-11 → 2026-10-15. The issue
merged the IFC status tool into the IFC Comparator across CEM_API, CEM_WEB_Library, CEM_CSharpExtended,
CEM_NavisworksAPI, CEM_IFC and CEM_Viewer. **This repo changed in one round only (5j).**

## What the user asked (round 5j, 2026-10-15)
The Navisworks Cemengal menu moves to a shared ACC (Desktop Connector) folder, loaded by absolute path from
every colleague's `PackageContents.xml` (tested: Navisworks 2027 accepts it). So "the AI Modeler must not write
next to its DLL": anything written there would sync to every colleague and fail when the folder is read-only or
locked. The user also wants unused code gone.

## Decisions and reasons
| Decision | Reason | Rejected |
|---|---|---|
| Delete the `mcp_manifest.json` disk write (`TryWriteToDisk`, `GetDefaultContentDirectory`, the shipped copy) | Nothing read the file: the Node client never opens it, both HTTP endpoints build the manifest in memory | Moving the write to the user home (keeps dead code) |
| Keep the repo source `mcp_manifest.json` and its contract test | It is source, read by `npm test` | Deleting it (loses the contract check) |
| Settings and log through CEM_CSharpExtended's `CemUserHome` (`NavisworksSettingsFile`, `NavisworksFile`) | One source of truth for the per-user home, shared with CEM_NavisworksAPI; no `Assembly.Location`, no `..\..` | Host hands the path over (CEM_NavisworksAPI's first proposal: a `UserPaths.Bundle` set by `CemRibbon`): needs a new init contract, breaks the standalone ribbon, and `LogHelper`'s static initialiser runs before any hand-over |
| Standalone bundle uses the same home | One home | A second home under `CEM_NavisIAModeler.bundle` |
| Drop the Documents and AppData fallbacks; keep `%TEMP%` | `%TEMP%` is per user and not DLL-adjacent | Silent fallback to the old location |
| `ButtonXmlLoader` keeps reading from the DLL folder | Reading shipped files is fine | |

File names are unchanged (`Settings\CEM_NavisIAModeler_settings.xml`, `CEM_NavisIAModeler_log.md`); the folder
is now always `CEM_NavisworksAPI.bundle`. Files of a standalone install under `CEM_NavisIAModeler.bundle` are not migrated.

## Contract with the other repos
- CEM_CSharpExtended: `CEM_CSharpExtended.Hosting.CemUserHome` (core project). `CEM_NavisIAModeler_Shared`
  references it by `ProjectReference` (netstandard2.0, builds on net48).
- CEM_NavisworksAPI deploys this repo's DLLs and must not expect `mcp_manifest.json` (its copy and publish lists dropped it).
- Open: the standalone bundle does not yet ship `CEM_CSharpExtended.dll` and dependencies ([known gaps](../../concepts/known-gaps.md) item 10).

## Verified
`npm test` 17/17 (3 new in `no-dll-adjacent-writes.test.mjs`, written red first); solution builds `Debug N27`,
0 warnings, 0 errors. CEM_NavisworksAPI then ran Navisworks 2027 from the absolute ACC-style path: three buttons,
the AI Modeler's health and manifest answered from memory.
