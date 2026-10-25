---
title: "Smoke test — the AI Modeler and its tools in Navisworks"
type: workflow
updated: 2026-10-24
sources: [MCP_Server/CEM_NavisIAModeler/Core/MCPServiceConnection.cs, MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs, ../../CEM_NavisworksAPI/CEM_RibbonUI/CemRibbon.cs]
related: [build-and-deploy.md, connect-an-agent.md, ../concepts/known-gaps.md, ../decisions/0008-one-job-at-a-time.md, ../decisions/0009-local-access-token.md, ../decisions/0010-send-code-and-screenshot.md, ../decisions/0011-logs-in-local-temp-nothing-under-acc.md]
tags: [verify, manual]
---

# Smoke test

The bar for anything that runs inside Navisworks.

1. Close Navisworks Manage 2027, then build **CEM_NavisworksAPI** (Cemengal checkout) or this solution
   (standalone) ([build and deploy](build-and-deploy.md)); check the output names the bundle folder.
2. Start Navisworks and open a model (`../../CEM_Test/CEM_NavisworksAPI/Prueba.nwf`, three IFC models).
   The **Cemengal** tab should show three buttons (the AI Modeler under **AI**, then the connection and IFC Comparator ones; Hello World was deleted on 2026-10-15), and there should be
   no other MCP tab. If not, read today's `%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-yyyyMMdd.log`
   ([0011](../decisions/0011-logs-in-local-temp-nothing-under-acc.md); `CEM_NavisIAModeler_log.md` in the bundle
   folder before 2026-10-24).
3. Press **AI Modeler**: no dialog; the button stays pressed and reads `AI Modeler (8765)`.
4. From a terminal:
   ```powershell
   $token = Get-Content "$env:APPDATA\Autodesk\ApplicationPlugins\CEM_NavisworksAPI.bundle\Settings\CEM_NavisIAModeler.token"
   curl.exe http://127.0.0.1:8765/health                                   # OK, no token needed
   curl.exe http://127.0.0.1:8765/manifest -i                              # 401 without the token
   curl.exe http://127.0.0.1:8765/manifest -H "Authorization: Bearer $token"   # the fifteen methods
   curl.exe -X POST http://127.0.0.1:8765/rpc -H "Authorization: Bearer $token" -H "Content-Type: application/json" -d '{\"id\":\"1\",\"method\":\"get_model_overview\",\"params\":{}}'
   curl.exe -X POST http://127.0.0.1:8765/rpc -H "Origin: https://example.com" -d '{}' -i   # 403
   ```
   The token file exists while the AI Modeler runs and is deleted when it stops
   ([0009](../decisions/0009-local-access-token.md)).
5. Call the tools through the client ([connect an agent](connect-an-agent.md)). Item ids (not model
   ids) still scan the whole document, pausing ([known gaps](../concepts/known-gaps.md) item 3).
6. **Jobs** ([0008](../decisions/0008-one-job-at-a-time.md)), on a large model (the STC01 federated
   model, 1.4 million items):
   - Start a long call (`get_property_distribution_by_category`, or `list_items_to_property` with
     scope `all`). After about 45 s the tool answers "still running as job …", not an error.
   - Meanwhile Navisworks stays usable (orbit, select); `get_server_status` answers at once with
     `busy: true`, the job, its `items_visited` growing and `ui_thread_responsive: true`; any other
     tool is refused at once with `NVX_BUSY`.
   - `cancel_job`: the job ends `cancelled` within a second (`get_job_status`).
   - `run_simple_clash` with two model `canonical_id`s from `get_model_overview`: the scopes resolve
     in about a second, `results` matches the test's count in Clash Detective, and the test covers
     the whole models.
   - `list_items_to_property` with a model scope: only that model is read; at most 200 items, with
     `truncated`.
7. **Code and screenshots** ([0010](../decisions/0010-send-code-and-screenshot.md)):
   - `send_code_to_navisworks` with `return new { models = doc.Models.Count, title = doc.Title };`: the
     values come back; the first call takes a few seconds to compile, Navisworks stays usable meanwhile.
   - Code with a typo: `NVX_COMPILE` with the line number in the code. Code that throws: `NVX_SCRIPT`.
   - `take_screenshot` (view) and `take_screenshot` with `mode: "window"`: images of the 3D view and of
     the whole window.
8. Press **AI Modeler** again: the label returns to `AI Modeler`, `/health` stops answering and the token
   file is gone.
9. **Files** ([0011](../decisions/0011-logs-in-local-temp-nothing-under-acc.md)): the session's log lines are in
   today's `%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-yyyyMMdd.log`; nothing else appeared there (no
   screenshot, no script), nothing beside the deployed DLLs (`Contents\v24`) and nothing under `C:\DC\ACCDocs`.
   Under `%APPDATA%\Autodesk\ApplicationPlugins\CEM_NavisworksAPI.bundle\Settings` only the token (start, stop)
   and the settings file (start, stop) change. Open test models from a local copy: Navisworks itself writes a
   `.nwc` cache beside each file it opens.
10. Record the Navisworks build, the date and the results below and in the change's source page.

## Results

**2026-10-07, first run (upstream names, standalone bundle).** Navisworks Manage 2027 24.0.1453.24
(Spanish UI, installed on `D:`), `Prueba.nwf` (684, 55,042 and 17,162 items). Driven from the
architect's shell through UI Automation and `MCP_Client/server/index.js` over stdio.

| Step | Result |
|---|---|
| Bundle loads, ribbon | a **waabe** tab with **MCP Info** and **Server starten** |
| Start | a port dialog (default 8080), then a modal "MCP Server erfolgreich gestartet!" box, which stayed open behind the later work until the user closed it; the server listened on 8765 (owned by `http.sys`, PID 4) |
| `/health`, `/manifest`, browser `Origin` | `OK`; ten routes (`base_url` repeats the port); `Origin` → **403**, no `Access-Control-*` header |
| `initialize`, `tools/list` | `waabe-navisworks-mcp` 1.0.0, ten tools |
| `get_model_overview`, `get_units_and_tolerances` | three models with item counts; mm, m², m³, tolerance 0.001 |
| `get_element_count_by_category` | `IfcBeam` 4,255, `IfcColumn` 864; without `category` → MCP error -32602 (by design) |
| `apply_selection` (three model roots), snapshot | applied in 31 s; count 3 |
| `run_simple_clash` (model 1 vs model 3) | id lookups of 19.7 and 69.5 minutes on the UI thread, then `test not found after AddCopy` (a 2027-port bug, since fixed); the client had timed out |

**2026-10-08, after the rename and hosting ([0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)).**
Same machine and document.

| Step | Result |
|---|---|
| Ribbon | only the **Cemengal** tab, with **Hello World** and **AI Modeler**; no waabe tab (Hello World no longer exists since 2026-10-15) |
| Press **AI Modeler** | no dialog; label `AI Modeler (8765)`; `/health` OK; settings and log in `CEM_NavisworksAPI.bundle` |
| Client with **no port setting** | connected on 8765; reports `cem-navis-aimodeler` |
| `get_units_and_tolerances`, `get_model_overview` | as before |
| `apply_selection` (one model root), snapshot | 22 s; count 1. CEM_NavisworksAPI's Hello World then showed `Selected items: 1` |
| Press **AI Modeler** twice | stopped (label `AI Modeler`, `/health` silent), then started again (`AI Modeler (8765)`) |

**2026-10-21, jobs, token, code and screenshots ([0008](../decisions/0008-one-job-at-a-time.md)–[0010](../decisions/0010-send-code-and-screenshot.md)).**
Same machine; the STC01 federated model (135 models, 1,387,034 items), opened from ACC; the AI Modeler
pressed through UI Automation (the ribbon exposes the button as a plain element, so a mouse click on it);
driven through `index.js` over stdio. The run was stopped after about a minute because the machine ran out
of memory (0.6 GB of 19.8 GB free: Navisworks 2.6 GB, AutoCAD, Desktop Connector and others), not by the
AI Modeler.

| Step | Result |
|---|---|
| Token | `/health` 200 without it; `/manifest` 401 without, 200 with it (15 routes); `/rpc` with a wrong token 401; `Origin` 403 |
| `get_server_status` (idle) | `busy: false`, `ui_thread_responsive: true` (3 ms), title and 135 models |
| `get_model_overview`, `get_units_and_tolerances` | 0.3 s, 135 models; mm |
| `send_code_to_navisworks` | values, a ModelItem as text and console output back; first compile 6.5 s (Roslyn cold), then 0.5 s; run 2–3 ms. A typo: `NVX_COMPILE`, `line 2, col 12`. A throw: `NVX_SCRIPT` with `code:línea 2` and the output printed before |
| `take_screenshot` | view 1200 × 780 in 2.3 s; window 1200 × 643 in 0.2 s, ribbon and panels included |
| Long job: `get_property_distribution_by_category` | a second request 4 s in: `NVX_BUSY` at once, naming the job; status every 5 s: `items_visited` 17,685 → 59,512 → 182,216, `ui_thread_responsive: true` (0–64 ms); Windows saw the window responding throughout (checks of 1–104 ms) |
| `cancel_job` | `cancelled` within 96 ms (194,846 items read), `NVX_CANCELED`; the next request ran at once |
| `list_items_to_property`, scope ME00 FutureExpedition (3,198 items), `maxResults` 5 | 0.1 s; 5 items, `truncated: true` |
| `apply_selection` by that model's id, snapshot, `clear_selection` | 0.2 s (22 s to 4.5 min before); 1 item; cleared |
| `run_simple_clash` ME00 vs GA0S FutureExpedition (the morning's pilot) | **2.3 s, 62 results** (9 min and 0 results that morning); both selections are the whole model files |
| The same tests read back by `send_code_to_navisworks` | the morning's `PILOT_…` test and the new ones: 62 results each. The first script used `Selection.SelectedItems`, which 2027 lacks: `NVX_COMPILE` with both lines, fixed with `ExplicitSelection` |

The first run kept going after Claude Code reported it stopped, so two `MCP smoke …` tests are in the
document. Not run yet: the four tools in [known gaps](../concepts/known-gaps.md) item 1.

**2026-10-24, files ([0011](../decisions/0011-logs-in-local-temp-nothing-under-acc.md)).** Same machine; the RDP
session was not rendering, so no ribbon click: a throwaway starter plugin (in the agent's scratchpad, never
deployed) called `MCPServiceConnection.ToggleAsync`, loaded with
`Roamer.exe -AddPluginAssembly <dll> -ExecuteAddInPlugin "CemAIModelerStarter.CEMT"`. The three STC01 drainage
IFCs of `CEM_Test\MCP` were opened from copies in `%TEMP%` by `send_code_to_navisworks`
(`TryOpenFile`, `TryAppendFile`). Driven through `index.js` over stdio, as Claude Desktop runs it, from a Python
`mcp` client. A file watcher recorded `C:\DC\ACCDocs` and `%APPDATA%\Autodesk\ApplicationPlugins`.

| Step | Result |
|---|---|
| Start | log lines in `%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-20261024.log` from the first one (`[UIINIT] ctx=WindowsFormsSynchronizationContext`); watcher: the token (2 events) and the settings file (4) in `CEM_NavisworksAPI.bundle\Settings`, nothing in `Contents\v24` |
| `send_code_to_navisworks` (open the copies) | three models; compiled 4.6 s, ran 17.6 s |
| Overview, units, `get_element_count_by_category` | 3 models; mm; `IfcPipeSegment` 560 |
| `list_items_to_property` | `Elemento` / `Nombre` (the Spanish UI's names; `Item` / `Name` found 0), scoped, `valueFilter` `Pipe`: 3 items |
| `apply_selection`, snapshot, `list_properties_for_item`, `clear_selection` | 1 model selected; the properties of a model root and of `Pipe - (275) 200 PVC DR-35` (`IfcPipeSegment`) |
| `take_screenshot` view and window | PNGs of 800 × 337 and 800 × 323, even on the non-rendering session |
| `send_code_to_navisworks` (harmless) | Roamer's `TEMP` is the user's (`C:\Users\…\AppData\Local\Temp`, no per-process folder); `LogHelper.GetCurrentLogPath()` is today's file |
| Files after 17 calls | only the log changed (223 lines, 21 KB, the only file in the folder); nothing in the bundle, nothing in `CEM_Test\MCP`; Navisworks wrote a `.nwc` cache beside each opened copy |
| Watcher | no event during the calls; no `ACCDocs` event since 21:13, before the session |
