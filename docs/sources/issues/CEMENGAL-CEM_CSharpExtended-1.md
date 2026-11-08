---
title: "CEMENGAL/CEM_CSharpExtended#1: C# libraries mirror the native library; here, the compile migration"
type: source
updated: 2026-11-08
sources: [https://github.com/CEMENGAL/CEM_CSharpExtended/issues/1, MCP_Server/CEM_NavisIAModeler/Infrastructure/AccessToken.cs, MCP_Server/CEM_NavisIAModeler/Services/SettingsManager.cs, MCP_Server/CEM_NavisIAModeler_Shared/LogHelper.cs, MCP_Server/CEM_NavisIAModeler/Controllers/CodeController.cs, MCP_Client/server/navis-rpc.js]
related: [../../decisions/0007-per-user-home-no-writes-next-to-the-dll.md, ../../decisions/0009-local-access-token.md, ../../decisions/0010-send-code-and-screenshot.md, ../../decisions/0011-logs-in-local-temp-nothing-under-acc.md, ../../modules/rpc-server.md, ../../concepts/known-gaps.md]
tags: [issue, cem_csharpextended, naming, migration, cross-repo]
---

# CEMENGAL/CEM_CSharpExtended#1

**What was asked.** Root decision [0041](../../../../../docs/decisions/0041-csharp-libraries-mirror-the-native-one.md)
(CROSS_REPO §6/§6b): every shared C# library mirrors the native library it extends, with the BCL's namespaces
and `X<Native>` names, and no add-in writes its own copy of reusable logic. CEM_CSharpExtended's core was
restructured; for the MCP repos the issue and its contract (item A16) ask for the **compile migration** to the
moved names, nothing else.

**What changed here (10 files, names only).**

| Was | Now |
|---|---|
| `using CEM_CSharpExtended.Hosting;` | `using CEM_CSharpExtended.IO;` (`CemUserHome`), `using CEM_CSharpExtended;` (the resolver) |
| `CemUserHome.NavisworksSettingsFile(name)` | `CemUserHome.AppSettingsFile("CEM_NavisworksAPI.bundle", name)` (`AccessToken`, `SettingsManager`) |
| `CemUserHome.NavisworksFolder` | `CemUserHome.AppFolder("CEM_NavisworksAPI.bundle")` (`LogHelper`'s startup line) |
| `DependencyResolver.Install()` | `AppDomain.CurrentDomain.InstallDependencyResolver()` (`CodeController`) |

The guard tests (`access-token`, `code-and-screenshot`, `file-writes`, `no-dll-adjacent-writes`) match the new
names; the comments in `navis-rpc.js` and the csproj follow. Paths on disk are unchanged: the token and settings
stay in `%APPDATA%\Autodesk\ApplicationPlugins\CEM_NavisworksAPI.bundle\Settings`.

**Decisions, with reasons**
- **Names only, no refactor.** The contract (A16) keeps the MCP repos to the compile migration, so this fork's
  divergence stays small ([0004](../../decisions/0004-minimal-divergence-from-upstream.md)). **Rejected:** moving
  the duplicate helpers into CEM_NavisworksAPI_Extended now; recorded for later in
  [known gaps](../../concepts/known-gaps.md) item 13.
- **The bundle name is written inline** at each call (QA-5). The generic `AppSettingsFile(bundle, name)` replaced
  the Navisworks-specific members, whose home is now `XApplication`'s host settings in CEM_NavisworksAPI_Extended.
  **Rejected:** a local constant here, a helper module of the kind 0041 forbids; the shared one comes with the
  later move.

**Verified.** In pass 1: `npm test` and the `Debug N27` build. **Not verified:** the migrated calls in Navisworks.

**The procedure, in the docs and the skill (2026-11-08, pass 2).** The user asked for the new way to proceed
to be written into the wikis and the skills, so that every agent writing C# here, without being told, writes new
code on the mirror structure and migrates the old names in a file it touches. Only the procedure: no code, no
test run. The dev skill's first hard rule, `CLAUDE.md`, `AGENTS.md`, index hard rule 10 and its lookup row, and
[shared](../../modules/shared.md) point to CEM_CSharpExtended's
[C# procedure](../../../../../CEM_CSharpExtended/docs/workflows/csharp-procedure.md) and its `cem-csharp-mirror`
skill. A reusable helper goes to CEM_CSharpExtended or, for Navisworks host code, to CEM_NavisworksAPI_Extended;
upstream code is still touched only under [0004](../../decisions/0004-minimal-divergence-from-upstream.md), but
what is touched is migrated. **Rejected:** copying the procedure here (CROSS_REPO: one copy); a repo-local
conventions page (the repo has none; the index and the skill are where agents look first).

**Open at push time** (from the issue; pushed on request so a new project can start from the foundation): the
final independent live check, the QA and security reviews and the acceptance run are pending; the migration
here was not run in Navisworks ([known gaps](../../concepts/known-gaps.md) item 14). Elsewhere: five Navisworks
tools still reference CEM_Rules; `WriteTable` fails on an edge-apostrophe sheet title or a cell over 32,767
characters; a text cell starting with `'` loses it in the xlsx; Revit not checked live.
