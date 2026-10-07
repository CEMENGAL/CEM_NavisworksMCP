---
title: "CEMENGAL/CEM_NavisworksAPI#1 (umbrella): the Navisworks MCP fork, ported, renamed and hosted"
type: source
updated: 2026-10-08
sources: [https://github.com/CEMENGAL/CEM_NavisworksAPI/issues/1]
related: [../commits/2026-10.md, ../../decisions/0001-sdk-style-2027-build.md, ../../decisions/0005-no-cors-refuse-browser-origins.md, ../../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md, ../../../../../docs/features/CEMENGAL-CEM_NavisworksAPI-1.md]
tags: [issue, fork, mcp, ai-modeler]
---

# CEMENGAL/CEM_NavisworksAPI#1 (umbrella)

**Issue:** https://github.com/CEMENGAL/CEM_NavisworksAPI/issues/1 ("Crear el repositorio e inicializar
el menú") · **Opened** 2026-10-08 · open. It lives in CEM_NavisworksAPI's tracker; this fork's work is
part of it, so its commits carry the same link ([feature page](../../../../../docs/features/CEMENGAL-CEM_NavisworksAPI-1.md)).

## What this repo got (one commit)
- The 2027 port: [0001](../../decisions/0001-sdk-style-2027-build.md), [0002](../../decisions/0002-msbuild-bundle-deploy.md).
- The contract tests and the security fix: [0003](../../decisions/0003-rpc-contract-tests.md),
  [0005](../../decisions/0005-no-cors-refuse-browser-origins.md).
- How it diverges from upstream: [0004](../../decisions/0004-minimal-divergence-from-upstream.md).
- The rename to `CEM_NavisIAModeler`, hosting by the Cemengal ribbon, port 8765 and the clash fix:
  [0006](../../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md).

The step-by-step record, with the mistakes made on the way, is in [commits 2026-10](../commits/2026-10.md).

## Verified
14/14 `npm test`; both configurations build with 0 errors; live in Navisworks 2027 the AI Modeler starts
and stops from the Cemengal ribbon, and seven tools answered through the real client
([smoke test](../../workflows/smoke-test.md)). Open: the whole-document id lookup, four tools, the clash
after its fix, authentication ([known gaps](../../concepts/known-gaps.md)).
