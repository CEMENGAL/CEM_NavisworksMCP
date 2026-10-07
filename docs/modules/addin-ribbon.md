---
title: "CEM_NavisIAModeler_Ribbon — upstream's ribbon tab, for standalone use"
type: module
updated: 2026-10-07
sources: [MCP_Server/CEM_NavisIAModeler_Ribbon/Plugins/CEM_NavisIAModeler_Ribbon.cs, MCP_Server/CEM_NavisIAModeler_Ribbon/Ribbon/Buttons.xml, MCP_Server/CEM_NavisIAModeler_Ribbon/Commands/, MCP_Server/CEM_NavisIAModeler_Ribbon/PackageContents.xml, MCP_Server/CEM_NavisIAModeler_Ribbon/CEM_NavisIAModeler_Ribbon.csproj]
related: [rpc-server.md, shared.md, ../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md]
tags: [module, ribbon, addin, standalone]
---

# CEM_NavisIAModeler_Ribbon

Upstream's `waabe_navi_mcp`, renamed ([0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)).
**In a Cemengal checkout it is built but not deployed**: the Cemengal tab's **AI Modeler** button
replaces it. It is the fork's entry point when the repo is used alone.

- `Plugins/CEM_NavisIAModeler_Ribbon.cs` builds an **AI Modeler** ribbon tab at start-up from
  `Ribbon/Buttons.xml` (loaded from the bundle's `Ribbon\` folder by `Helpers/ButtonsXmlLoader`), with
  icons from `Ribbon\Icons\`, as AdWindows controls in code (not a `[RibbonLayout]` XAML).
- Buttons (`Buttons.xml`, German labels from upstream):
  - `BTN_MCP`, **MCP Info** → `Commands/MCPButtonHandler`: an About box naming the fork and upstream.
  - `BTN_MCP_SERVER`, **Server starten** → `Commands/MCPServerButtonHandler`: starts or stops the
    server through the service registry, after a port dialog (default 8765), and confirms with a
    **modal** message box. That box blocks the UI until dismissed; the hosted path has none.
- `CEM_NavisIAModeler_Ribbon.csproj` references the server only so the standalone bundle carries it, and
  owns the standalone deploy: `CEM_NavisIAModeler.bundle` with `PackageContents.xml` (`Nw24`), the
  three DLLs, `mcp_manifest.json`, `Ribbon\` and `Settings\`, only when `CemStandaloneDeploy` is true
  ([0002](../decisions/0002-msbuild-bundle-deploy.md), [0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)).
