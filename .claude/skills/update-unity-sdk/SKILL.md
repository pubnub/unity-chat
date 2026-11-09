---
name: update-unity-sdk
description: Moves the Unity Chat SDK to a new PubNub Unity SDK (com.pubnub.sdk) version together with the matching PubnubPCL NuGet version, and fixes the resulting API changes in the C# Chat library, the Unity copy and the Unity-only files (UnityListenerFactory, UnityChat). Use when updating the base Unity or C# SDK dependency, when a Unity SDK release changes SubscribeCallbackListener or PubnubUnityUtils, or when the two dependency versions have drifted apart.
---

# Update the base SDK

The Chat compiles against the C# SDK twice: in .NET through the `PubnubPCL` NuGet package, and in Unity through the C# SDK DLL bundled in `com.pubnub.sdk`. Both must be the same C# SDK version. Copy this checklist and track progress:

```text
- [ ] 1. Versions chosen
- [ ] 2. .NET side updated (PubnubPCL)
- [ ] 3. Unity side updated (package.json, dev project lock)
- [ ] 4. Unity-only API changes fixed
- [ ] 5. Synced and verified
- [ ] 6. Changelog line proposed
```

## 1. Choose the versions

- **Find the bundled C# SDK version.** Take the target `com.pubnub.sdk` version from the user, then find the C# SDK version it bundles in one of two ways:
  - the Unity SDK's `.pubnub.yml` changelog ("Updated the base C# SDK to latest version - 9.1.0.")
  - the assembly version of its DLL: `[Reflection.AssemblyName]::GetAssemblyName((Resolve-Path <unity-sdk-repo>/PubNubUnity/Assets/PubNub/Runtime/Plugins/PubnubApiUnity.dll).Path).Version`. Ask the user for the Unity SDK repository path. After step 3, the same DLL is under `unity-chat/PubnubChatUnity/Library/PackageCache/com.pubnub.sdk@*/Runtime/Plugins/`.
- **Pick `PubnubPCL`:** use that exact version. For example, `com.pubnub.sdk` 9.3.0 bundles C# SDK 8.0.1, so `PubnubPCL` is 8.0.1. If NuGet doesn't have that version, ask the user.
- **Read the changelogs** of both SDKs between the current and target versions, and list the breaking changes. Major Unity SDK versions break the Unity layer: 10.0.0 changed the `SubscribeCallbackListener` constructor.

## 2. .NET side

- In `c-sharp-chat/PubnubChatApi/PubnubChatApi/PubnubChatApi.csproj`, set `<PackageReference Include="PubnubPCL" Version="..." />`.
- Run `dotnet build c-sharp-chat/PubnubChatApi/PubnubChatApi.sln`. Fix C# SDK breaking changes in the library without changing the Chat's public API (see `.cursor/rules/chat-library.mdc`).
- The live tests are the main behavioral check; the last base SDK update adjusted several of them. Ask the user whether to run the full suite, because it takes a long time.

## 3. Unity side

- **The package:** set `"com.pubnub.sdk": "<version>"` under `dependencies` in `unity-chat/PubnubChatUnity/Assets/PubnubChat/package.json`.
- **The dev project:** `Packages/manifest.json` takes `com.pubnub.sdk` from the Unity SDK's git HEAD, and `Packages/packages-lock.json` pins the commit in its `hash`. Move the dev project to the target version in one of three ways:
  - **Update the package in the Package Manager.** This only works if the Unity SDK's HEAD is the target release.
  - **Set `hash` in `packages-lock.json`** to the target release's commit.
  - **Pin the revision in the manifest URL** (`...?path=/PubNubUnity/Assets/PubNub#<tag-or-commit>`). This changes how the dev project is set up, so ask the user first.
- **Re-import:** run `Test-UnityCompile.ps1`, which re-resolves packages and re-imports the project. The first run takes several minutes.

## 4. Unity-only API changes

| Unity SDK change | Fix in `unity-chat/PubnubChatUnity/Assets/PubnubChat/Runtime/` |
| --- | --- |
| `SubscribeCallbackListener` constructor (10.0.0 inserted `dataSyncEventCallback` fifth, for 8 parameters) | `UnityListenerFactory.cs`: pass every callback by name, and `dataSyncEventCallback: null` unless the Chat uses it |
| `PubnubUnityUtils.NewUnityPubnub(...)` signature | `UnityChat.cs` |
| `PNConfigAsset` | The `UnityChat.CreateInstance(PubnubChatConfig, PNConfigAsset, string)` overload |
| `UnityPNSDKSource` or `IPNSDKSource` | `UnityChatPNSDKSource.cs` |
| A new listener event the Chat should use | `ChatListenerFactory`, `DotNetListenerFactory` and `UnityListenerFactory` (see `.cursor/rules/async-and-events.mdc`) |
| A higher minimum Unity version | `"unity"` in `package.json`, and possibly `ProjectSettings/ProjectVersion.txt`; ask the user |

## 5. Sync and verify

- **Sync:** if the library changed, run `Sync-UnityChat.ps1` (the `sync-unity-chat` skill).
- **Run the `run-checks` skill.** The Unity compile is mandatory here, because the Unity SDK itself changed.
- **Ask the user to smoke-test in Unity:**
  1. Import the console sample (`Samples~/PubnubChatConfigAsset`) into a scene and enter play mode.
  2. Check that messages and updates arrive.
  3. Make a WebGL build if the Unity SDK's transport changed.

## 6. Changelog

Don't edit versions; the release bot does. Propose "Updated Unity SDK dependency to latest version - X.Y.Z." for the PR description, plus one line per user-visible change, starting with `BREAKING CHANGE:` where it applies.
