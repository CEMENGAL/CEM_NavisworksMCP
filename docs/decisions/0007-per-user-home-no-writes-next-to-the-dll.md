---
title: "0007 — The AI Modeler writes nothing next to its DLL; per-user files live in CemUserHome"
type: decision
status: current
updated: 2026-10-24
sources:
  - MCP_Server/CEM_NavisIAModeler/Services/SettingsManager.cs
  - MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs
  - MCP_Server/CEM_NavisIAModeler/Infrastructure/ManifestBuilder.cs
  - MCP_Client/server/tests/no-dll-adjacent-writes.test.mjs
related:
  - 0006-renamed-and-hosted-by-cem-ribbonui.md
  - 0011-logs-in-local-temp-nothing-under-acc.md
  - ../modules/rpc-server.md
tags: [deploy, settings, log, acc]
---

# 0007 — No writes next to the DLL

**Context.** The Cemengal Navisworks menu moves to a shared ACC (Desktop Connector) folder, loaded by absolute
path. Anything written next to the DLL would sync to every colleague and fail when the folder is read-only.

**Decision.**
- `mcp_manifest.json` is no longer written to disk. Nothing read the file: the client never opens it and both
  `/manifest` and `/mcp_manifest.json` are served from memory by `ManifestBuilder.Build`. The repo source
  `MCP_Server/CEM_NavisIAModeler/mcp_manifest.json` stays (the contract test reads it) but is not shipped.
- `Settings\CEM_NavisIAModeler_settings.xml` and `CEM_NavisIAModeler_log.md` resolve through CEM_CSharpExtended's
  `CemUserHome` (`NavisworksSettingsFile`, `NavisworksFile`): `%APPDATA%\Autodesk\ApplicationPlugins\CEM_NavisworksAPI.bundle`,
  also for the standalone bundle. No `Assembly.Location`, no Documents fallback; `%TEMP%` stays the last resort.
  - **Refined by [0011](0011-logs-in-local-temp-nothing-under-acc.md) on 2026-10-24.** A log is not per-user
    state: it moved to `%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-yyyyMMdd.log` (CEM_CSharpExtended's
    `CemTempFolder`, pruned on its own), out of the roaming profile. Settings have no `%TEMP%` last resort any
    more: without `CemUserHome` they stay in memory for the session.
- Reading shipped files next to the DLL (`ButtonXmlLoader`, standalone ribbon only) is fine.
- `CEM_NavisIAModeler_Shared` references CEM_CSharpExtended (core), so the standalone bundle must also ship
  `CEM_CSharpExtended.dll` and its dependencies (the DLL itself since [0011](0011-logs-in-local-temp-nothing-under-acc.md)).

**Checked by** `no-dll-adjacent-writes.test.mjs`, and every other file write by `file-writes.test.mjs`
([0011](0011-logs-in-local-temp-nothing-under-acc.md)).
