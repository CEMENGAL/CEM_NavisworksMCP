# CEM_NavisworksMCP: agent instructions

A fork of `kikki/MCP-Add-in-Autodesk_Navisworks_Manage_2026`, ported to Navisworks Manage 2027 and renamed
CEM_NavisIAModeler: a C# server (`MCP_Server/CEM_NavisIAModeler`, hosted by CEM_NavisworksAPI's ribbon) and
a Node MCP client (`MCP_Client/server/`, `cem-navis-aimodeler`).

1. **Read [docs/index.md](docs/index.md) ("Start here") first**, then the pages its "Before you
   change X" table names. The wiki holds the conventions, the decisions and why, and the known gaps.
2. Follow the canonical skill: [.claude/skills/cem-navismcp-dev/SKILL.md](.claude/skills/cem-navismcp-dev/SKILL.md)
   (hard rules, verify commands).
3. Cross-repo invariants: [../../CROSS_REPO.md](../../CROSS_REPO.md).
4. Search the wiki with the qmd CLI when the index is not enough:
   `qmd query -c cem_navismcp "..."`, or `qmd query "..."` across all wikis. Read a hit with
   `qmd get qmd://cem_navismcp/<path>.md`. The collection names are listed in
   [../../tools/wiki/README.md](../../tools/wiki/README.md).
5. **Commit-time ingest.** Every commit carries its own wiki update in `docs/`:
   - the source page `sources/issues/<org>-<repo>-<N>.md`, or `sources/commits/<YYYY-MM>.md`, with
     each decision, its reason, and the rejected alternatives
   - the affected pages, and new pages in `index.md`
   - a `log.md` entry
   - `python ../../tools/wiki/wiki_lint.py .` with 0 errors

   The commit message is one line, `FEAT|FIX|REFACTOR|DOCS - Description. <issue URL>`, with no body
   or trailers, on `main`, only after the user sends the issue link. The git `pre-commit` hook
   rejects code without its docs update.
