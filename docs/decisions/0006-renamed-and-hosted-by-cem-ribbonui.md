---
title: "0006 — Renamed to CEM_NavisIAModeler and hosted by CEM_NavisworksAPI's ribbon, like CEM_RevitMCP"
type: decision
status: current
updated: 2026-10-07
sources:
  - MCP_Server/CEM_NavisIAModeler/Core/MCPServiceConnection.cs
  - MCP_Server/Directory.Build.props
  - MCP_Server/CEM_NavisIAModeler_Ribbon/CEM_NavisIAModeler_Ribbon.csproj
  - MCP_Client/server/tests/default-port.test.mjs
  - ../../CEM_NavisworksAPI/CEM_RibbonUI/CemRibbon.cs
related:
  - 0002-msbuild-bundle-deploy.md
  - 0004-minimal-divergence-from-upstream.md
  - ../modules/rpc-server.md
  - ../workflows/smoke-test.md
  - ../../../CEM_RevitMCP/docs/decisions/0011-cem-ribbonui-hosts-deployment.md
  - ../../../CEM_RevitMCP/docs/decisions/0024-standalone-deploy-only-without-ribbon-host.md
  - ../../../../CEM_NavisworksAPI/docs/decisions/0007-ribbon-hosts-the-ai-modeler.md
tags: [rename, hosting, ribbon, deploy, port]
---

# 0006 — Renamed to CEM_NavisIAModeler and hosted by the Cemengal ribbon

## Context

After the first live run the user asked to "rename MCP as we did with RevitAPI and integrate inside
Cemengal Ribbon". CEM_RevitMCP renamed upstream's projects (`RevitMCPPlugin` → `CEM_IAModeler`,
namespaces included) and is hosted by CEM_RevitAPI's `CEM_RibbonUI`, which owns its deployment and
shows its button on the Cemengal tab ([its 0011](../../../CEM_RevitMCP/docs/decisions/0011-cem-ribbonui-hosts-deployment.md));
its standalone deploy runs only without that host ([its 0024](../../../CEM_RevitMCP/docs/decisions/0024-standalone-deploy-only-without-ribbon-host.md)).
Here the first run showed a separate **waabe** tab, a port dialog defaulting to 8080 (the client
defaulted to 1234, and 8080 is CEM_RevitMCP's port), and a modal "MCP Server erfolgreich gestartet!"
box that stayed open behind the work.

## Decision

- **Rename**, projects, folders, assemblies and namespaces:

  | Upstream | Now | Role |
  |---|---|---|
  | `waabe_navi_mcp_server` | `CEM_NavisIAModeler` | the HTTP/RPC server, controllers, backends |
  | `waabe_navi_shared` | `CEM_NavisIAModeler_Shared` | service registry, logging |
  | `waabe_navi_mcp` | `CEM_NavisIAModeler_Ribbon` | upstream's own ribbon tab, **standalone use only** |
  | `waabe_navi_mcp.sln` | `CEM_NavisworksMCP.sln` | |
  | client `waabe-navisworks-mcp` | `cem-navis-aimodeler` | the MCP server name agents see, the `.dxt` and npm name |

  Settings and log files follow (`CEM_NavisIAModeler_settings.xml`, `CEM_NavisIAModeler_log.md`).
  User-visible "WAABE" texts became "CEM AI Modeler"; the plugin developer id is `CEMG`. Upstream's
  internal class names (`IWaabeService`, `IWaabeNavisworksBackend`, `WaabeRibbon`) stay, as
  CEM_RevitMCP kept upstream class names.
- **Hosting.** `CEM_NavisIAModeler/Core/MCPServiceConnection` is the entry point a host calls:
  `ToggleAsync()` (creates the service on the UI thread on first use, starts or stops it, never throws),
  `IsRunning`, `Port`, `DefaultPort`. CEM_NavisworksAPI's `CEM_RibbonUI` references `CEM_NavisIAModeler`
  (and `_Shared`) with `CemRibbonHostBuild=true`, ships them in `CEM_NavisworksAPI.bundle`, and shows
  an **AI → AI Modeler** button ([its 0007](../../../../CEM_NavisworksAPI/docs/decisions/0007-ribbon-hosts-the-ai-modeler.md)).
  The server no longer references the ribbon project (it never used its types).
- **No success dialog in the hosted path.** The host's button is pressed while the server runs and
  its label carries the port (`AI Modeler (8765)`); only a failed start shows a message.
- **Who deploys.** `MCP_Server/Directory.Build.props` sets `CemRibbonHostPresent` when
  `../../CEM_NavisworksAPI/CEM_RibbonUI/CEM_RibbonUI.csproj` exists and `CemStandaloneDeploy` only when
  neither that nor `CemRibbonHostBuild` holds. The standalone bundle (`CEM_NavisIAModeler.bundle`, with
  the ribbon plugin) moved from the server project to `CEM_NavisIAModeler_Ribbon` and runs only then.
  So in a Cemengal checkout one bundle exists, however the build is entered (the 0024 lesson).
- **One default port, 8765**, on both halves (`SettingsManager`, `MCPServiceConnection.DefaultPort`,
  the port dialogs, `index.js`, the `.dxt` `api_port` default). `default-port.test.mjs` fails if any
  literal differs.

**Rejected:**
- Keeping upstream's names: the user asked for the Revit treatment, and "waabe" in the ribbon and in
  agents' MCP lists is someone else's brand.
- Deleting upstream's ribbon plugin: it is the fork's standalone entry, and costs nothing when the
  host owns deployment.
- Adding the fork's projects to `CEM_NavisworksAPI.sln`: CEM_RevitMCP's 0024 shows that a direct
  solution build bypasses the host flag. The host references them; the host-presence check covers the rest.

## Consequences

- Merging upstream is now a manual port: every namespace differs ([upstream and fork](../concepts/upstream-and-fork.md)).
- A project outside the building solution gets no `N27` configuration; `Directory.Build.props` defaults
  the year, so it still targets `net48`.
- The host must list `CEM_NavisIAModeler_Shared` itself with the same property, or MSBuild builds it twice.
- Verified live on 2026-10-07 ([smoke test](../workflows/smoke-test.md) results).
