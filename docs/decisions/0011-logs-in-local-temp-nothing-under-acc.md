---
title: "0011 — Nothing under ACC; the log in a self-cleaning local temp folder, settings never in temp"
type: decision
status: current
updated: 2026-10-25
sources:
  - https://github.com/CEM-EnriqueRuiz/CEM_API/issues/82
  - ../../docs/decisions/0031-nothing-writes-under-acc-logs-in-local-temp.md
  - ../../CEM_CSharpExtended/CEM_CSharpExtended/Hosting/CemTempFolder.cs
  - MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs
  - MCP_Server/CEM_NavisIAModeler/Services/SettingsManager.cs
  - MCP_Server/CEM_NavisIAModeler_Ribbon/CEM_NavisIAModeler_Ribbon.csproj
  - MCP_Client/server/tests/file-writes.test.mjs
related:
  - 0007-per-user-home-no-writes-next-to-the-dll.md
  - 0009-local-access-token.md
  - 0010-send-code-and-screenshot.md
  - ../modules/shared.md
  - ../modules/rpc-server.md
  - ../workflows/test.md
  - ../sources/issues/CEM-EnriqueRuiz-CEM_API-82.md
  - ../../../../docs/decisions/0028-shared-acc-menus-per-user-files.md
tags: [acc, log, settings, temp, deploy, guard]
---

# 0011 — Nothing under ACC; the log in local temp; settings never in temp

## Context

On 2026-10-24 the user asked that nothing of ours writes logs or other files under an ACC path, which
Desktop Connector syncs to every colleague, and that logs go to a local temp folder that cleans itself
([CEM_API#82](../sources/issues/CEM-EnriqueRuiz-CEM_API-82.md)). On 2026-10-25 the user narrowed it to the MCPs,
which have no exception; this repo is one.
Root decision [0031](../../../../docs/decisions/0031-nothing-writes-under-acc-logs-in-local-temp.md) sets
the contract for every repo; CEM_CSharpExtended's `Hosting.CemTempFolder` implements it for C#.

The audit of every file write in this repo that day:

| Writer | What | Where, before | Verdict |
|---|---|---|---|
| `LogHelper` (Shared) | log | `%APPDATA%\Autodesk\ApplicationPlugins\CEM_NavisworksAPI.bundle\CEM_NavisIAModeler_log.md` (`CemUserHome.NavisworksFile`, [0007](0007-per-user-home-no-writes-next-to-the-dll.md)): one Markdown table that never shrank (371 KB), in the roaming profile; `%TEMP%\CEM_NavisIAModeler_log.md` when `CemUserHome` threw; `%TEMP%\CEM_NavisIAModeler_fallback_log.md` when an append failed (two unshared `File.AppendAllLines` colliding) | breaks 0031 §2 and §4: moved |
| `SettingsManager` | per-user settings | `CemUserHome\Settings\CEM_NavisIAModeler_settings.xml`; `%TEMP%\CEM_NavisIAModeler_settings.xml` when `CemUserHome` threw | the fallback breaks 0031 §5: removed |
| `AccessToken` | per-user state | `CemUserHome\Settings\CEM_NavisIAModeler.token`, at start; deleted at stop ([0009](0009-local-access-token.md)) | stays; the client only reads it |
| `take_screenshot` (`ScreenCapture`) | image | nothing: a PNG in a `MemoryStream`, returned as MCP image content ([0010](0010-send-code-and-screenshot.md)) | stays |
| `send_code_to_navisworks` (`CodeRunner`) | script | nothing: Roslyn emits into `MemoryStream`s, `Assembly.Load(bytes)`; the log gets the line count and timings only | stays |
| The Node client | — | no file at all; its stderr goes to the agent host's own log | stays |
| The standalone ribbon's loaders | reads | `Buttons.xml`, icons beside the DLL | reads are allowed |

No source, project or script of the repo names an ACC path.

## Decision

- **The log** is `%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-yyyyMMdd.log`, one file per local day,
  written by `CemTempFolder.AppendLog("NavisworksMCP", "CEM_NavisIAModeler", line)`: UTF-8, appended shared
  and thread-safe, never throwing. The folder is pruned on the first write of each process (7 days, then the
  oldest until 50 MB). A line is `HH:mm:ss.fff [PROJECT] message`; the Markdown header is gone. When a line
  cannot be written it goes to the debugger output only: no second file. `LogHelper`'s public methods are
  unchanged; `GetCurrentLogPath()` names today's file.
- **Settings without a per-user home stay in memory.** When `CemUserHome` cannot give the settings file,
  `SettingsManager` keeps the port (8765) and the enabled state for the session; a save changes the
  in-memory value and is logged; `GetSettingsFilePath()` is null. The next start tries `CemUserHome` again.
  In that state the access token cannot be written either, so the AI Modeler does not start
  ([0009](0009-local-access-token.md)); the fallback only keeps the button and the port dialog working.
- **The token stays in `CemUserHome`**: per-user state, not a log (0031 §5).
- **Screenshots and scripts stay in memory.** Writing nothing is better than a temp copy that nothing reads.
- **The standalone bundle ships `CEM_CSharpExtended.dll`** beside the AI Modeler (`CemUserHome`,
  `CemTempFolder`). The hosted bundle is CEM_NavisworksAPI's and already ships it.
- **Guard:** `file-writes.test.mjs` fails when
  - a shipped source (C#, csproj, props, targets, the client's JS, `manifest.json`) names an `ACCDocs` or
    `C:\DC\` path;
  - a C# file other than `SettingsManager` and `AccessToken` calls a file-system write API (or saves or emits
    to anything but a `MemoryStream`): a new writer must be classified first;
  - those writers stop resolving their path through `CemUserHome`, or derive one from the assembly folder,
    the temp folder or a special folder;
  - any C# file builds a temp path itself (`GetTempPath`, `%TEMP%`): temp only through `CemTempFolder`;
  - the log stops going through `CemTempFolder`, or settings fall back to a file;
  - the Node client imports an `fs` write function;
  - the standalone copy list loses `CEM_CSharpExtended.dll`.

## Rejected

| Alternative | Why not |
|---|---|
| Keep the log in `CemUserHome` and trim it | A log is not per-user state (0031 §5); the roaming profile would still carry it, and trimming is the helper's job |
| Keep a `%TEMP%` fallback log beside the daily one | If `%TEMP%\Cemengal` cannot be written, neither can `%TEMP%`; a second file never pruned is what 0031 removes |
| Settings in `%TEMP%` when `CemUserHome` fails | 0031 §5: settings are never temp; a temp copy would be pruned and silently lose the port |
| No fallback: let `SettingsManager` throw | It is read by the host button and the port dialog; a throw there breaks the ribbon for a state that the token check already reports |
| Save screenshots to `%TEMP%\Cemengal\NavisworksMCP\Screenshots` | Nothing reads them ([0010](0010-send-code-and-screenshot.md)); the coordinator confirmed on 2026-10-24 |
| A Node temp helper for the client | The client writes no file; 0031 §6 asks for one only if it does |
| A list of forbidden write calls per file instead of the inventory | A new writer would pass silently; the inventory makes each new write a decision |

## Consequences

- The old `%APPDATA%\…\CEM_NavisworksAPI.bundle\CEM_NavisIAModeler_log.md` (371 KB) and
  `%TEMP%\CEM_NavisIAModeler_fallback_log.md` stay on disk for the user to delete; nothing writes them any
  more.
- The log is per machine and lasts at most 7 days, shared in its 50 MB with nothing else of this app.
- Writes the user still sees under `%APPDATA%\Autodesk\ApplicationPlugins\CEM_NavisworksAPI.bundle\Settings`
  are the token at each start and stop, and the settings when they change. On the development machine that
  folder is also the dev bundle; on the ACC menu the DLLs are elsewhere ([root 0028](../../../../docs/decisions/0028-shared-acc-menus-per-user-files.md)).
- The fix reaches colleagues only when the Navisworks menu is published to ACC. The user accepted the change on
  2026-10-25 and left that publish to the coordinator, with the user's OK; this repo never publishes.
- Verified live on 2026-10-24 ([smoke test](../workflows/smoke-test.md)): 17 MCP calls changed only the daily
  log; a file watcher saw nothing under ACC or beside the deployed DLLs, and Roamer's `TEMP` is the user's.
