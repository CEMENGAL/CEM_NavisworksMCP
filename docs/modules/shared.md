---
title: "CEM_NavisIAModeler_Shared — service registry and logging"
type: module
updated: 2026-10-07
sources: [MCP_Server/CEM_NavisIAModeler_Shared/ServiceRegistry.cs, MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs, MCP_Server/CEM_NavisIAModeler_Shared/IWaabeService.cs, MCP_Server/CEM_NavisIAModeler_Shared/WaabeRibbon.cs]
related: [addin-ribbon.md, rpc-server.md]
tags: [module, shared]
---

# CEM_NavisIAModeler_Shared

Assembly `CEM_NavisIAModeler_Shared.dll` (upstream's `waabe_navi_shared`), referenced by the server and
the standalone ribbon; CEM_RibbonUI references it too, with the host flag, so MSBuild builds it once.

- `IWaabeService` + `ServiceRegistry`: the server registers its service here (key
  `CEM_NavisIAModeler`); `MCPServiceConnection` and the standalone ribbon find it there.
- `LogHelper`: `LogEvent`, `LogInfo`, `LogDebug`, appending to `CEM_NavisIAModeler_log.md` in the bundle
  folder the DLL was loaded from (`CEM_NavisworksAPI.bundle` when hosted).
- `WaabeRibbon`: upstream's ribbon constants (class name kept).
