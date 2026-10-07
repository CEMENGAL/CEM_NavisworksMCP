---
title: "0004 — Diverge from upstream minimally, and mark every change"
type: decision
status: current
updated: 2026-10-07
sources: [../../CROSS_REPO.md]
related: [../concepts/upstream-and-fork.md, 0001-sdk-style-2027-build.md]
tags: [fork, upstream]
---

# 0004 — Diverge from upstream minimally, and mark every change

## Context

[CROSS_REPO §5](../../../../CROSS_REPO.md) treats forks as not Cemengal-original: CEM_RevitMCP extends
its upstream through its own documented pattern, and CEM_AgentMemory tracks upstream and avoids
gratuitous divergence. This upstream is a finished proof of concept, quiet since 2025-09-22, so syncs
are unlikely, but keeping our changes visible still makes the fork reviewable and attribution clear.

## Decision

- Change upstream code only for the 2027 port, a security fix, or a recorded Cemengal need.
- Mark each change in upstream files with a `// Cemengal …` comment saying what changed and why,
  linking the decision where there is one. Build files (`*.csproj`, `Directory.Build.props`, the `.sln`)
  are ours wholesale since [0001](0001-sdk-style-2027-build.md); they carry a header comment.
- Cemengal-only files (`docs/`, `MCP_Client/server/tests/`, `CLAUDE.md`, `AGENTS.md`, `.claude/`,
  `.agents/`, `.mcp.json`) need no marking.
- Keep upstream's `LICENSE` (MIT, © 2025 kikki) and attributions. The info dialog names both.
- Do not translate or restyle upstream code (German texts and comments stay) unless a change needs it.

**Rejected:** a rewrite into the CEM_NavisworksAPI conventions (`X` wrappers, ribbon host). It is a
working tool with its own architecture; rewriting it would make it ours to maintain in full, for no
user-visible gain. If the two should share code later, decide that at the root first.

## Consequences

- **Refined by [0006](0006-renamed-and-hosted-by-cem-ribbonui.md):** the user asked for the Revit
  treatment, so projects, namespaces and user-visible names were renamed wholesale; that rename is
  the recorded Cemengal need. Upstream class names stay, and code changes keep their `Cemengal` comments.

- `git grep -n "Cemengal"` in `MCP_Server/` and `MCP_Client/server/index.js` lists every code divergence.
