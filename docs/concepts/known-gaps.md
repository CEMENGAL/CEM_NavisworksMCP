---
title: "Known gaps — what is missing, unverified or risky"
type: concept
updated: 2026-10-21
sources: [MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs, MCP_Server/CEM_NavisIAModeler/Services/Backends/FallbackBackend.cs, MCP_Server/CEM_NavisIAModeler/Infrastructure/RpcJobs.cs, MCP_Server/CEM_NavisIAModeler/Plugins/MCPServerRegistrar.cs]
related: [../workflows/smoke-test.md, upstream-and-fork.md, ../decisions/0008-one-job-at-a-time.md, ../decisions/0009-local-access-token.md, ../decisions/0010-send-code-and-screenshot.md]
tags: [gaps, security, drift]
---

# Known gaps

Recorded at the fork (2026-10-07). Remove an item when it is fixed, and log it.

**Resolved on 2026-10-07:**
- *Never run in Navisworks 2027.* It ran, standalone and then hosted by the Cemengal ribbon
  ([smoke test](../workflows/smoke-test.md) results).
- *Port defaults disagreed* (server 8080, client 1234, and 8080 is CEM_RevitMCP's). Both are 8765, pinned
  by `default-port.test.mjs` ([0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)).
- *A separate "waabe" tab and a modal "server started" box.* The Cemengal tab's **AI Modeler** button
  hosts it, shows its state, and opens no dialog on success.
- *The 2027 clash port failed* with `test not found after AddCopy`: `TestsData.Value` is a snapshot,
  read before the add. It is now read again after the add; see item 3 for why it is not yet re-run.

**Resolved on 2026-10-21 ([0008](../decisions/0008-one-job-at-a-time.md)), after an agent's night of
"Navisworks is not answering":** requests queued on the UI thread behind a slow one (now one job at a
time, others refused at once with `NVX_BUSY`, long ones answered with a job id); a fixed 10 s token
that returned partial scans as complete and triggered an off-thread re-run of the call (now the job's
token, no re-run); scans that froze Navisworks and could not be stopped (now they pause every 100 ms
and stop on `cancel_job`); `list_items_to_property` ignoring its scope and listing the whole document
first; model ids found by scanning every item (now from the model list); clash model scopes that
tested one branch of the model; the clash result count reading a disposed snapshot (always 0); a
client with no request timeout. Confirmed in Navisworks the same day on the 1.4-million-item model ([smoke test](../workflows/smoke-test.md)).

**Checked on 2026-10-11, not a gap:** the C# request-layer review
([CEM_API#74](../sources/issues/CEM-EnriqueRuiz-CEM_API-74.md)) found no CEM_API request here, no route
string. (Since 2026-10-15 `CEM_NavisIAModeler_Shared` references CEM_CSharpExtended for `CemUserHome` only, [0007](../decisions/0007-per-user-home-no-writes-next-to-the-dll.md).) Every URL in the code is this repo's own MCP server on
`127.0.0.1`/`localhost` or the GitHub project link. A future CEM_API call goes through CEM_CSharpExtended's
`Api` (root [0023](../../../../docs/decisions/0023-one-shared-request-layer.md)); no guard test enforces that yet,
here or in the other C# consumers.

1. **Four tools not yet run live:** `get_property_distribution_by_category`,
   `list_properties_for_item`, `list_items_to_property`, `clear_selection`.
2. **Same-user programs can read the token.** Since [0009](../decisions/0009-local-access-token.md) every
   request needs the per-start token from the user's settings folder, which closes the server to other
   Windows users and to browsers ([0005](../decisions/0005-no-cors-refuse-browser-origins.md)). A program
   running as the same user can read it, and so call `send_code_to_navisworks`; it could already run code
   as that user.
3. **Item ids still need a scan of the whole document.** Since 0008 model ids resolve from the model
   list, and the scan pauses for Navisworks and can be cancelled, but an item's `p:` path hash still
   means walking every item and building each one's ancestor path (about 185 µs per item: 4.5 min for
   the 1,387,034-item federated model of 2026-10-21; GUIDs are cheaper, no hashing). An id → item
   index per document would fix it; [0008](../decisions/0008-one-job-at-a-time.md) records why not yet.
   Also: a clash test cannot pause while Navisworks computes it, and
   `get_property_distribution_by_category` has no scope, so on a large model it reads every property
   of every item (minutes, as a job).
4. **Stale prebuilt artifacts.** `generated_MCP_Server/waabe_navi_mcp.bundle.zip` and
   `generated_MCP_Client/waabe-navisworks-mcp.dxt` are upstream's 2026 builds. Do not install them.
5. **No audit log.** CEM_RevitMCP routes every command through an audit log; this server only writes
   `LogHelper` lines to `CEM_NavisIAModeler_log.md`, `[JOB]` lines included. The last 20 jobs are kept in
   memory only, and forgotten when the AI Modeler restarts.
6. **Two upstream CS1998 warnings** (async methods without `await`, in `MCPServer.cs` and
   `FallbackBackend.cs`). Harmless; left as upstream wrote them.
7. **German texts.** Dialogs, error messages and logs are mostly German, as upstream wrote them.
8. **Standalone start-up registration error.** In the standalone bundle, `MCPServerRegistrar.OnLoaded`
   logs `UiThread.InitializeFromCurrentThread: Unerwarteter Context-Typ 'System.Threading.SynchronizationContext'`
   and registers nothing; the first **Server starten** click creates the service instead. The hosted
   path does not use the registrar.
9. **Manifest `base_url` repeats the port** (`http://127.0.0.1:8765:8765/`) in `GET /manifest`;
   upstream's `GetBaseUrl` appends the port to a host that already has it. The client does not use it.
10. **The standalone bundle does not ship `CEM_CSharpExtended.dll`, nor Roslyn.** The `DeployNavisworksBundle` copy list in `CEM_NavisIAModeler_Ribbon.csproj` carries only the three DLLs, but the Shared project now needs CEM_CSharpExtended and its dependencies (SocketIO*, System.Text.Json, System.Memory...). The hosted bundle has them; a standalone install (no CEM_NavisworksAPI) fails to load `LogHelper`/`SettingsManager` until they are added. Since [0010](../decisions/0010-send-code-and-screenshot.md) the server also needs `Microsoft.CodeAnalysis*.dll` and their dependencies for `send_code_to_navisworks`.
11. **Roslyn's language folders deploy with the host bundle.** `cs`, `de`, `es`, `fr`, `it`, `ja`, `ko`, `pl`,
   `pt-BR`, `ru`, `tr`, `zh-Hans`, `zh-Hant` reach CEM_NavisworksAPI's output and its bundle (and the ACC menu,
   when published). Harmless; CEM_NavisworksAPI's `CEM_RibbonUI` would drop them with
   `<SatelliteResourceLanguages>en</SatelliteResourceLanguages>` (its own `en-US`/`es-ES` resources are not affected).
12. **Scripts cannot pause.** A `send_code_to_navisworks` script runs on the UI thread in one piece: a heavy
   one freezes Navisworks until it ends, and `cancel_job` stops it only if it checks `ct`. Each script also
   leaves a small compiled assembly in memory until Navisworks closes ([0010](../decisions/0010-send-code-and-screenshot.md)).
