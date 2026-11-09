---
name: sync-unity-chat
description: Copies the C# Chat library (c-sharp-chat/PubnubChatApi/PubnubChatApi) into the Unity Chat package (Assets/PubnubChat/Runtime/PubnubChatApi), creates and deletes the matching .meta files, and walks through the Unity-only follow-ups (UnityListenerFactory, UnityChat, PubnubChatConfigAsset, snippets). Use after any change to the C# Chat library, when the Unity copy is reported out of sync, or when resolving merge conflicts in copied files.
---

# Sync the Unity copy

Unity compiles a copy of the C# Chat library's `.cs` files. The copy must match the library file for file, and every copied file and folder needs a `.meta`. Copy this checklist and track progress:

```text
- [ ] 1. The library builds
- [ ] 2. Copy synced
- [ ] 3. Unity-only follow-ups done
- [ ] 4. The Unity project compiles
- [ ] 5. Committed together
```

## 1. Build the library

```bash
dotnet build c-sharp-chat/PubnubChatApi/PubnubChatApi.sln
```

Fix errors first. Syncing broken code only moves the error into Unity.

## 2. Sync

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/sync-unity-chat/scripts/Sync-UnityChat.ps1
```

- **What it prints:** added, updated and deleted files, plus created and deleted `.meta` files. "OK: already in sync" means there was nothing to do.
- **`-Check`** only reports, and exits with 1 on drift. The `run-checks` skill uses it.
- **`DIFFERS` on a file you didn't change** means someone edited the copy by hand. Find out which side is right before syncing, because a sync overwrites the copy. Move a valid edit into the C# Chat file, and ask the user when it's unclear.
- **`.meta` files:** new ones get random GUIDs, and existing ones are never rewritten. A rename is a delete plus an add, so the file gets a new GUID. That is harmless for library scripts, since no asset references them.

## 3. Unity-only follow-ups

The .NET build can't see these files. Review the library diff (`git diff -- c-sharp-chat/PubnubChatApi/PubnubChatApi`) against this table:

| If the change touched | Update |
| --- | --- |
| `ChatListenerFactory.ProduceListener` | `Utilities/DotNetListenerFactory.cs` in the library, and `unity-chat/PubnubChatUnity/Assets/PubnubChat/Runtime/UnityListenerFactory.cs` |
| `PubnubChatConfig` constructor parameters or defaults | `Samples~/PubnubChatConfigAsset/PubnubChatConfigAsset.cs`, then run `.claude/skills/run-checks/scripts/Compare-ChatConfigAsset.ps1` |
| `Chat.CreateInstance` overloads | `Runtime/UnityChat.cs` |
| Any public member that was renamed, re-signed or obsoleted | Its uses in `Assets/Snippets/`, `Samples~/` and `Runtime/*.cs` |

To find uses of a member: `rg -n "MemberName" unity-chat/PubnubChatUnity/Assets/Snippets unity-chat/PubnubChatUnity/Assets/PubnubChat/Samples~ unity-chat/PubnubChatUnity/Assets/PubnubChat/Runtime --glob "!**/PubnubChatApi/**"`.

## 4. Compile in Unity

```powershell
powershell -ExecutionPolicy Bypass -File .claude/skills/run-checks/scripts/Test-UnityCompile.ps1
```

- **Requirements:** the Unity 2022.3.57f1 editor, and the project closed in the Editor. Without them, ask the user to open `unity-chat/PubnubChatUnity` and check the Console, and report the check as not run.
- **`.meta` files:** the script also makes Unity create `.meta` files for new assets outside the copy (snippets, Unity-only files); commit them.
- **`Samples~/`** isn't compiled. If you changed a sample, check it as described in `.cursor/rules/unity-layer.mdc`.

## 5. Commit

Put the library change, the copy, the new and deleted `.meta` files and the Unity-only follow-ups in the same commit or PR. Run `Sync-UnityChat.ps1 -Check` last; it must print OK.
