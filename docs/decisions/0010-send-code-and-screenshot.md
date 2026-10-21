---
title: "0010 — send_code_to_navisworks and take_screenshot"
type: decision
status: current
updated: 2026-10-21
sources:
  - MCP_Server/CEM_NavisIAModeler/Controllers/CodeController.cs
  - MCP_Server/CEM_NavisIAModeler/Services/Code/CodeRunner.cs
  - MCP_Server/CEM_NavisIAModeler/Services/Code/ScriptValue.cs
  - MCP_Server/CEM_NavisIAModeler/Controllers/ViewController.cs
  - MCP_Server/CEM_NavisIAModeler/Services/Backends/ScreenCapture.cs
  - MCP_Server/CEM_NavisIAModeler/CEM_NavisIAModeler.csproj
  - MCP_Client/server/index.js
  - MCP_Client/server/tests/code-and-screenshot.test.mjs
related:
  - 0008-one-job-at-a-time.md
  - 0009-local-access-token.md
  - ../modules/rpc-server.md
  - ../../../CEM_RevitMCP/docs/modules/send-code-to-revit.md
  - ../../../CEM_RevitMCP/docs/decisions/0023-send-code-scripts-not-persisted.md
tags: [tools, roslyn, screenshot, escape-hatch]
---

# 0010 — send_code_to_navisworks and take_screenshot

## Context

The ten upstream tools read models, select and run one simple clash. Anything else, such as reading
Clash Detective's results, hiding items or setting up search sets, had no tool. On the night of
2026-10-21 the agent ended up writing XML import files for the user to load by hand. CEM_RevitMCP solves
this with an escape hatch, `send_code_to_revit` ([its module page](../../../CEM_RevitMCP/docs/modules/send-code-to-revit.md)),
and lets the agent look with `take_screenshot`. The user asked for both here on 2026-10-21.

## Decision

**`send_code_to_navisworks`** (`CodeController`, `Services/Code/CodeRunner.cs`)
- The code is the body of `static object Execute(Document doc, string[] parameters, CancellationToken ct)`,
  with `System`, `System.Collections.Generic`, `System.Linq`, `System.Text`, `System.Threading`,
  `Autodesk.Navisworks.Api`, `.Clash`, `.DocumentParts` and the aliases `ComApi` and `ComApiBridge` in
  scope. Using directives at the top of the code are moved above the class; a `#line` directive maps
  compile errors to the code's own lines. Code that returns nothing returns null.
- Compiled in process with Roslyn (`Microsoft.CodeAnalysis.CSharp` 4.8.0, the version CEM_RevitMCP uses
  on net48) on the **job's thread**, so Roslyn's seconds never freeze Navisworks. It is then run on the
  **UI thread**, as a job ([0008](0008-one-job-at-a-time.md)) behind the token ([0009](0009-local-access-token.md)).
- References: every assembly loaded in Navisworks, the newest of each name, plus the `netstandard` facade.
  So a script reaches the Navisworks API, Clash Detective, COM and the Cemengal add-ins.
- CEM_CSharpExtended's `DependencyResolver` is installed before the first Roslyn type loads: Navisworks
  has no binding redirects for the add-in's dependencies.
- The reply: the returned value as bounded JSON, its type, the console output (100,000 characters at
  most), compile and run times, and the warnings. `ScriptValue` turns Navisworks objects into text (a
  ModelItem as name, class and GUID) and bounds depth (6), lists (1,000) and nodes (20,000). A plain
  serializer would walk a ModelItem's properties, `Descendants` included, through the whole model.
- Errors: `NVX_COMPILE` with line numbers, `NVX_SCRIPT` with the exception, the script's stack frames and
  its output so far, `NVX_CANCELED` when the script stops on `ct`.
- **Scripts are never persisted**: the log records their line count and timings only, as CEM_RevitMCP's
  [0023](../../../CEM_RevitMCP/docs/decisions/0023-send-code-scripts-not-persisted.md).
- No transaction mode: unlike Revit, the Navisworks API needs no transaction to change a document.

**`take_screenshot`** (`ViewController`, `Services/Backends/ScreenCapture.cs`)
- `mode: "view"` (default): `View.GenerateImage(ScenePlusOverlay, w, h, enableSectioning: true)` at the
  viewport's proportions. The 3D view as it is, without the user interface.
- `mode: "window"`: the Navisworks main window by `PrintWindow` with `PW_RENDERFULLCONTENT`, so the DirectX
  view is included even under other windows; `CopyFromScreen` if that fails. Panels and dialogs included.
- Scaled to `max_size` (default 1,600 px on the longest side, at most 4,096), a PNG in memory, returned as
  an MCP image. Nothing is written to disk ([0007](0007-per-user-home-no-writes-next-to-the-dll.md)).

## Rejected

| Alternative | Why not |
|---|---|
| CodeDom's `CSharpCodeProvider` | C# 5 only, through an external `csc.exe`; agents write modern C# |
| A separate AppDomain per script, to unload it | Navisworks API objects do not cross AppDomains; each script leaves a small assembly in memory instead, accepted |
| Serializing the returned object as it is | Walks Navisworks object graphs on the UI thread; see `ScriptValue` above |
| A `transactionMode` like `send_code_to_revit` | Not needed by the Navisworks API |
| Saving screenshots to a folder, as CEM_RevitMCP does | Nothing reads them; the image goes to the agent |
| Only `CopyFromScreen` for the window | It captures whatever covers Navisworks |

## Consequences

- A script cannot pause: a heavy one keeps Navisworks busy like a clash test. `get_server_status` still
  answers, and `cancel_job` stops it only if it checks `ct`. The tool description tells the agent this.
- Roslyn and its dependencies ship in the host's bundle. Its 13 language folders come with them unless
  CEM_NavisworksAPI's `CEM_RibbonUI` sets `SatelliteResourceLanguages`. The standalone bundle ships
  neither Roslyn nor CEM_CSharpExtended yet ([known gaps](../concepts/known-gaps.md) item 10).
- Repeated scripts are the signal to build a proper tool, as in CEM_RevitMCP.
