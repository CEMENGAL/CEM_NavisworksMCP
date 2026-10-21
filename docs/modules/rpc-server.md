---
title: "CEM_NavisIAModeler — the AI Modeler's HTTP/RPC server inside Navisworks"
type: module
updated: 2026-10-21
sources: [MCP_Server/CEM_NavisIAModeler/Core/MCPServiceConnection.cs, MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs, MCP_Server/CEM_NavisIAModeler/Infrastructure/RpcRouter.cs, MCP_Server/CEM_NavisIAModeler/Infrastructure/RpcJobs.cs, MCP_Server/CEM_NavisIAModeler/Mapping/RpcMap.cs, MCP_Server/CEM_NavisIAModeler/Controllers/JobsController.cs, MCP_Server/CEM_NavisIAModeler/Services/Backends/]
related: [../concepts/rpc-contract.md, ../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md, ../decisions/0008-one-job-at-a-time.md, ../decisions/0009-local-access-token.md, ../decisions/0010-send-code-and-screenshot.md, addin-ribbon.md, node-client.md, ../workflows/add-a-tool.md]
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
| `Services/MCPServer.cs` | `HttpListener` on `http://127.0.0.1:<port>/` and `http://localhost:<port>/`. Sends no CORS headers and answers 403 to any request with an `Origin` header ([0005](../decisions/0005-no-cors-refuse-browser-origins.md)). Endpoints: `GET /` (a banner), `GET /health`, `GET /manifest` and `/mcp_manifest.json` (built from the routes by `ManifestBuilder`, served from memory, never written to disk: [0007](../decisions/0007-per-user-home-no-writes-next-to-the-dll.md)), `POST /rpc`. JSON via `JavaScriptSerializer` (`System.Web.Extensions`), with no 2 MB reply limit. Stopping it cancels the running job ([0008](../decisions/0008-one-job-at-a-time.md)). Every request but `GET /` and `/health` needs `Authorization: Bearer <token>`, else 401 ([0009](../decisions/0009-local-access-token.md)). |
| `Infrastructure/AccessToken.cs` (Cemengal) | Issues the per-start token to `CemUserHome.NavisworksSettingsFile("CEM_NavisIAModeler.token")` once the port is bound, revokes it at stop, compares headers in constant time ([0009](../decisions/0009-local-access-token.md)). |
| `Services/SettingsManager.cs` | `Settings\CEM_NavisIAModeler_settings.xml` in the per-user home (`CemUserHome`, [0007](../decisions/0007-per-user-home-no-writes-next-to-the-dll.md)): port (default 8765) and enabled state. |
| `Infrastructure/RpcRouter.cs` | `Dispatch(req)`: `NVX_BAD_REQUEST` without a method, `NVX_NOT_FOUND` for an unknown one; the job tools run at once, every other route through `RpcJobs.Run`. |
| `Infrastructure/RpcJobs.cs` (Cemengal) | **One request at a time, as a job** ([0008](../decisions/0008-one-job-at-a-time.md)): another request while one runs gets `NVX_BUSY` at once; a job still running after 20 s answers `NVX_JOB_RUNNING` with its id. `CurrentToken` is the running job's cancellation token; `Report` takes a scan's progress; the last 20 jobs are kept. `Immediate` names the three job tools. |
| `Mapping/RpcMap.cs` | **The route table**: method name → controller method, each wrapped to add `meta` (timing). The names are [the RPC contract](../concepts/rpc-contract.md). |
| `Controllers/` | One per domain: `Model`, `Search`, `Selection`, `Clash`, `Jobs` (Cemengal: `get_server_status`, `get_job_status`, `cancel_job`), `Code` (Cemengal: `send_code_to_navisworks`), `View` (Cemengal: `take_screenshot`), plus `Export`, `Visibility` and `System`, which have no routes yet. They deserialize `params` into `Contracts/Queries.cs` types and call a backend with the job's token. |
| `Validation/Validators.cs` | `EnsureNonEmpty`, `EnsureLimit` (1–5000), `EnsureOffset`. |
| `Services/Backends/` | `BackendResolver` picks `ReflectionBackend` when the UI thread is initialized, else `FallbackBackend`; both implement `IWaabeNavisworksBackend`. `UiThread` marshals calls onto Navisworks' UI thread; `UiThread.PauseAsync` and `UiSlice` (Cemengal) let a scan hand it back every 100 ms, report progress, stop on cancel and stop if the document changes ([0008](../decisions/0008-one-job-at-a-time.md)). `FallbackBackend` holds most of the Navisworks API and COM code, including `RunSimpleClash` and `ResolveItemsByCanonicalIds`, which matches model ids from the model list before it scans items ([known gaps](../concepts/known-gaps.md)). A failure on the UI thread is not re-run off it. `ScreenCapture` (Cemengal) renders the 3D view (`View.GenerateImage`) or captures the main window (`PrintWindow`) as a PNG ([0010](../decisions/0010-send-code-and-screenshot.md)). |
| `Services/Code/` (Cemengal) | `CodeRunner` compiles a script with Roslyn (`Microsoft.CodeAnalysis.CSharp` 4.8.0) against every loaded assembly and runs it on the UI thread; `ScriptValue` turns its return value into bounded JSON ([0010](../decisions/0010-send-code-and-screenshot.md)). |
| `Services/Implementations/`, `Infrastructure/Caching`, `ErrorHandlingMiddleware`, `Telemetry/` | Domain services, response cache, error envelope, metrics for `meta`. |
| `Dialogs/PortConfigDialog.xaml` | A WPF port dialog (used by the standalone ribbon). |

The server talks to Navisworks through `Autodesk.Navisworks.Api`, the COM bridge (`ComApi`,
`Interop.ComApi`) and `Autodesk.Navisworks.Api.Clash`, all compile-only from NuGet
([0001](../decisions/0001-sdk-style-2027-build.md)). It references `CEM_NavisIAModeler_Shared` ([shared](shared.md)), through it CEM_CSharpExtended (`CemUserHome`, `DependencyResolver`), and Roslyn ([0010](../decisions/0010-send-code-and-screenshot.md)).
