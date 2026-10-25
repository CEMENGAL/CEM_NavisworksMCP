---
title: "CEM_NavisIAModeler_Shared — service registry and logging"
type: module
updated: 2026-10-24
sources: [MCP_Server/CEM_NavisIAModeler_Shared/ServiceRegistry.cs, MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs, MCP_Server/CEM_NavisIAModeler_Shared/IWaabeService.cs, MCP_Server/CEM_NavisIAModeler_Shared/WaabeRibbon.cs]
related: [addin-ribbon.md, rpc-server.md, ../decisions/0011-logs-in-local-temp-nothing-under-acc.md]
tags: [module, shared]
---

# CEM_NavisIAModeler_Shared

Assembly `CEM_NavisIAModeler_Shared.dll` (upstream's `waabe_navi_shared`), referenced by the server and
the standalone ribbon; CEM_RibbonUI references it too, with the host flag, so MSBuild builds it once.

- `IWaabeService` + `ServiceRegistry`: the server registers its service here (key
  `CEM_NavisIAModeler`); `MCPServiceConnection` and the standalone ribbon find it there.
- `LogHelper`: `LogEvent`, `LogInfo`, `LogDebug`…, one line each (`HH:mm:ss.fff [PROJECT] message`) appended to
  today's `%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-yyyyMMdd.log` through CEM_CSharpExtended's
  `CemTempFolder.AppendLog` (shared, thread-safe, never throws; the folder prunes itself after 7 days or
  50 MB). A line that cannot be written goes to the debugger output only. `GetCurrentLogPath()` names today's
  file ([0011](../decisions/0011-logs-in-local-temp-nothing-under-acc.md); until 2026-10-24 a Markdown
  `CEM_NavisIAModeler_log.md` in the per-user home, [0007](../decisions/0007-per-user-home-no-writes-next-to-the-dll.md)).
- `WaabeRibbon`: upstream's ribbon constants (class name kept).
