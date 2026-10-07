---
title: "Connect an agent — start the AI Modeler and register the client"
type: workflow
updated: 2026-10-07
sources: [MCP_Client/manifest.json, MCP_Client/README.md, MCP_Client/server/index.js]
related: [../modules/node-client.md, smoke-test.md, ../concepts/known-gaps.md]
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
   - **Any MCP host, as a command** (Claude Code, for example): run `npm install` once in
     `MCP_Client/server`, then register `node <path>\MCP_Client\server\index.js`. For Claude Code:
     `claude mcp add navisworks -- node <path>\MCP_Client\server\index.js`
     (add `--env NAVISWORKS_API_PORT=<port>` for a non-default port).
3. Ask the agent for `get_model_overview`. An `HTTP` or `ECONNREFUSED` error means the server is not
   running (the button is not pressed) or the ports differ.

Never install upstream's `generated_MCP_Client/waabe-navisworks-mcp.dxt`: it is the 2026 build.
