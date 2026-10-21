---
title: "0008 — One request at a time, as a job; long scans pause for Navisworks"
type: decision
status: current
updated: 2026-10-21
sources:
  - MCP_Server/CEM_NavisIAModeler/Infrastructure/RpcJobs.cs
  - MCP_Server/CEM_NavisIAModeler/Infrastructure/RpcRouter.cs
  - MCP_Server/CEM_NavisIAModeler/Controllers/JobsController.cs
  - MCP_Server/CEM_NavisIAModeler/Services/Backends/UiSlice.cs
  - MCP_Server/CEM_NavisIAModeler/Services/Backends/UiThread.cs
  - MCP_Server/CEM_NavisIAModeler/Services/Backends/ReflectionBackend.cs
  - MCP_Server/CEM_NavisIAModeler/Services/Backends/FallbackBackend.cs
  - MCP_Client/server/navis-rpc.js
  - MCP_Client/server/index.js
  - MCP_Client/server/tests/job-protocol.test.mjs
  - MCP_Client/server/tests/job-server.test.mjs
related:
  - ../modules/rpc-server.md
  - ../modules/node-client.md
  - ../concepts/rpc-contract.md
  - ../concepts/known-gaps.md
  - 0004-minimal-divergence-from-upstream.md
tags: [jobs, ui-thread, timeouts, clash, performance]
---

# 0008 — One request at a time, as a job; long scans pause for Navisworks

## Context

On the night of 2026-10-20/21 an agent in Claude Desktop tried to run the SBEP clash matrix on
`STC01-CMGL-GA00-0000000-00001-FederatedModel.nwf` (135 models, 1,387,034 items). From its side,
"Navisworks stopped answering" for hours; in the morning a small pilot clash "blocked the plugin past
its 60 s reply window". The AI Modeler's log shows what happened:

| Time | What the log shows |
|---|---|
| 01:26:11 | `get_property_distribution_by_category` stopped after exactly 10 s at a model boundary and returned the models read so far as a complete, successful result |
| 01:26:28 | `list_items_to_property` for `01_02-RepresentationGroup`, scope `all`. `ResolveScopeItems` ignored the scope and listed all 1,387,034 items before reading any: **49 minutes**, Navisworks frozen |
| 01:27:32 | the agent's retry (its host gave up after 60 s), queued on the UI thread behind the first |
| 01:28–02:35 | four `get_units_and_tolerances` health checks (1 ms of work each) queued behind both |
| 02:15:35 | the first query hit its 10 s token at last; `ReflectionBackend` caught the cancel and **ran the whole query again off the UI thread**, beside the retry now running on it, both calling the Navisworks API |
| 03:01, 04:27 | the retry ended the same way; the four health checks ran at 04:27, their callers long gone (`HttpListenerException` writing to closed connections) |
| 08:48:59 | the pilot clash, ME00 vs GA0S of FutureExpedition by model id: two id lookups scanned all 1,387,034 items (257 s and 275 s); the clash itself took about one second |
| 08:57:53 | it reported **0 clashes**: the count read the test snapshot taken before the run (`ObjectDisposed`) |

So: every request runs on Navisworks' UI thread and upstream queued each behind the one before; a
fixed 10 s token stopped work only at coarse points and then caused a re-run; nothing told the agent
"busy, with what"; the client's `fetch` had no timeout; and three code paths (scope ignored, whole
document id scans, whole models copied item by item into clash selections) made ordinary calls take
minutes. Two clash bugs surfaced on the way: the result count above, and model scopes that tested
one branch of the model (below).

## Decision

**One request at a time, as a job** (`Infrastructure/RpcJobs.cs`, `RpcRouter.Dispatch`).
- A request starts a job only when none is running. Any other request is refused **at once** with
  `NVX_BUSY` and the running job (id, method, elapsed), instead of queueing on the UI thread.
- A job that outlasts `DispatchWaitMs` (20 s) answers `NVX_JOB_RUNNING` with its job id; it keeps
  running. The work is unchanged otherwise: a job that ends in time returns its usual reply.
- Three **immediate** tools run outside the rule and never wait on the UI thread longer than a short
  probe: `get_server_status` (busy or idle, the running job, whether Navisworks' UI thread answers a
  1.5 s probe, the document, the last five jobs), `get_job_status` (long-polls up to 25 s, returns a
  finished job's own reply) and `cancel_job`. The last 20 jobs are kept in memory.
- The controllers' work gets the **job's cancellation token** instead of a fixed 10 s one. A
  cancelled job ends `NVX_CANCELED`; scans no longer return a partial result as a complete one.
- `ReflectionBackend` **no longer re-runs** a call that failed on the UI thread off it.
- Stopping the AI Modeler cancels its running job.

**Long scans pause for Navisworks** (`Services/Backends/UiSlice.cs`, `UiThread.PauseAsync`).
Every scan over model items (the id lookup, `list_items_to_property`, the property distribution, the
category count) works in slices of 100 ms and then hands the UI thread back for 20 ms: Navisworks
repaints and takes input, `get_server_status` answers, the job reports its progress (`phase`,
`items_visited`), and cancellation takes effect. If the active document or its model count changed
during a pause, the scan stops (`DocumentChangedException`).

**The client ends every tool call within the agent host's timeout** (`MCP_Client/server/navis-rpc.js`).
Each HTTP request times out after 30 s; a tool call follows a running job with `get_job_status`
until its 45 s budget runs out, then answers (not as an error) with the job id and "do not send the
request again". Claude Desktop's MCP request timeout is 60 s. `job-protocol.test.mjs` checks that the
budgets nest: server waits (20 s, 25 s) < HTTP timeout (30 s) < tool budget (45 s) < 60 s. Unreachable
server and no answer get their own messages (how to start the AI Modeler; call `get_server_status`).

**The slow paths behind the night's calls are fixed.**
- `list_items_to_property` reads only the scope's models (a list of model ids or names, as for the
  other tools), streams their items instead of listing the document first, and returns at most
  `maxResults` items, **200 by default**, with `truncated` and a `message`.
- `get_element_count_by_category` accepts a list of models as its scope; upstream matched the whole
  comma-separated list as one model name.
- The id lookup matches **model ids against the model list** before scanning items (a model root's
  path hash is its own name). Tokens that are not ids at all start no scan.
- A clash scope that is a **model or other container goes into the selection itself**; Clash
  Detective tests everything under it. Upstream replaced it with the geometric children of the first
  container its breadth-first search met (one storey or branch), and for name scopes copied every
  descendant into the selection item by item.
- The clash result count reads the test **after** the run.
- The HTTP reply has no 2 MB JSON limit (the serializer's default), so a whole-model property
  distribution is not lost after minutes of work.

## Rejected

| Alternative | Why not |
|---|---|
| Keep queueing, with timeouts on each request | A timeout cannot stop work already running on the UI thread; the queue still grows with every retry, which is what froze Navisworks for three hours |
| Run the Navisworks API on a background thread | The API is not thread-safe: while the off-thread re-run and the UI-thread retry ran at once, one of the two whole-document scans returned 432,490 items instead of 1,387,034 |
| `Application.DoEvents()` inside the loops | Re-entrant: user commands and our own posted calls would run in the middle of a scan |
| Several read-only jobs at once | Interleaving on one thread buys nothing and makes cancellation and progress per job harder; one job and an immediate "busy" are predictable for the agent |
| MCP progress notifications to stretch the host's 60 s timeout | Whether a host resets its timeout on progress is up to the host; a job id the agent can wait on works in every host |
| Stop the id scan at the first match | `p:` path hashes are not unique among same-named siblings and upstream returns every match; the model-list pass covers the slow case that matters (model scopes) |
| Build an id index of the whole document once | About 4.5 min and 1.4 million items held in memory for each document; the model-list pass and pausing scans are enough for now |
| Leave `maxResults` unlimited | 1.4 million items would not fit any agent's context, nor the reply |

## Consequences

- A tool call can now answer "still running as job jN-xxxxxx"; the agent waits with `get_job_status`.
  The tool descriptions say so.
- `TestsRunTest` itself cannot pause: while a clash computes, Navisworks is busy and
  `get_server_status` reports `ui_thread_responsive: false`; `cancel_job` takes effect after it.
- Item-level `p:` ids still need a scan of the document; it pauses and can be cancelled
  ([known gaps](../concepts/known-gaps.md)).
- New upstream-code changes are marked `Cemengal … (docs/decisions/0008)` ([0004](0004-minimal-divergence-from-upstream.md)).
