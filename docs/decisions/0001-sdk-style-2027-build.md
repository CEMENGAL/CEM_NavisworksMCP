---
title: "0001 — SDK-style net48 build for Navisworks 2027, with the API from NuGet"
type: decision
status: current
updated: 2026-10-07
sources: [MCP_Server/Directory.Build.props, MCP_Server/waabe_navi_mcp/waabe_navi.csproj, MCP_Server/waabe_navi_mcp_server/waabe_navi_mcp_server.csproj, MCP_Server/waabe_navi_shared/waabe_navi_shared.csproj, MCP_Server/waabe_navi_mcp.sln]
related: [0002-msbuild-bundle-deploy.md, ../workflows/build-and-deploy.md, ../../../../CEM_NavisworksAPI/docs/decisions/0001-net48-year-by-configuration.md, ../../../../CEM_NavisworksAPI/docs/decisions/0002-api-from-nuget-compile-only.md]
tags: [build, msbuild, nuget, port]
---

# 0001 — SDK-style net48 build for Navisworks 2027

## Context

Upstream's three projects were old-style csproj (VS 2022) with `HintPath`s to
`..\..\..\..\..\..\..\Program Files\Autodesk\Navisworks Manage 2026\*.dll`, so they built only on a
machine with Navisworks 2026 installed at that relative depth. They also referenced four DLLs no NuGet
package carries for 2027 (`Automation`, `Interop.ComApiAutomation`, `Navisworks.Clash.Plugin`,
`Navisworks.Clash.Mfc.Interop`). The fork targets Navisworks 2027, which is still .NET Framework 4.8
(checked in CEM_NavisworksAPI, [its 0001](../../../../CEM_NavisworksAPI/docs/decisions/0001-net48-year-by-configuration.md)).

## Decision

- The projects are SDK-style: `Microsoft.NET.Sdk` for `waabe_navi_shared`, `Microsoft.NET.Sdk.WindowsDesktop`
  with WPF and WinForms for the other two. Assembly names and namespaces are unchanged.
  `GenerateAssemblyInfo=false` keeps upstream's `Properties/AssemblyInfo.cs`.
- `MCP_Server/Directory.Build.props` sets `net48`, `x64`, the configurations `Debug N27`/`Release N27`,
  `$(NavisworksVersion)` 2027 and `$(NavisworksSeries)` 24, as CEM_NavisworksAPI does.
- The Navisworks API comes from `Speckle.Navisworks.API` `2027.*`, compile-only (`ExcludeAssets="runtime"`).
  The 4.8 reference assemblies come from `Microsoft.NETFramework.ReferenceAssemblies`.
- The four DLLs not on NuGet are dropped. No source file uses their namespaces; only the namespaces
  `Api`, `Plugins`, `Interop`, `Interop.ComApi`, `DocumentParts`, `Controls`, `ComApi`, `Clash` and
  `Autodesk.Windows` are used, and the package covers them all.
- The framework references are carried over from the old projects, including `System.Web.Extensions`
  (for `JavaScriptSerializer`), which the first port missed.
- One 2027 API change was ported in `FallbackBackend.RunSimpleClash`: `TestsData.Tests` became
  `TestsData.Value.TestsRoot.Children`, and `TestsAddCopy(test)` became `TestsAddCopy(TestsRoot, test)`.
  The 2027 members were read by reflection from the package's `Autodesk.Navisworks.Clash.dll`.

**Rejected:**
- Keeping `HintPath`s, pointed at 2027: building would still need Navisworks installed.
- `Chuongmep.Navis.Api.*` and `Alexpivo394.*`: they stop at 2026 or 2025.
- Multi-targeting 2026 and 2027: the fork exists for 2027; upstream stays for 2026.

## Consequences

- **Names refined by [0006](0006-renamed-and-hosted-by-cem-ribbonui.md):** the projects and the
  solution were renamed (`CEM_NavisIAModeler`, `_Shared`, `_Ribbon`, `CEM_NavisworksMCP.sln`); the build
  above is unchanged, plus a default year for builds started from another solution.

- `dotnet build MCP_Server\CEM_NavisworksMCP.sln -c "Debug N27"` works anywhere with a .NET 5+ SDK.
  It gives 0 errors and upstream's 2 CS1998 warnings.
- A future upstream change to the csproj files conflicts with ours; keep ours ([upstream and fork](../concepts/upstream-and-fork.md)).
