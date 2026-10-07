---
title: "Build and deploy — Debug N27, hosted or standalone"
type: workflow
updated: 2026-10-07
sources: [MCP_Server/Directory.Build.props, MCP_Server/Directory.Build.targets, MCP_Server/CEM_NavisworksMCP.sln, MCP_Server/CEM_NavisIAModeler_Ribbon/CEM_NavisIAModeler_Ribbon.csproj]
related: [../decisions/0001-sdk-style-2027-build.md, ../decisions/0002-msbuild-bundle-deploy.md, ../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md, smoke-test.md]
tags: [build, deploy]
---

# Build and deploy

```powershell
dotnet build MCP_Server\CEM_NavisworksMCP.sln -c "Debug N27" -v minimal -nologo
dotnet build MCP_Server\CEM_NavisworksMCP.sln -c "Release N27" -v minimal -nologo
```

- **Prerequisites:** a .NET SDK 5.0 or newer. No Navisworks, no 4.8 targeting pack
  ([0001](../decisions/0001-sdk-style-2027-build.md)).
- **Expected:** 0 errors, 2 warnings (CS1998, in upstream code).

## Who deploys

| Checkout | What runs in Navisworks | How it gets there |
|---|---|---|
| **Cemengal** (a sibling `CEM_NavisworksAPI` exists) | the Cemengal tab's **AI → AI Modeler** button | build **CEM_NavisworksAPI** (`dotnet build CEM_NavisworksAPI.sln -c "Debug N27"`): its `CEM_RibbonUI` builds this server and deploys it in `CEM_NavisworksAPI.bundle`. Building this solution deploys nothing |
| **Standalone** (this repo alone) | an **AI Modeler** tab ([standalone ribbon](../modules/addin-ribbon.md)) | building this solution deploys `%APPDATA%\Autodesk\ApplicationPlugins\CEM_NavisIAModeler.bundle\`, where Navisworks Manage 2027 is installed |

The rule is `CemStandaloneDeploy` in `MCP_Server/Directory.Build.props`
([0006](../decisions/0006-renamed-and-hosted-by-cem-ribbonui.md)). The install is found through the
registry on any drive ([0002](../decisions/0002-msbuild-bundle-deploy.md)). Close Navisworks before
deploying; `-p:DeployNavisworksBundle=true` forces the standalone deploy (add `-p:APPDATA=<folder>` to
try it elsewhere), `=false` skips it.

- **The client:** nothing to build. `npm install` in `MCP_Client/server` for the dependencies; to
  package a `.dxt`, see [node client](../modules/node-client.md).
- **Do not** install the files in `generated_MCP_Server/` or `generated_MCP_Client/`: they are
  upstream's 2026 builds ([known gaps](../concepts/known-gaps.md)).
