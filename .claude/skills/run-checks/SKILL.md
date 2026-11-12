---
name: run-checks
description: Runs the Unity Chat SDK verification steps (C# Chat build, Unity copy sync check, ConfigureAwait(false) check, PubnubChatConfigAsset comparison, Unity batch-mode compile, targeted live tests on request) and reports what was and wasn't run. Use after changing C# Chat code, Unity-only files or project files, before committing or opening a PR, or when asked to build or test.
---

# Run checks

Run everything from the repository root. You need the .NET 8 SDK or newer. The scripts are PowerShell: run them with `powershell -ExecutionPolicy Bypass -File <script>` on Windows, or `pwsh -File <script>` on macOS and Linux.

## 1. Consistency checks (seconds)

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/sync-unity-chat/scripts/Sync-UnityChat.ps1 -Check
powershell -ExecutionPolicy Bypass -File .claude/skills/run-checks/scripts/Test-ConfigureAwait.ps1
powershell -ExecutionPolicy Bypass -File .claude/skills/run-checks/scripts/Compare-ChatConfigAsset.ps1
```

- **`Sync-UnityChat.ps1 -Check`** fails when the Unity copy differs from the C# Chat library, or when a `.meta` file is missing or orphaned. Fix it by running the script without `-Check` (the `sync-unity-chat` skill).
- **`Test-ConfigureAwait.ps1`** reports awaits without `.ConfigureAwait(false)` on lines changed against `origin/master`, plus untracked files.
  - `-All` lists the 24 older ones in the library; leave those alone unless asked.
  - A new violation is a failure, unless the user agreed to it for a listener callback (see `.cursor/rules/async-and-events.mdc`).
- **`Compare-ChatConfigAsset.ps1`** must print no FAIL lines. A WARN is a default mismatch between `PubnubChatConfig` and `PubnubChatConfigAsset`; fix it unless the user wants the difference.

## 2. Build

```bash
dotnet build c-sharp-chat/PubnubChatApi/PubnubChatApi.sln
```

- **Warnings:** a full rebuild of the library prints about 112 existing warnings, mostly nullable-reference ones; the test project adds `[Obsolete]` usages. Count them with `dotnet build c-sharp-chat/PubnubChatApi/PubnubChatApi/PubnubChatApi.csproj --no-incremental`, and don't add new ones in code you touched.
- **Configurations:** the solution builds the library in its Release configuration even for Debug, so the output is always `bin/Release/netstandard2.1/`.

## 3. Unity compile

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/run-checks/scripts/Test-UnityCompile.ps1
```

- **Coverage:** the library copy compiled against the `com.pubnub.sdk` package, the Unity-only `Runtime/*.cs` files and `Assets/Snippets/`. Not `Samples~/`.
- **Duration:** about 30 seconds normally, several minutes after a package or editor change.
- **"The project is open in another Unity editor":** ask the user to close it, or to check the Console of the open editor, which recompiles when it gets focus.
- **No editor at the default Unity Hub path:** pass `-UnityPath`, or ask the user, and report the check as not run.
- **Side effects:** the run can create `.meta` files for new assets; commit them. If it changes other tracked files (`ProjectSettings/`, `Packages/packages-lock.json`), don't commit those; tell the user.

## 4. Live tests (only when the user asks)

```bash
dotnet test c-sharp-chat/PubnubChatApi/PubNubChatApi.Tests/PubNubChatApi.Tests.csproj --filter "FullyQualifiedName~<Fixture>"
```

- **Keys:** the tests read `PN_PUB_KEY`, `PN_SUB_KEY` and `PN_SEC_KEY`, falling back to the public `demo-36` keyset. They hit the network and wait on fixed delays.
- **Failures:** network or timing failures aren't regressions from your change. Report them rather than retrying until they pass.
- **The full suite,** which is what CI runs, is the same command without `--filter`. It takes many minutes.

## Report

Summarize:
- the build result, and whether the warning count changed
- the sync, `ConfigureAwait` and config-asset check results
- the Unity compile result, or why it didn't run
- which tests ran, with counts, or that none ran and why
- what is left for the user to verify in Unity: `Samples~`, play-mode behavior such as events arriving on the main thread, and WebGL builds
