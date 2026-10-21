---
title: "CEM_NavisworksMCP — overview"
type: overview
updated: 2026-10-21
sources: [README.md, MCP_Server/CEM_NavisworksMCP.sln, MCP_Client/server/index.js, MCP_Client/manifest.json]
related: [index.md, concepts/upstream-and-fork.md, concepts/rpc-contract.md, ../../../docs/repos/CEM_NavisworksMCP.md]
tags: [overview, mcp, navisworks]
---

# CEM_NavisworksMCP overview

An MCP bridge that lets an AI agent query and drive **Autodesk Navisworks Manage 2027**. Forked on
2026-10-07 from `kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026` (MIT), a proof of concept by
WAABE written in September 2025 for Navisworks 2026, and ported to 2027
([upstream and fork](concepts/upstream-and-fork.md)).

## Parts

```
MCP_Server/                      CEM_NavisworksMCP.sln (net48, "Debug N27")
  CEM_NavisIAModeler/            the AI Modeler: HTTP listener + JSON-RPC router, controllers, Navisworks
                                 backends, Core/MCPServiceConnection (what CEM_NavisworksAPI's ribbon calls)
  CEM_NavisIAModeler_Shared/     service registry and logging
  CEM_NavisIAModeler_Ribbon/     upstream's own ribbon tab, standalone use only (not deployed in Cemengal)
MCP_Client/                      Node MCP server over stdio, packaged as a .dxt for Claude Desktop
  server/index.js                MCP tools -> one RPC call each
  server/navis-rpc.js            the job protocol: timeouts, following long requests (Cemengal)
  server/tests/                  RPC contract tests (Cemengal)
  manifest.json                  .dxt manifest: command, env NAVISWORKS_API_PORT, tool list
generated_MCP_Client/, generated_MCP_Server/   upstream's prebuilt 2026 artifacts (stale)
docs/                            this wiki (Cemengal)
```

## The request path

1. In Navisworks, **Cemengal → AI → AI Modeler** (CEM_NavisworksAPI's ribbon) starts the listener on
   `http://127.0.0.1:8765/` and `http://localhost:8765/`; the button stays pressed and shows the port
   ([0006](decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)). Standalone, upstream's tab does it
   ([standalone ribbon](modules/addin-ribbon.md)).
2. Claude Desktop runs `node server/index.js` from the installed `.dxt`, with
   `NAVISWORKS_API_PORT` from the user's config (default 8765, the server's default too).
3. A tool call such as `get_model_overview` becomes `POST /rpc {id, method, params}`, with the access
   token the AI Modeler wrote to the user's settings folder when it started
   ([0009](decisions/0009-local-access-token.md)).
4. `RpcRouter.Dispatch` finds the route in `RpcMap` and runs it as a job, one at a time: a request
   that arrives while another runs is refused at once (`NVX_BUSY`), and one still running after 20 s
   answers with its job id ([0008](decisions/0008-one-job-at-a-time.md)). A controller validates the
   parameters, and a backend reads Navisworks on its UI thread, pausing every 100 ms in long scans
   ([server](modules/rpc-server.md)). The answer is `{ok, data | error, meta}`.
5. The client turns `data` into MCP content for the agent, following a running job for up to 45 s
   so every tool call ends inside the host's 60 s timeout ([client](modules/node-client.md)).

The ten upstream tools: `get_model_overview`, `get_units_and_tolerances`,
`get_property_distribution_by_category`, `get_element_count_by_category`, `list_properties_for_item`,
`list_items_to_property`, `clear_selection`, `get_current_selection_snapshot`, `apply_selection`,
`run_simple_clash`; since [0008](decisions/0008-one-job-at-a-time.md) the job tools
`get_server_status`, `get_job_status`, `cancel_job`; and since [0010](decisions/0010-send-code-and-screenshot.md)
`send_code_to_navisworks` (C# compiled with Roslyn and run in Navisworks, for anything no tool does) and
`take_screenshot`. The names are pinned across both halves by [the RPC contract](concepts/rpc-contract.md).

## Place in the family

The Navisworks counterpart of [CEM_RevitMCP](../../CEM_RevitMCP/docs/index.md), next to the
Navisworks add-ins in [CEM_NavisworksAPI](../../../CEM_NavisworksAPI/docs/index.md). It shares no code
with either yet. Root node page: [CEM_NavisworksMCP](../../../docs/repos/CEM_NavisworksMCP.md).
It makes no CEM_API request; should it need one, the call goes through CEM_CSharpExtended's `Api`
(root [0023](../../../docs/decisions/0023-one-shared-request-layer.md), [CEM_API#74](sources/issues/CEM-EnriqueRuiz-CEM_API-74.md)).
