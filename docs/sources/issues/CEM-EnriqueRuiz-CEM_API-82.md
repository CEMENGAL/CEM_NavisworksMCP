---
title: "CEM-EnriqueRuiz/CEM_API#82: the MCPs write nothing into ACC; here, the AI Modeler log in a self-cleaning local temp folder"
type: source
updated: 2026-10-25
sources: [https://github.com/CEM-EnriqueRuiz/CEM_API/issues/82, MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs, MCP_Server/CEM_NavisIAModeler/Services/SettingsManager.cs, MCP_Server/CEM_NavisIAModeler_Ribbon/CEM_NavisIAModeler_Ribbon.csproj, MCP_Client/server/tests/file-writes.test.mjs, MCP_Client/server/tests/no-dll-adjacent-writes.test.mjs]
related: [../../decisions/0011-logs-in-local-temp-nothing-under-acc.md, ../../decisions/0007-per-user-home-no-writes-next-to-the-dll.md, ../../decisions/0009-local-access-token.md, ../../decisions/0010-send-code-and-screenshot.md, ../../modules/shared.md, ../../modules/rpc-server.md, ../../workflows/test.md, ../../workflows/smoke-test.md, ../../concepts/known-gaps.md, ../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-82.md, ../../../../../docs/decisions/0031-nothing-writes-under-acc-logs-in-local-temp.md, ../../../../../docs/decisions/0028-shared-acc-menus-per-user-files.md]
tags: [issue, acc, log, settings, temp, guard, cem_csharpextended, cross-repo]
---

# CEM-EnriqueRuiz/CEM_API#82

**Issue:** https://github.com/CEM-EnriqueRuiz/CEM_API/issues/82, "Borrar logs in ACC de los MCP". It is filed in
CEM_API's tracker with an empty body; the cross-repo work is on the root
[feature page](../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-82.md). Implemented on 2026-10-24, committed
on 2026-10-25 after the user tested it. The decision is [0011](../../decisions/0011-logs-in-local-temp-nothing-under-acc.md),
under root [0031](../../../../../docs/decisions/0031-nothing-writes-under-acc-logs-in-local-temp.md).

## What was asked
The user, 2026-10-24 (translated): test the MCPs with the files in `CEM_Test\MCP`, and verify that no one writes
logs or other files into an ACC path, so that Desktop Connector does no massive sync. Any log goes to a local temp
folder that cleans itself, without accumulating. Fix what you find.

For this repo that meant:
- audit every file write of the AI Modeler and the Node client;
- move the log to a daily file in `%TEMP%\Cemengal\NavisworksMCP` through CEM_CSharpExtended's `CemTempFolder`;
- remove the settings temp fallback;
- find out where screenshots and scripts go;
- confirm that the client only reads the token;
- extend the guard test, red first;
- prove it live through the MCP, with a file watcher on ACC and the deployed add-in folders.

## What the audit found (2026-10-24)
- **The main log was not in `%TEMP%`.** It was `CEM_NavisIAModeler_log.md` in the roaming per-user home
  (`%APPDATA%\Autodesk\ApplicationPlugins\CEM_NavisworksAPI.bundle`, [0007](../../decisions/0007-per-user-home-no-writes-next-to-the-dll.md)):
  one Markdown table that never shrank, at 371 KB. The `%TEMP%` files were its two fallbacks:
  - `%TEMP%\CEM_NavisIAModeler_log.md`, when `CemUserHome` threw;
  - `%TEMP%\CEM_NavisIAModeler_fallback_log.md`, when an append failed.
- **The settings** fell back to `%TEMP%\CEM_NavisIAModeler_settings.xml` when `CemUserHome` threw.
- **The access token** is per-user state in `CemUserHome\Settings`. The client reads it with `readFileSync`; only
  the tests write one, in an `os.tmpdir()` folder they remove.
- **Screenshots and scripts** never reach the disk: a PNG in a `MemoryStream`, and Roslyn emitting into
  `MemoryStream`s.
- **The Node client** writes no file.
- **No ACC path** appears anywhere in the code, the projects or the scripts.

The full table is in [0011](../../decisions/0011-logs-in-local-temp-nothing-under-acc.md).

## Decisions and reasons
| Decision | Reason | Rejected |
|---|---|---|
| The log is `%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-yyyyMMdd.log`, through `CemTempFolder.AppendLog`; one plain line per event (`HH:mm:ss.fff [PROJECT] message`), no Markdown header | A log is not per-user state (0031 §5); the helper appends shared, never throws, and prunes the folder (7 days, then 50 MB) | Keep it in `CemUserHome` and trim it (the roaming profile would still carry it; trimming is the helper's job) |
| No fallback file: a line that cannot be written goes to the debugger output | If `%TEMP%\Cemengal` cannot be written, neither can `%TEMP%`; a second file that nothing prunes is what 0031 removes | A `%TEMP%` fallback log beside the daily one |
| Settings without `CemUserHome` stay in memory for the session (port 8765, not enabled); `GetSettingsFilePath()` is null | Settings are never temp (0031 §5): a pruned copy would silently lose the port | Settings in `%TEMP%`; letting `SettingsManager` throw (it breaks the host button and the port dialog) |
| The token stays in `CemUserHome` | Per-user state, not a log ([0009](../../decisions/0009-local-access-token.md)) | Moving it to temp |
| Screenshots and scripts stay in memory | Nothing reads a saved copy ([0010](../../decisions/0010-send-code-and-screenshot.md)); the coordinator confirmed on 2026-10-24 | `%TEMP%\Cemengal\NavisworksMCP\Screenshots` |
| No Node temp helper | The client writes no file; 0031 §6 asks for one only if it does | A helper with nothing to write |
| The standalone bundle copies `CEM_CSharpExtended.dll` | `LogHelper` and `SettingsManager` need it without the host bundle | Leaving it to known gap 10 |
| Guard `file-writes.test.mjs`: an inventory of the C# writers, not a list of forbidden calls | A new writer fails the test until it is classified; a forbidden-call list lets a new one pass silently | A per-file blacklist |

## The user's decisions (2026-10-25)
- Keep every pass-1 change.
- Only the MCPs were in question. Root 0031 now says it: the MCPs have no exception, and the add-ins keep their
  deliberate team files on ACC. This repo is an MCP, so the strict rule applies.
- The coordinator deleted the 148 old files in ACC's `CEM_RibbonUI\Logs`, `05-CEMAIModeler\Log`, `Screenshots` and
  `Recipes`. None of them came from this repo.
- The coordinator publishes the Navisworks menu to ACC later, with the user's OK. The fix reaches colleagues only
  with that publish; this repo never publishes.
- The doc drift reported in pass 1 stays reported (below).

## Files changed
- `MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs`: the log through `CemTempFolder`; the header and both
  fallback files gone; `GetCurrentLogPath()` names today's file.
- `MCP_Server/CEM_NavisIAModeler/Services/SettingsManager.cs`: in-memory settings instead of the temp fallback.
- `MCP_Server/CEM_NavisIAModeler_Ribbon/CEM_NavisIAModeler_Ribbon.csproj`: the standalone copy list ships
  `CEM_CSharpExtended.dll`.
- `MCP_Client/server/tests/file-writes.test.mjs` (new, 9 tests), and `no-dll-adjacent-writes.test.mjs` (the log
  through `CemTempFolder`).
- The dev skill's hard rules, and the wiki: [0011](../../decisions/0011-logs-in-local-temp-nothing-under-acc.md),
  [0007](../../decisions/0007-per-user-home-no-writes-next-to-the-dll.md), 0006, [shared](../../modules/shared.md),
  the [server](../../modules/rpc-server.md), [test](../../workflows/test.md), the
  [smoke test](../../workflows/smoke-test.md), [known gaps](../../concepts/known-gaps.md) (5, 10), the index (hard
  rule 9, a lookup row) and the log.

## Verified (2026-10-24)
- **Red first:** `file-writes.test.mjs` failed 6 of 9 on the old code (an unclassified writer, a hand-made settings
  path and a hand-made temp path, the log outside `CemTempFolder`, the settings fallback file, the missing DLL in the
  standalone list).
- **Mutation checks:** an ACC literal, a `File.WriteAllText` and an `Assembly.Location` path in `ViewController.cs`
  turned 3 tests red; an `appendFileSync` import in the client turned one red. Both were reverted.
- **`npm test`:** 51/51, run again after CEM_CSharpExtended's `UserTempPath` change. No code or test file changed after
  that run.
- **Build:** `Debug N27`, 0 errors. CEM_NavisworksAPI's deploy build compiled this code with the new helper, and that
  build is what ran live.
- **Settings harness:** a throwaway `net48` console compiled the real `SettingsManager.cs` against a `CemUserHome`
  that throws. The old code wrote `CEM_NavisIAModeler_settings.xml` into temp (red); the new one kept the port in
  memory and wrote only its log line (green).
- **Live, 17 MCP calls** through `index.js` over stdio, as Claude Desktop runs it, on copies of the three STC01
  drainage IFCs in `%TEMP%` (details in the [smoke test](../../workflows/smoke-test.md)): every call answered;
  `IfcPipeSegment` 560; two screenshots as in-memory PNGs; Roamer's `TEMP` is the user's, so the log is
  `…\Temp\Cemengal\NavisworksMCP\CEM_NavisIAModeler-20261024.log`. Only that file changed; nothing in the bundle or in
  `CEM_Test\MCP`.
- **Watcher** on `C:\DC\ACCDocs` and `%APPDATA%\Autodesk\ApplicationPlugins`: no event during the calls, and no ACC
  event since 21:13, before the session. At the AI Modeler's start only the token (2 events) and the settings file
  (4) changed, in `CEM_NavisworksAPI.bundle\Settings`, as 0031 §5 intends.

## Left open
- **Stale files** that nothing writes any more, left for the user: the 371 KB `CEM_NavisIAModeler_log.md` in
  `%APPDATA%\…\CEM_NavisworksAPI.bundle` and `%TEMP%\CEM_NavisIAModeler_fallback_log.md`.
- **Navisworks' `.nwc` cache** beside every native file it opens: a model opened from ACC gets one there. It is the
  host's behaviour, not this repo's code (root feature page, "Left open").
- **Doc drift, reported, not fixed:**
  - the [server](../../modules/rpc-server.md) page says the registrar auto-starts the server when the settings say
    so. In the hosted path the service is created on the first button press, so it never starts at Navisworks load;
  - the standalone deploy still creates an unused `CEM_NavisIAModeler.bundle\Settings` folder;
  - every log line is tagged `[SHARED]`: upstream's `GetCallingProject` sees LogHelper's own assembly;
  - for its owner: CEM_CSharpExtended's `CemUserHome` doc comment still says logs live under it (root 0028 was
    amended on 2026-10-25).
