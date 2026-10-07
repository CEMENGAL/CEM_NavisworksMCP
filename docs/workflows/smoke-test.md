---
title: "Smoke test — the AI Modeler and its tools in Navisworks"
type: workflow
updated: 2026-10-08
sources: [MCP_Server/CEM_NavisIAModeler/Core/MCPServiceConnection.cs, MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs, ../../CEM_NavisworksAPI/CEM_RibbonUI/CemRibbon.cs]
related: [build-and-deploy.md, connect-an-agent.md, ../concepts/known-gaps.md]
tags: [verify, manual]
---

# Smoke test

The bar for anything that runs inside Navisworks.

1. Close Navisworks Manage 2027, then build **CEM_NavisworksAPI** (Cemengal checkout) or this solution
   (standalone) ([build and deploy](build-and-deploy.md)); check the output names the bundle folder.
2. Start Navisworks and open a model (`../../CEM_Test/CEM_NavisworksAPI/Prueba.nwf`, three IFC models).
   The **Cemengal** tab should show **General → Hello World** and **AI → AI Modeler**, and there should be
   no other MCP tab. If not, read `CEM_NavisIAModeler_log.md` in the bundle folder.
3. Press **AI Modeler**: no dialog; the button stays pressed and reads `AI Modeler (8765)`.
4. From a terminal:
   ```powershell
   curl.exe http://127.0.0.1:8765/health                                   # OK
   curl.exe http://127.0.0.1:8765/manifest                                 # the ten methods
   curl.exe -X POST http://127.0.0.1:8765/rpc -H "Content-Type: application/json" -d '{\"id\":\"1\",\"method\":\"get_model_overview\",\"params\":{}}'
   curl.exe -X POST http://127.0.0.1:8765/rpc -H "Origin: https://example.com" -d '{}' -i   # 403
   ```
5. Call the tools through the client ([connect an agent](connect-an-agent.md)). Use small scopes for
   `apply_selection` and `run_simple_clash`: id lookups scan the whole document on the UI thread
   ([known gaps](../concepts/known-gaps.md) item 3).
6. Press **AI Modeler** again: the label returns to `AI Modeler` and `/health` stops answering.
7. Record the Navisworks build, the date and the results below and in the change's source page.

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
| Ribbon | only the **Cemengal** tab, with **Hello World** and **AI Modeler**; no waabe tab |
| Press **AI Modeler** | no dialog; label `AI Modeler (8765)`; `/health` OK; settings and log in `CEM_NavisworksAPI.bundle` |
| Client with **no port setting** | connected on 8765; reports `cem-navis-aimodeler` |
| `get_units_and_tolerances`, `get_model_overview` | as before |
| `apply_selection` (one model root), snapshot | 22 s; count 1. CEM_NavisworksAPI's Hello World then showed `Selected items: 1` |
| Press **AI Modeler** twice | stopped (label `AI Modeler`, `/health` silent), then started again (`AI Modeler (8765)`) |

Not run yet: the four tools in [known gaps](../concepts/known-gaps.md) item 1, and the clash after its fix.
