---
title: "Connect an agent — start the AI Modeler and register the client"
type: workflow
updated: 2026-10-21
sources: [MCP_Client/manifest.json, MCP_Client/README.md, MCP_Client/server/index.js, MCP_Client/server/navis-rpc.js]
related: [../modules/node-client.md, smoke-test.md, ../concepts/known-gaps.md, ../decisions/0008-one-job-at-a-time.md, ../decisions/0009-local-access-token.md]
tags: [mcp, claude, setup]
---

# Connect an agent

1. In Navisworks, press **Cemengal → AI → AI Modeler**. The button stays pressed and reads
   `AI Modeler (8765)`; there is no dialog. (Standalone: **AI Modeler → Server starten**, then the port
   dialog.) Port 8765 is the default on both sides, so nothing else needs setting.
2. Register the client with your agent host:
   - **Claude Desktop, as an extension:** package the client (`dxt pack . cem-navis-aimodeler.dxt` in
     `MCP_Client/`, see [node client](../modules/node-client.md)) and install the `.dxt`. Change its
     **Navisworks API Port** setting only if you changed the server's port.
   - **Claude Desktop, by its config file:** run `npm install` once in `MCP_Client/server`, then add to
     `mcpServers` in `claude_desktop_config.json`
     `"cem-navis-aimodeler": { "command": "C:\\Program Files\\nodejs\\node.exe", "args": ["<path>\\MCP_Client\\server\\index.js"] }`
     and quit Claude Desktop from the tray icon, then reopen it. The Store (MSIX) install keeps that
     file in `%LOCALAPPDATA%\Claude-Data\claude_<id>\`, not `%APPDATA%\Claude\` (seen 2026-10-20).
   - **Any MCP host, as a command** (Claude Code, for example): run `npm install` once in
     `MCP_Client/server`, then register `node <path>\MCP_Client\server\index.js`. For Claude Code:
     `claude mcp add navisworks -- node <path>\MCP_Client\server\index.js`
     (add `--env NAVISWORKS_API_PORT=<port>` for a non-default port).
3. Ask the agent for `get_model_overview`. "Cannot reach the AI Modeler" means the server is not
   running (the button is not pressed) or the ports differ. "Refused the request: no valid access token"
   means the client could not read the token the AI Modeler writes at start: the agent host must run as the
   same Windows user as Navisworks ([0009](../decisions/0009-local-access-token.md)). "Still running in Navisworks as job …" is
   not an error: Navisworks runs one request at a time, and the agent waits with `get_job_status`;
   `get_server_status` says what is running ([0008](../decisions/0008-one-job-at-a-time.md)).

Never install upstream's `generated_MCP_Client/waabe-navisworks-mcp.dxt`: it is the 2026 build.
