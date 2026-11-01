---
title: "CEMENGAL/CEM_Workspace#5: the MCPs for the modelling agent; here, QA-10, logging that never throws"
type: source
updated: 2026-11-01
sources: [https://github.com/CEMENGAL/CEM_Workspace/issues/5, MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs, MCP_Client/server/tests/logging-never-throws.test.mjs]
related: [../../decisions/0011-logs-in-local-temp-nothing-under-acc.md, ../../modules/shared.md, ../../workflows/test.md, CEM-EnriqueRuiz-CEM_API-82.md]
tags: [issue, log, temp, junction, qa, cem_csharpextended, cross-repo]
---

# CEMENGAL/CEM_Workspace#5

**Issue:** https://github.com/CEMENGAL/CEM_Workspace/issues/5, the umbrella issue the user chose for the
2026-11-01 work on the MCPs for the modelling agent (review `2026-11-01-mcp-modeller`). The architect created it;
the work spans CEM_AutocadMCP, CEM_RevitMCP, CEM_CSharpExtended, this repo and the two reviewers. This repo's
part is one robustness fix, QA-10, under [0011](../../decisions/0011-logs-in-local-temp-nothing-under-acc.md).

## What was asked
The independent QA reviewer's recheck 2 found QA-10 (Medium), and the user approved fixing it:

- CEM_CSharpExtended's SEC-8 fix makes `CemTempFolder` refuse to write through a junction or link in
  `%TEMP%\Cemengal`. `LogFile`, `ArtifactFolder` and `NewArtifactFile` now throw `CemTempFolderUnsafeException`, an
  `IOException`; `AppendLog` still only answers false.
- In this repo `LogHelper.LogEvent` calls `AppendLog`. On false it calls `HandleLogError`, which called
  `GetCurrentLogPath` → `CemTempFolder.LogFile` with no guard. So with a planted junction every log call threw,
  startup included: `LogStartup` called `LogProjectStartup` outside its try.

The brief: logging must never throw, it degrades silently. Guard `GetCurrentLogPath` and `HandleLogError`, put
the startup banner inside a try, test first, build against the local CEM_CSharpExtended working tree. No deploy.

## Decisions and reasons
| Decision | Reason | Rejected |
|---|---|---|
| `GetCurrentLogPath()` catches `IOException` (the new exception included) and `ArgumentException` and answers null | It is the one place that calls `LogFile`; both are the exceptions `LogFile` documents. Null lets `HandleLogError` and `LogPathDebug` format it and carry on | Catching `CemTempFolderUnsafeException` alone (a future IO failure would throw again); catching every `Exception` (hides a real bug) |
| `HandleLogError` prints `(unavailable)` for a null path | The line still reaches the debugger output, as 0011 says, with no second file | A fallback file in `%TEMP%` (0011 removed those) |
| `LogStartup` runs `LogProjectStartup` in a try of its own | A failing banner must not break the AI Modeler's start | Moving it into the version try: its catch logs "Navisworks Version: Nicht verfügbar", which would mislead |
| A guard test of the source, in the Node suite | The C# side has no unit-test project; the other C# guards (`file-writes`, `job-server`) are read from the source the same way | A new C# test project for one method |

The coordinator approved this plan before the edit (2026-11-01).

## Files changed
- `MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs`: the guards above, marked `Cemengal`.
- `MCP_Client/server/tests/logging-never-throws.test.mjs` (new, 3 tests).
- The wiki: [0011](../../decisions/0011-logs-in-local-temp-nothing-under-acc.md), [shared](../../modules/shared.md),
  [test](../../workflows/test.md), the index and the log.

## Findings and their end state
| Finding | Severity | End state |
|---|---|---|
| QA-10: every log call threw on a junctioned temp folder | Medium | Fixed; the QA reviewer's check 3 confirmed it (0 blocking findings) and noted that no caller dereferences the null |

The reviewer's own tests for it: `suites/workspace/test_cemtempfolder_unsafe_consumers.py` (2, passing) and
`suites/CEM_NavisworksMCP/test_shared_builds.py` (1, passing).

## Verified (2026-11-01)
- **Red first:** `logging-never-throws.test.mjs` failed 2 of 3 on the old code (`LogFile` outside a try,
  `LogProjectStartup` outside a try).
- **`npm test`:** 54/54 after the fix.
- **Build:** `Debug N27`, 0 errors, against the local CEM_CSharpExtended working tree with SEC-8 (a
  `ProjectReference`).

## Left open
- Not run inside Navisworks with a real junction in `%TEMP%\Cemengal`; the change was not deployed.
