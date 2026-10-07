# CEM_NavisworksMCP

A fork of `kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026`, ported to **Navisworks Manage 2027** and renamed
**CEM_NavisIAModeler** (the AI Modeler), as CEM_RevitMCP became CEM_IAModeler: a C# server
(`MCP_Server/CEM_NavisIAModeler`) that runs a local HTTP/RPC server inside Navisworks, hosted by
CEM_NavisworksAPI's Cemengal ribbon (**AI → AI Modeler**), and a Node MCP client
(`MCP_Client/server/index.js`, `cem-navis-aimodeler`) that lets an AI agent such as Claude call it.

**Read [docs/index.md](docs/index.md) ("Start here") before planning or editing**, then the pages it
names. The wiki holds the conventions, the decisions and their reasons, and the known gaps. The
upstream READMEs describe the 2026 proof of concept; the code and the wiki win.

Load the `cem-navismcp-dev` skill (`.claude/skills/cem-navismcp-dev/SKILL.md`) before writing code.
Cross-repo rules: [../../CROSS_REPO.md](../../CROSS_REPO.md).

Every commit carries its wiki update ([docs/workflows/finish-a-change.md](docs/workflows/finish-a-change.md)).
Commit message: `FEAT|FIX|REFACTOR|DOCS - Description. <issue URL>`. **That line is the entire
message**: no body, no file list, no test tally, no `Co-Authored-By`. Use one `git commit -m "..."`.
