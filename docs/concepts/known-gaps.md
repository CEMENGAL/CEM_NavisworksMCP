---
title: "Known gaps — what is missing, unverified or risky"
type: concept
updated: 2026-10-07
sources: [MCP_Server/CEM_NavisIAModeler/Services/MCPServer.cs, MCP_Server/CEM_NavisIAModeler/Services/Backends/FallbackBackend.cs, MCP_Server/CEM_NavisIAModeler/Plugins/MCPServerRegistrar.cs]
related: [../workflows/smoke-test.md, upstream-and-fork.md]
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

1. **Four tools not yet run live:** `get_property_distribution_by_category`,
   `list_properties_for_item`, `list_items_to_property`, `clear_selection`.
2. **No authentication.** Loopback only, no CORS, browser `Origin`s refused
   ([0005](../decisions/0005-no-cors-refuse-browser-origins.md)), but any local *process* can call
   `/rpc`, including `apply_selection` and `run_simple_clash`, which change the document. CEM_RevitMCP
   uses a local shared secret ([its 0022](../../../CEM_RevitMCP/docs/decisions/0022-local-shared-secret-handshake.md));
   port that before using this on shared machines.
3. **The id lookup scans the whole document, on the UI thread.** `ResolveItemsByCanonicalIds` (in
   `FallbackBackend`) resolves each canonical id by walking the whole model tree and keeps walking after
   a match. In the 72,888-item test document: `apply_selection` of one to three model roots took 22–31 s;
   `run_simple_clash` with two whole models spent 19.7 min resolving scope A and 69.5 min resolving
   scope B, with Navisworks unresponsive. That is why the clash fix (above) is not re-run yet. Fix: build
   an id → item map once per document and stop at the first match; then re-run the clash.
4. **Stale prebuilt artifacts.** `generated_MCP_Server/waabe_navi_mcp.bundle.zip` and
   `generated_MCP_Client/waabe-navisworks-mcp.dxt` are upstream's 2026 builds. Do not install them.
5. **No audit log.** CEM_RevitMCP routes every command through an audit log; this server only writes
   `LogHelper` lines to `CEM_NavisIAModeler_log.md`.
6. **Two upstream CS1998 warnings** (async methods without `await`, in `MCPServer.cs` and
   `FallbackBackend.cs`). Harmless; left as upstream wrote them.
7. **German texts.** Dialogs, error messages and logs are mostly German, as upstream wrote them.
8. **Standalone start-up registration error.** In the standalone bundle, `MCPServerRegistrar.OnLoaded`
   logs `UiThread.InitializeFromCurrentThread: Unerwarteter Context-Typ 'System.Threading.SynchronizationContext'`
   and registers nothing; the first **Server starten** click creates the service instead. The hosted
   path does not use the registrar.
9. **Manifest `base_url` repeats the port** (`http://127.0.0.1:8765:8765/`) in `GET /manifest`;
   upstream's `GetBaseUrl` appends the port to a host that already has it. The client does not use it.
