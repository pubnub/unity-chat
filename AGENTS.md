# AGENTS.md

Guidance for AI coding agents working on the PubNub Unity Chat SDK, and the single source of truth for guidance shared by all agents. Cursor and other agents that read `AGENTS.md` (such as Codex) load it directly, and `CLAUDE.md` imports it for Claude Code. Area-specific rules live in `.cursor/rules/` and reusable workflows in `.claude/skills/` (see [Where to find more](#where-to-find-more)).

## What this repository is

- **The product:** the PubNub Unity Chat SDK, a chat layer over PubNub (channels, users, memberships, messages, threads, typing indicators, read receipts, moderation, files, push).
  - It ships as the UPM package `com.pubnub.pubnubchat`, the folder `unity-chat/PubnubChatUnity/Assets/PubnubChat`.
  - Customers install it from a git URL (`https://github.com/pubnub/unity-chat.git?path=/unity-chat/PubnubChatUnity/Assets/PubnubChat`). There is no build step: the package folder at the release tag is what ships.
- **Two projects share one codebase:**

  | Project | Path | Built with | Role |
  | --- | --- | --- | --- |
  | C# Chat | `c-sharp-chat/PubnubChatApi/PubnubChatApi` | `dotnet`; `netstandard2.1`, C# 9 | **The source of truth for all Chat logic.** References the NuGet package `PubnubPCL`. |
  | C# Chat tests | `c-sharp-chat/PubnubChatApi/PubNubChatApi.Tests` | `dotnet`; `net8.0`, NUnit 3 | The test suite, and the only tests CI runs |
  | Unity Chat | `unity-chat/PubnubChatUnity` | Unity Editor 2022.3.57f1 | Development project holding the package, the docs snippets and TextMesh Pro |

- **Why the sources are copied:**
  - The package must depend on the PubNub Unity SDK (`com.pubnub.sdk`), whose `PubnubApiUnity.dll` is the C# SDK built for Unity.
  - Packing the C# Chat as a DLL that references that DLL hasn't worked, so Unity compiles a copy of the C# Chat's `.cs` files from `Assets/PubnubChat/Runtime/PubnubChatApi/`.
  - The copy matches the library file for file, and `Sync-UnityChat.ps1` writes it. Don't try to change this arrangement unless asked.
- **Unity-only code** sits next to the copy in `Assets/PubnubChat/Runtime/`:
  - `UnityChat.CreateInstance(...)`: the Unity entry point
  - `UnityListenerFactory`: listeners that raise events on the main thread
  - `UnityChatPNSDKSource`: the `pnsdk` value
  - `PubnubUnitySDKReference.asmref`: compiles all of `Runtime/` into the Unity SDK's `PubNubAPI` assembly
- **The C# SDK version must match on both sides.**
  - In .NET, the C# Chat compiles against `PubnubPCL` from NuGet. In Unity, it compiles against the C# SDK DLL bundled in the `com.pubnub.sdk` version that `package.json` depends on.
  - Both are C# SDK 8.0.1 today: `PubnubPCL` 8.0.1, and `com.pubnub.sdk` 9.3.0, which bundles it.
  - If the two differ, code that builds and passes tests in .NET can fail to compile in Unity.
- **It is one of several Chat SDKs.** The JavaScript, Kotlin and Swift Chat SDKs read and write the same data: message and event payloads, internal channel IDs, custom-data keys and message action types. Check how they behave, and read the public docs, before adding or changing anything that goes over the wire.
- **History:** earlier versions wrapped a native library built on PubNub's C-Core. The current code is pure C# on top of the C# SDK, but the git history and a few names date from the C-Core era.
- Public docs: <https://www.pubnub.com/docs/chat/unity-chat-sdk/overview>. For PubNub concepts and the C# SDK API, see the C# SDK's `AGENTS.md` (`pubnub/c-sharp`) and <https://www.pubnub.com/docs/sdks/c-sharp>.

## Chat concepts

| Concept | Code | Built on |
| --- | --- | --- |
| Chat | `Chat`, created by `Chat.CreateInstance()` (.NET) or `UnityChat.CreateInstance()` (Unity) | One `Pubnub` instance (`chat.PubnubInstance`), the `PubnubChatConfig` and a listener factory |
| User | `User` | App Context user (UUID) metadata; `custom.lastActiveTimestamp` when `StoreUserActivityTimestamp` is on |
| Channel | `Channel`, with `Type` `public`, `group` or `direct` | App Context channel metadata; the pinned message in `custom.pinnedMessageTimetoken` and `custom.pinnedMessageChannelID` |
| Membership | `Membership` | App Context membership; `custom.lastReadMessageTimetoken` drives read receipts and unread counts; invites create `status: "pending"` memberships |
| Message | `Message` | A publish of `{"type": "text", "text": ..., "files": ...}`; mentions, channel references, text links and quotes go in `meta`; edits, deletions and reactions are message actions (`edited`, `deleted`, `reaction`) |
| Thread | `ThreadChannel`, `ThreadMessage` | Channel `PUBNUB_INTERNAL_THREAD_{channelId}_{timetoken}`, plus a `threadRootId` message action on the parent message |
| Message draft | `MessageDraft` | Client-side editing of text with mentions, references, links, quotes and files, plus mention suggestions |
| Chat event | `ChatEvent`, `PubnubChatEventType` | A publish whose payload has a `"type"` of `typing` or `receipt` (neither stored), or `mention`, `invite`, `report`, `moderation` or `custom` |
| Moderation | `SetRestriction()`, `Message.Report()` | Restrictions are members of `PUBNUB_INTERNAL_MODERATION_{channelId}` with `custom.ban`, `mute` and `reason`; reports go to that channel; restriction events go to `PUBNUB_INTERNAL_MODERATION{userId}` (no underscore) |
| Muted users | `MutedUsersManager` | A client-side filter; with `SyncMutedUsers` on, stored in user metadata `PN_PRV.{userId}.mute1` |
| Access | `ChatAccessManager.CanI()` | Parses the Access Manager token in `PNConfiguration.AuthKey` |
| Push, files | `Chat.RegisterPushChannels()`, `Channel.RegisterForPush()`, `ChatFile` | The C# SDK's mobile push and file APIs |

These IDs, keys and payload shapes are shared with the other Chat SDKs, odd ones included. Never change them in only this SDK.

## Repository map

```text
c-sharp-chat/PubnubChatApi/
  PubnubChatApi.sln                     Library and tests
  PubnubChatApi/                        The C# Chat library, the source of truth (netstandard2.1, C# 9, nullable enabled)
    Entities/                           Chat, Channel, ThreadChannel, User, Membership, Message, ThreadMessage,
                                        MessageDraft, MutedUsersManager, ChatAccessManager
      Base/                             ChatEntity (StreamUpdates, SetListening), UniqueChatEntity (Id)
      Data/                             ChatOperationResult, PubnubChatConfig, *Data inputs, *Wrapper results
      Events/                           ChatEvent
    Enums/                              Channel, event, message and message action types
    Utilities/                          ChatParsers, ChatEnumConverters, listener factories, ExponentialRateLimiter,
                                        DiffMatchPatch (third-party), PubnubChatDotNetPNSDKSource
  PubNubChatApi.Tests/                  NUnit live tests (net8.0)
unity-chat/PubnubChatUnity/             Unity 2022.3.57f1 development project
  Assets/PubnubChat/                    The UPM package com.pubnub.pubnubchat (package.json)
    Runtime/PubnubChatApi/              Copy of the C# Chat library, written by Sync-UnityChat.ps1. Never edit it.
    Runtime/UnityChat.cs                UnityChat.CreateInstance(...) overloads
    Runtime/UnityListenerFactory.cs     ChatListenerFactory that produces main-thread SubscribeCallbackListeners
    Runtime/UnityChatPNSDKSource.cs     pnsdk value; the release bot sets its build constant
    Runtime/PubnubUnitySDKReference.asmref   Puts Runtime/ into the Unity SDK's PubNubAPI assembly
    Samples~/                           UPM samples: PubnubChatConfigAsset, a console sample, a UI prefab. Not compiled here.
  Assets/Snippets/                      Docs snippets (// snippet.<name> ... // snippet.end), compiled only by Unity
  Assets/TextMesh Pro/                  Imported Unity package used by the UI sample
  Packages/                             Dev project dependencies; com.pubnub.sdk comes from git
.github/workflows/                      CI: dotnet test on Windows, .pubnub.yml validation, release automation
.pubnub.yml                             Version and changelog, written by the release bot
```

## How a Chat operation works

```csharp
public async Task<ChatOperationResult<Channel>> DoThing(string channelId, ChatChannelData data)
{
    var result = new ChatOperationResult<Channel>("Chat.DoThing()", this);
    var update = await Channel.UpdateChannelData(this, channelId, data).ConfigureAwait(false);
    if (result.RegisterOperation(update))   // true when the registered operation failed
    {
        return result;
    }
    result.Result = new Channel(this, channelId, data);
    return result;
}
```

- **Results, not exceptions.** Public async methods return `Task<ChatOperationResult>` or `Task<ChatOperationResult<T>>`. Server and network failures come back as `Error = true` with `Exception` set; they are never thrown.
- **`RegisterOperation()`** takes a C# SDK `PNResult<T>` or another `ChatOperationResult`. It appends their `PNStatus` objects to `InternalStatuses`, logs them at debug level, and returns `true` on error so the caller can return early. `pnResult.ToChatOperationResult("Chat.X()", chat)` wraps a single call.
- **The operation name** (`"Class.Method()"`) only appears in logs.
- **Entities** (`Channel`, `User`, `Message`...) hold a `chat` reference and a local copy of their data. `Refresh()` re-fetches it, and listeners update it.
- **Events** are C# `event Action<...> OnXxx` members, enabled by a `StreamXxx(bool)` method. Each `StreamXxx` subscribes through `ChatEntity.SetListening(...)` with a listener from `chat.ListenerFactory.ProduceListener(...)`.
  - The older `SetListeningForXxx` methods are `[Obsolete]` wrappers around them.
  - `chat.BroadcastAnyEvent()` also forwards every chat event to `Chat.OnAnyEvent`.
- **Listener factories:** `Chat` uses `DotNetListenerFactory` (plain `SubscribeCallbackExt`, thread-pool threads) unless one is passed in. `UnityChat` passes `UnityListenerFactory`, whose `SubscribeCallbackListener` dispatches each event to Unity's main thread.
- **Logging:** `chat.Logger` is the C# SDK's `PubnubLogModule`. Configure it with `PNConfiguration.LogLevel`; in Unity, pass `unityLogging: true` to `UnityChat.CreateInstance()`.

## Commands

Run from the repository root. The .NET side needs the .NET 8 SDK or newer. The scripts are PowerShell: run them with `powershell -ExecutionPolicy Bypass -File <script>` on Windows, or `pwsh -File <script>` on macOS and Linux.

| Purpose | Command |
| --- | --- |
| Build the library and the tests | `dotnet build c-sharp-chat/PubnubChatApi/PubnubChatApi.sln` |
| Copy the library into the Unity package | `.claude/skills/sync-unity-chat/scripts/Sync-UnityChat.ps1` |
| Check that the Unity copy is in sync | `.claude/skills/sync-unity-chat/scripts/Sync-UnityChat.ps1 -Check` |
| Check awaits on changed lines | `.claude/skills/run-checks/scripts/Test-ConfigureAwait.ps1` |
| Check `PubnubChatConfigAsset` against `PubnubChatConfig` | `.claude/skills/run-checks/scripts/Compare-ChatConfigAsset.ps1` |
| Compile the Unity project in batch mode | `.claude/skills/run-checks/scripts/Test-UnityCompile.ps1` |
| One test fixture (live) | `dotnet test c-sharp-chat/PubnubChatApi/PubNubChatApi.Tests/PubNubChatApi.Tests.csproj --filter "FullyQualifiedName~MessageDraftTests"` |

- **The library build** prints about 110 existing warnings, mostly nullable-reference ones (CS8618, CS8625), and the test build adds `[Obsolete]` usages. Don't add new warnings in code you touch.
- **All 93 tests are live.** They create users, channels and messages on a real keyset and wait with fixed `Task.Delay`s.
  - Keys come from `PN_PUB_KEY`, `PN_SUB_KEY` and `PN_SEC_KEY`, falling back to the public `demo-36` keyset.
  - Run them only when the user asks, and then only the fixtures you touched. A full run takes many minutes and can fail on timing.
- **The Unity compile check** needs the Unity Editor (Unity Hub's default install path, or `-UnityPath`) and the project closed in the Editor. It takes from about 30 seconds to several minutes.
- **CI** (`run-tests.yml`, on a Windows runner) runs every test with the `PN_*` secrets. It never opens the Unity project, so only the Unity compile check, or a run in the Editor, catches Unity-side errors.
- The `run-checks` skill has the full verification workflow.

## Working rules

- **Change the C# Chat, then sync the copy.**
  - Edit only `c-sharp-chat/PubnubChatApi/PubnubChatApi`, then run `Sync-UnityChat.ps1` and commit the library change and the copy together, as the feature PRs in the history do.
  - Never edit `Runtime/PubnubChatApi/` by hand.
  - The script also creates a `.meta` for each new file and folder and deletes the `.meta` of removed ones. Unity ignores assets without a `.meta` when a package is installed from git, so a missing one breaks customers while the dev project still works.
- **Write code that compiles in both builds.**
  - **Language and APIs:** C# 9 at most (Unity 2022.3) and .NET Standard 2.1 APIs. The csproj enforces both.
  - **C# SDK APIs:** don't use any that are newer than the C# SDK bundled in the `com.pubnub.sdk` version in `package.json`.
  - **Dependencies:** don't add NuGet packages. Unity only provides Newtonsoft.Json (via `com.unity.nuget.newtonsoft-json`) and the Unity SDK.
  - **Unity runtime:** customer games run the code under IL2CPP (ahead-of-time compilation), so avoid `dynamic`, `Reflection.Emit` and runtime code generation. WebGL has no background threads, so prefer `async`/`Task.Delay` over threads and timers.
  - **Nullability:** Unity compiles the copy without a nullable context, so `?` annotations are fine; they only produce warnings, which Unity suppresses for this assembly.
- **Raise user-facing events on the listener's thread.**
  - Raise events synchronously inside a `chat.ListenerFactory.ProduceListener(...)` callback; in Unity that callback runs on the main thread.
  - Never create a `SubscribeCallbackExt` directly in library code.
  - An event raised after an `await ... ConfigureAwait(false)` or from a timer reaches Unity users on a thread-pool thread, where Unity APIs throw. See `async-and-events.mdc` before adding one.
- **Every `await` in the library ends with `.ConfigureAwait(false)`.** All but 24 of the library's 218 awaits do. `Test-ConfigureAwait.ps1` checks the lines you change; `-All` lists the 24 older exceptions.
- **Don't block or fire and forget.** No `.Result`, `.Wait()` or `GetAwaiter().GetResult()` on Chat or SDK tasks, because that freezes a Unity game. Don't add `async void` methods either; an exception thrown from one crashes the host. The existing ones are listed in `async-and-events.mdc`.
- **Changes that need a matching Unity-only change**, which the .NET build can't check:

  | When you change | Also update |
  | --- | --- |
  | `ChatListenerFactory.ProduceListener` (`Utilities/ChatListenerFactory.cs`) | `DotNetListenerFactory.cs`, plus `Runtime/UnityListenerFactory.cs`, which must call a `SubscribeCallbackListener` constructor that exists in the Unity SDK version in use |
  | `PubnubChatConfig` constructor parameters or defaults | `Samples~/PubnubChatConfigAsset/PubnubChatConfigAsset.cs` (member and implicit operator); check with `Compare-ChatConfigAsset.ps1` |
  | `Chat.CreateInstance(...)` overloads | `Runtime/UnityChat.cs`, which passes `new UnityListenerFactory()` and `new UnityChatPNSDKSource()` |
  | A public API used in the docs | `unity-chat/PubnubChatUnity/Assets/Snippets/` |

  Then run `Test-UnityCompile.ps1`, or ask the user to open the project in Unity and check the Console.
- **Keep backward compatibility.** This is a published package; break the public API only in a major version.
  - Mark replaced APIs `[Obsolete("...")]` and keep them working, as the codebase already does (`SetListeningForXxx` became `StreamXxx`; `Join()` became `JoinChannel()` plus `Connect()`).
  - Before changing a signature, search for callers in the tests, snippets, samples and Unity-only files.
- **Keep wire compatibility with the other Chat SDKs:** payloads, event types, internal channel prefixes, custom-data keys and message action types (see [Chat concepts](#chat-concepts)). Build new JSON payloads from a dictionary with `chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(...)`, not with string interpolation.
- **Test every behavior change** in `PubNubChatApi.Tests` (see `tests.mdc`). The Unity project has no tests.
- **Match the surrounding style.** There is no `.editorconfig`.
  - Library files use 4 spaces, block-scoped `namespace PubnubChatApi { }` and explicit `using`s (`ImplicitUsings` is off in the library and on in the tests).
  - Private fields are camelCase; `ExponentialRateLimiter` uses `_camelCase`.
  - Add XML doc comments to public members; the existing ones often include `<example><code>`.
- **Leave versions and changelogs to the release bot:** `.pubnub.yml`, `"version"` in `package.json`, and the `build` constant in `UnityChatPNSDKSource.cs`. The `com.pubnub.sdk` dependency in `package.json` and `PubnubPCL` in the csproj are changed by hand, when updating the base SDK (the `update-unity-sdk` skill). Suggest the changelog line for the PR description instead.
- **Protect secrets.** Never commit keys. Tests read them from the environment, and committed sample assets and snippets use empty or `demo` keys.
- **Unity assets:** commit a `.meta` with every new file or folder under `unity-chat/PubnubChatUnity/Assets/`, and never regenerate or delete existing ones, because Unity references assets by the GUID inside.
- **Don't modify:**
  - Unity-generated folders: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `obj/`, `bin/`, and the generated `*.csproj`/`*.sln` in the Unity project
  - `ProjectSettings/`, unless asked
  - `Assets/TextMesh Pro/`, which is an imported Unity package
  - `.github/.release/`, which CI checks out

## Known quirks

These are existing behaviors. Don't copy them into new code, and only fix them when asked, since some fixes change public behavior.

- **Some events reach Unity off the main thread.**
  - `Channel.OnUsersTyping` when a typing timer expires (it is a `System.Timers.Timer`).
  - `Channel.OnPresenceUpdate`, which is raised after `await WhoIsPresent().ConfigureAwait(false)`.
  - `MessageDraft.OnDraftUpdated` when mention suggestions are searched.
- **The Unity SDK stays on 9.x until Data Sync support is added.**
  - 10.0.0 replaced `SubscribeCallbackListener`'s 7-parameter constructor with an 8-parameter one that adds a Data Sync callback. `UnityListenerFactory` calls the 9.x constructor, so it must change when the dependency moves to 10.x.
  - `package.json` depends on 9.3.0, and the dev project's `Packages/manifest.json` pins the git URL to the `v9.3.0` tag. Don't move either to 10.x outside a planned base SDK update.
- **`ChatOperationResult.Error` reflects the last registered operation.** Code relies on this: a failed existence check (`GetChannel`) followed by a successful create reports success. An early failure that the method doesn't return on is hidden by later successes.
- **Payloads aren't escaped.** Event payloads are built with string interpolation: `reason` in restriction events isn't escaped, and `channelId` in the invite payload (`InviteToChannel`, `InviteMultipleToChannel`) isn't quoted.
- **`Chat` has a finalizer that calls `Destroy()`.** `Destroy()` also destroys a `Pubnub` instance that the caller created and passed in. `Chat` isn't `IDisposable`.
- **Events stall while a Unity game is paused.** The Unity SDK's dispatcher drains its queue in `FixedUpdate`, which doesn't run while `Time.timeScale == 0`.
- **The committed `SamplePubNubChatConfigAsset.asset` is out of date.** It still has fields from an older version (`PublishKey`, `SubscribeKey`, `UserId`, `AuthKey`), which Unity ignores, and lacks the newer ones, which Unity fills with the asset's defaults.
- **`Samples~` is never compiled.** Unity skips folders ending in `~`, so sample code can only be checked by copying it into `Assets/` temporarily or importing the package into another project.

## Where to find more

Area rules in `.cursor/rules/` are markdown with YAML frontmatter. Cursor attaches them automatically by glob. Other agents should read the matching rule before editing:

| When editing | Rule |
| --- | --- |
| The C# Chat library (`c-sharp-chat/PubnubChatApi/PubnubChatApi/`) | `.cursor/rules/chat-library.mdc` |
| Async code, listeners and events in the library or Unity layer | `.cursor/rules/async-and-events.mdc` |
| Anything in `Assets/PubnubChat/Runtime/PubnubChatApi/` (the copy) | `.cursor/rules/unity-mirror.mdc` |
| Unity-only files, `Samples~`, snippets, `package.json`, `Packages/` | `.cursor/rules/unity-layer.mdc` |
| `c-sharp-chat/PubnubChatApi/PubNubChatApi.Tests/` | `.cursor/rules/tests.mdc` |
| `.pubnub.yml`, `.github/workflows/`, `package.json` version, `UnityChatPNSDKSource.cs` | `.cursor/rules/release-files.mdc` |

Skills in `.claude/skills/`, loaded by both Claude Code and Cursor:

- `sync-unity-chat`: copies the C# Chat into the Unity package, maintains `.meta` files, and lists the Unity-only follow-ups.
- `run-checks`: builds the library and tests, then runs the sync, `ConfigureAwait`, config-asset and Unity compile checks, and reports what was and wasn't run.
- `add-chat-feature`: adds or changes a Chat API end to end, from parity with the other Chat SDKs through tests, the Unity copy, the snippets and the changelog line.
- `update-unity-sdk`: moves to a new `com.pubnub.sdk` (and matching `PubnubPCL`) version and fixes the API changes on both sides.

The PubNub MCP server (`https://mcp.pubnub.com`) is configured for this project in `.cursor/mcp.json` (Cursor) and `.mcp.json` (Claude Code). Each user enables it and signs in with their own PubNub account.
- Use it to look up PubNub and Chat SDK documentation, REST API details and how the other SDKs behave.
- It acts with the signed-in user's account permissions. Don't use its account-changing tools (creating or changing apps, keysets or Functions, publishing messages, deleting data) unless the user explicitly asks.
- Setup help: <https://www.pubnub.com/docs/ai/pubnub-mcp-server>.
