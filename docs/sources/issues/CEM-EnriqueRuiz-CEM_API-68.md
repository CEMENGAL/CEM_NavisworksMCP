---
title: "CEM-EnriqueRuiz/CEM_API#68: the LLM wiki, searched through the machine's shared qmd server"
type: source
updated: 2026-10-11
sources: [https://github.com/CEM-EnriqueRuiz/CEM_API/issues/68, .mcp.json, .claude/settings.local.json]
related: [../../concepts/upstream-and-fork.md, ../../workflows/finish-a-change.md, ../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-68.md, ../../../../../docs/decisions/0020-one-shared-qmd-server-per-machine.md]
tags: [issue, wiki, qmd, process, cross-repo]
---

# CEM-EnriqueRuiz/CEM_API#68

**Issue:** https://github.com/CEM-EnriqueRuiz/CEM_API/issues/68 · open. It lives in CEM_API's tracker and covers every repo's wiki
([feature page](../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-68.md)). This repo's wiki, hooks and qmd
settings came with the fork bootstrap, under [CEMENGAL/CEM_NavisworksAPI#1](CEMENGAL-CEM_NavisworksAPI-1.md);
only the change below carries the #68 link.

## qmd through the machine's shared server (2026-10-11)
- **What:** `.mcp.json`'s `qmd` entry no longer starts a stdio `qmd mcp` per session
  (`"command": "cmd", "args": ["/c", "qmd", "mcp"]`). It is
  `{ "type": "http", "url": "http://127.0.0.1:8181/mcp" }`, the machine's one shared server.
- **Why:** root decision [0020](../../../../../docs/decisions/0020-one-shared-qmd-server-per-machine.md). A
  per-session server loads 2 to 3.5 GB of models for a hybrid query, and nine sessions exhausted a 20 GB machine.
  One server, owned by the "Cemengal qmd server" logon task, loads them once for every session.
- **Unchanged:** the server name stays `qmd`, so `enabledMcpjsonServers: ["qmd"]` in
  `.claude/settings.local.json` still applies. Scope repo questions to the `cem_navismcp` collection. The
  `qmd` CLI (`qmd query -c cem_navismcp "..."`) keeps working when the server is down, because it reads the
  index directly.
- **Why so late:** the fork's bootstrap (2026-10-07) copied the stdio entry the other repos had then, and it
  missed their 2026-10-08 switch. The coordinator caught it on 2026-10-11, with CEM_CSharpExtended and
  CEM_NavisworksAPI.
