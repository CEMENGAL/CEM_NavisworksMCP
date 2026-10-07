---
title: "CEM_NavisIAModeler — the AI Modeler's HTTP/RPC server inside Navisworks"
type: module
updated: 2026-10-07
sources: [MCP_Server/CEM_NavisIAModeler/Core/MCPServiceConnection.cs, MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs, MCP_Server/CEM_NavisIAModeler/Infrastructure/RpcRouter.cs, MCP_Server/CEM_NavisIAModeler/Mapping/RpcMap.cs, MCP_Server/CEM_NavisIAModeler/Services/Backends/]
related: [../concepts/rpc-contract.md, ../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md, addin-ribbon.md, node-client.md, ../workflows/add-a-tool.md]
tags: [module, server, rpc]
---

# CEM_NavisIAModeler

Assembly `CEM_NavisIAModeler.dll` (upstream's `waabe_navi_mcp_server`, renamed by
[0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)). In a Cemengal checkout it ships inside
`CEM_NavisworksAPI.bundle` and the Cemengal tab's **AI → AI Modeler** button runs it; standalone, it
ships in `CEM_NavisIAModeler.bundle` with [the standalone ribbon](addin-ribbon.md).

## Layers

| Folder | Role |
|---|---|
| `Core/MCPServiceConnection.cs` (Cemengal) | The host's entry point: `ToggleAsync()` (creates the service on the UI thread on first use, starts or stops it, never throws), `IsRunning`, `Port`, `DefaultPort = 8765`. |
| `Plugins/MCPServerRegistrar.cs` | `MCPServerService`: the server's lifecycle (start/stop, port and "enabled" settings; auto-start when the settings say it was running). Also an `EventWatcherPlugin` that registers the service at start-up, which only runs in the standalone bundle and logs a `SynchronizationContext` error there ([known gaps](../concepts/known-gaps.md)). |
| `Services/MCPServer.cs` | `HttpListener` on `http://127.0.0.1:<port>/` and `http://localhost:<port>/`. Sends no CORS headers and answers 403 to any request with an `Origin` header ([0005](../decisions/0005-no-cors-refuse-browser-origins.md)). Endpoints: `GET /` (a banner), `GET /health`, `GET /manifest` and `/mcp_manifest.json` (built from the routes by `ManifestBuilder`, written beside the DLL), `POST /rpc`. JSON via `JavaScriptSerializer` (`System.Web.Extensions`). |
| `Services/SettingsManager.cs` | `Settings\CEM_NavisIAModeler_settings.xml` in the bundle folder (the one the DLL was loaded from): port (default 8765) and enabled state. |
| `Infrastructure/RpcRouter.cs` | `Dispatch(req)`: `NVX_BAD_REQUEST` without a method, `NVX_NOT_FOUND` for an unknown one, else the route. |
| `Mapping/RpcMap.cs` | **The route table**: method name → controller method, each wrapped to add `meta` (timing). The names are [the RPC contract](../concepts/rpc-contract.md). |
| `Controllers/` | One per domain: `Model`, `Search`, `Selection`, `Clash`, plus `Export`, `Visibility` and `System`, which have no routes yet. They deserialize `params` into `Contracts/Queries.cs` types and call a backend. |
| `Validation/Validators.cs` | `EnsureNonEmpty`, `EnsureLimit` (1–5000), `EnsureOffset`. |
| `Services/Backends/` | `BackendResolver` picks `ReflectionBackend` when the UI thread is initialized, else `FallbackBackend`; both implement `IWaabeNavisworksBackend`. `UiThread` marshals calls onto Navisworks' UI thread. `FallbackBackend` holds most of the Navisworks API and COM code, including `RunSimpleClash` and the slow `ResolveItemsByCanonicalIds` ([known gaps](../concepts/known-gaps.md)). |
| `Services/Implementations/`, `Infrastructure/Caching`, `ErrorHandlingMiddleware`, `Telemetry/` | Domain services, response cache, error envelope, metrics for `meta`. |
| `Dialogs/PortConfigDialog.xaml` | A WPF port dialog (used by the standalone ribbon). |

The server talks to Navisworks through `Autodesk.Navisworks.Api`, the COM bridge (`ComApi`,
`Interop.ComApi`) and `Autodesk.Navisworks.Api.Clash`, all compile-only from NuGet
([0001](../decisions/0001-sdk-style-2027-build.md)). It references only `CEM_NavisIAModeler_Shared` ([shared](shared.md)).
