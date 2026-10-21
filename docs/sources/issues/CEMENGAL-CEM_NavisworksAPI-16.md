---
title: "CEMENGAL/CEM_NavisworksAPI#16: Mejorar NavisworksMCP (no more freezes; token, code and screenshots)"
type: source
updated: 2026-10-21
sources: [https://github.com/CEMENGAL/CEM_NavisworksAPI/issues/16]
related: [../../decisions/0008-one-job-at-a-time.md, ../../decisions/0009-local-access-token.md, ../../decisions/0010-send-code-and-screenshot.md, ../../concepts/known-gaps.md, ../../workflows/smoke-test.md]
tags: [issue, jobs, ui-thread, token, roslyn, screenshot, clash]
---

# CEMENGAL/CEM_NavisworksAPI#16

**Issue:** https://github.com/CEMENGAL/CEM_NavisworksAPI/issues/16, "Mejorar NavisworksMCP" (enhancement,
opened 2026-10-21 with no body). Filed in CEM_NavisworksAPI, whose ribbon hosts the AI Modeler; the work is
in this repo only. CEM_NavisworksAPI was rebuilt to deploy it, with no source change.

## What the user asked (2026-10-20/21)
1. Register the MCP in Claude Desktop and make it run (done by its config file; see
   [connect an agent](../../workflows/connect-an-agent.md)).
2. After an agent's overnight attempt at the SBEP clash matrix on the STC01 federated model ended with
   "Navisworks is not answering" and a pilot clash "blocking the plugin past its 60 s window": "could it be
   improved to avoid that communication issues?", then "fix the issues to run MCP without that freezing".
3. "this MCP does not have the main and basic tool to be able to send custom code … something similar to
   RevitMCP … able also to make screenshots", and "do it now".

## What the log showed
The AI Modeler's log for the night: requests queued on the UI thread behind a 49-minute
`list_items_to_property` that ignored its scope; a 10 s token that returned partial scans as complete and
re-ran a cancelled call off the UI thread; model ids found by scanning all 1,387,034 items (2 × 4.5 min for
the pilot clash, whose clash itself took about a second); the clash count read from a disposed snapshot (0
instead of 62). Details in [0008](../../decisions/0008-one-job-at-a-time.md).

## Decisions and reasons
| Decision | Reason | Rejected |
|---|---|---|
| One request at a time, as a job; `NVX_BUSY` at once for any other; `NVX_JOB_RUNNING` with the job id after 20 s; `get_server_status`, `get_job_status`, `cancel_job` ([0008](../../decisions/0008-one-job-at-a-time.md)) | The queue on the UI thread is what turned one slow call into three hours of silence | Per-request timeouts on the queue (cannot stop running work); several read-only jobs at once |
| The job's cancellation token instead of a fixed 10 s; no re-run off the UI thread | The 10 s token produced silent partial results and the off-thread re-run | Running the Navisworks API on a background thread (not thread-safe, seen returning a wrong item count) |
| Scans pause every 100 ms for 20 ms (`UiSlice`) | Navisworks stays usable and `cancel_job` takes effect within a second | `Application.DoEvents()` (re-entrant) |
| The client ends each tool call within 45 s, under the host's 60 s; HTTP timeout 30 s | The agent always gets an answer: the result or a job id | MCP progress notifications (host-dependent) |
| `list_items_to_property` honours its scope, streams, caps at 200; counts accept model lists | The 49-minute call listed the whole document first | Unlimited results |
| Model ids from the model list; clash model scopes as whole models; the count read after the run | 9 min → 2.3 s for the pilot; it reported 0 of 62 clashes; model scopes tested one branch | Stop-at-first-match id scan (ambiguous `p:` hashes); a whole-document id index (memory, time) |
| A per-start access token in the user's settings folder on every request but `/health` ([0009](../../decisions/0009-local-access-token.md)) | Running code must not be open to other users of the machine; closes known gap 2 | Windows auth on the listener (no Node support); named pipes; a fixed secret |
| `send_code_to_navisworks` with Roslyn 4.8.0, compiled off and run on the UI thread, bounded results, scripts never persisted; `take_screenshot` of the view or the window ([0010](../../decisions/0010-send-code-and-screenshot.md)) | The escape hatch and the eyes CEM_RevitMCP has | CodeDom (C# 5); a separate AppDomain; saving screenshots to disk |

## Verified
- `npm test` 42/42 (four new files: job protocol, job server, access token, code and screenshot; the
  contract test also reads `navis-rpc.js`); solution builds `Debug N27` with 0 warnings; CEM_NavisworksAPI builds
  with Roslyn in its output; the code template compiled against the 2027 API outside Navisworks.
- Live on the STC01 federated model (135 models, 1,387,034 items), recorded in the
  [smoke test](../../workflows/smoke-test.md): the token checks; code (first compile 6.5 s, then 0.5 s;
  compile and runtime errors with line numbers); both screenshots; a whole-model scan answering `NVX_BUSY`
  to others, reporting progress with Navisworks responsive, cancelled in 96 ms; a model-scoped list in
  0.1 s; a model-id selection in 0.2 s; the pilot clash in 2.3 s with 62 results, as Clash Detective counts.
  The machine ran out of memory once during the run (Navisworks, AutoCAD and others), not the AI Modeler.

## Open
- [Known gaps](../../concepts/known-gaps.md): item ids still scan the document (3); scripts cannot pause (12);
  Roslyn's 13 language folders ship with the host bundle until CEM_NavisworksAPI's `CEM_RibbonUI` sets
  `SatelliteResourceLanguages` (11); the standalone bundle lacks CEM_CSharpExtended and Roslyn (10).
