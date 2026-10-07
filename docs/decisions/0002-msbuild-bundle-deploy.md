---
title: "0002 — One MSBuild target deploys the ApplicationPlugins bundle"
type: decision
status: current
updated: 2026-10-07
sources: [MCP_Server/waabe_navi_mcp_server/waabe_navi_mcp_server.csproj, MCP_Server/waabe_navi_mcp/PackageContents.xml, MCP_Server/waabe_navi_mcp_server/Infrastructure/ManifestBuilder.cs]
related: [0001-sdk-style-2027-build.md, ../workflows/build-and-deploy.md]
tags: [deploy, bundle]
---

# 0002 — One MSBuild target deploys the bundle

## Context

Upstream deployed with three German `cmd` post-build events (`xcopy` into
`%APPDATA%\Autodesk\ApplicationPlugins\waabe_navi_mcp.bundle\Contents\v23`), one per project. They ran
on every build, on any machine, and hard-coded the 2026 series (`v23`). Navisworks loads a
`.bundle` folder through its `PackageContents.xml`, which names each DLL and the series range.

## Decision

- `waabe_navi_mcp_server.csproj` (it references the other two) has one `DeployNavisworksBundle` target
  after `Build`. It writes:
  - `PackageContents.xml` at the bundle root;
  - the three DLLs (and PDBs) plus `mcp_manifest.json` in `Contents\v$(NavisworksSeries)\` (`v24`);
  - `Ribbon\Buttons.xml` and `Ribbon\Icons\*.png`, where the ribbon plugin looks for them;
  - an empty `Settings\` folder, for the port settings.
- It runs by default only where Navisworks Manage 2027 is installed. The install path comes from
  `HKLM\SOFTWARE\Autodesk\Navisworks Manage\24.0\Location` (`Path`), read in `Directory.Build.props`, with a
  `reg query` fallback in `MCP_Server/Directory.Build.targets` because the .NET 5 SDK's MSBuild returns empty
  for registry functions (the same files as CEM_NavisworksAPI). Navisworks need not be on `C:`.
  `-p:DeployNavisworksBundle=true` forces it, `=false` skips it, and a DLL locked by a running
  Navisworks turns the copy into a warning.
- `PackageContents.xml` says `Nw24`/`Contents\v24`, and `ManifestBuilder.GetDefaultContentDirectory`
  says `v24`. The two `ComponentEntry`s for DLLs the solution does not build
  (`waabe_navi_chatfenster`, `waabe_navi_mcpserver`, leftovers of upstream experiments) were removed.

**Rejected:** keeping the post-build events with `v24`. They are `cmd`-only, run without Navisworks,
and repeat the paths three times.

## Consequences

- **Refined by [0006](0006-renamed-and-hosted-by-cem-ribbonui.md):** this bundle is now the
  *standalone* one, `CEM_NavisIAModeler.bundle`, deployed by `CEM_NavisIAModeler_Ribbon` only when no
  CEM_NavisworksAPI host is present. In a Cemengal checkout the server ships in `CEM_NavisworksAPI.bundle`.

- A new Navisworks year changes `NavisworksSeries`, `PackageContents.xml` and `ManifestBuilder` together.
- Verified on 2026-10-07: the bundle loads in Navisworks Manage 2027 (24.0.1453.24, installed on `D:`),
  the **waabe** tab appears and the server starts ([smoke test](../workflows/smoke-test.md)).
