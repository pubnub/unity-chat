---
name: add-chat-feature
description: Adds or changes a Unity Chat SDK feature end to end, covering parity with the JavaScript, Kotlin and Swift Chat SDKs, the C# Chat implementation (ChatOperationResult, listener-factory events, config options), Unity-only follow-ups, live tests, the sync into the Unity package, docs snippets and the changelog line. Use when implementing a new Chat method, event, entity or config option, porting a feature from another Chat SDK, or changing existing Chat behavior.
---

# Add a Chat feature

Copy this checklist and track progress:

```text
- [ ] 1. Contract understood (other Chat SDKs, docs)
- [ ] 2. Implemented in the C# Chat library
- [ ] 3. Events go through the listener factory
- [ ] 4. Config option and Unity-only follow-ups
- [ ] 5. Tests
- [ ] 6. Synced into the Unity package (sync-unity-chat skill)
- [ ] 7. Snippets
- [ ] 8. Checks pass (run-checks skill)
- [ ] 9. Changelog line proposed
```

Paths are relative to `c-sharp-chat/PubnubChatApi/PubnubChatApi/` unless they start with `c-sharp-chat/` or `unity-chat/`.

## 1. Understand the contract

Chat features usually exist in the JavaScript Chat SDK first, and in the Kotlin and Swift ones. All of them must read each other's data.

- **Sources:** ask the user for the reference implementation or design doc if they didn't give one, and use the PubNub MCP server or <https://www.pubnub.com/docs/chat> for the public behavior.
- **Write down the contract:**
  - the public API: names, parameters, defaults and return values, mapped to C# naming (`PascalCase`, `Task<ChatOperationResult<T>>`)
  - the PubNub calls underneath: App Context, publish, message actions, presence, history, files, push
  - the wire data: payload fields, event `type` strings, message action types, custom-data keys, internal channel IDs
  - error cases, and which behaviors are intentionally SDK-specific
- **Don't guess wire names.** If the sources don't settle one, ask.

## 2. Implement in the C# Chat library

- **Where it goes:**
  - chat-wide calls by ID go on `Entities/Chat.cs`
  - entity calls go on `Channel`, `User`, `Membership` or `Message`, with overrides in `ThreadChannel` and `ThreadMessage` when threads behave differently
  - inputs and multi-value results go in `Entities/Data/`
  - enums go in `Enums/` (append members, and add both string mappings to `Utilities/ChatEnumConverters.cs`)
  - payload parsing goes in `Utilities/ChatParsers.cs`
- **The shape** (see `.cursor/rules/chat-library.mdc`):

```csharp
/// <summary>
/// Sets a topic for this channel, stored in its custom data.
/// </summary>
/// <param name="topic">The new topic. Must not be empty.</param>
/// <returns>A ChatOperationResult indicating the success or failure of the operation.</returns>
public async Task<ChatOperationResult> SetTopic(string topic)
{
    var result = new ChatOperationResult("Channel.SetTopic()", chat);
    if (string.IsNullOrEmpty(topic))
    {
        result.Error = true;
        result.Exception = new PNException("Topic can't be empty.");
        return result;
    }
    var getData = await GetChannelData(chat, Id).ConfigureAwait(false);
    if (result.RegisterOperation(getData))
    {
        return result;
    }
    ChatChannelData data = getData.Result;
    data.CustomData ??= new Dictionary<string, object>();
    data.CustomData["topic"] = topic;
    var update = await UpdateChannelData(chat, Id, data).ConfigureAwait(false);
    if (result.RegisterOperation(update))
    {
        return result;
    }
    UpdateLocalData(update.Result);
    return result;
}
```

- **Language and APIs:** C# 9, .NET Standard 2.1, only C# SDK APIs present in the version bundled with the Unity SDK dependency, and no new packages.
- **Awaits:** every `await` ends with `.ConfigureAwait(false)`.
- **Changing an existing public member:** add an overload, or keep the old one as an `[Obsolete("Please use X instead")]` wrapper that still works. Search the tests, snippets, samples and Unity-only files for callers.
- **Publishing a chat event:** use `chat.EmitEvent(type, channelId, jsonPayload)`.
  - It strips the outer braces of `jsonPayload` and appends `"type"`, so pass a JSON object, built from a dictionary with `chat.PubnubInstance.JsonPluggableLibrary.SerializeToJsonString(...)`.
  - `typing` and `receipt` events are never stored; `custom` ones are stored only when `storeInHistory` is set.

## 3. Events

- **Declare the event:** `public event Action<T> OnXxx;`, with a summary ending "Call StreamXxx(true) to enable this callback."
- **Add the stream method:** a `private Subscription xxxSubscription;` field and `public void StreamXxx(bool stream)`, copied from `Channel.StreamCustomEvents` (message-based) or `ChatEntity.StreamUpdates` (App Context updates):
  - subscribe with `SetListening(ref xxxSubscription, SubscriptionOptions.None, stream, channelId, chat.ListenerFactory.ProduceListener(...))`
  - in the callback, parse with `ChatParsers.TryParseEvent(chat, m, PubnubChatEventType.X, out var chatEvent)`, raise `OnXxx` synchronously, then call `chat.BroadcastAnyEvent(chatEvent)`
- **Main thread:** don't raise the event after an `await` or from a timer; in Unity that runs on a thread-pool thread. If the feature needs it, ask the user (see `.cursor/rules/async-and-events.mdc`).
- **A new event type:**
  - append it to `PubnubChatEventType`
  - add both cases to `ChatEnumConverters`; `StringToEventType` throws on unknown strings, and `TryParseEvent` drops those events
  - use the other SDKs' `type` string
- **A callback the listener factory doesn't offer** changes `ChatListenerFactory`, `DotNetListenerFactory` and the Unity-only `UnityListenerFactory`.

## 4. Config option and Unity-only follow-ups

- **A new `PubnubChatConfig` option:**
  1. Append a constructor parameter **at the end**, with a default value, because callers pass the first parameters positionally.
  2. Add a get-only property, and assign it in the constructor.
  3. Add `[field: SerializeField] public T Name { get; private set; } = <same default>;` to `unity-chat/PubnubChatUnity/Assets/PubnubChat/Samples~/PubnubChatConfigAsset/PubnubChatConfigAsset.cs`, and pass it by name in its implicit operator.
  4. Run `powershell -ExecutionPolicy Bypass -File .claude/skills/run-checks/scripts/Compare-ChatConfigAsset.ps1`.

  Group nested settings in a `[System.Serializable]` nested class, as `PushNotificationsConfig` is.
- **Other Unity-only files:** step 3 of the `sync-unity-chat` skill lists what else needs a Unity-side change (`UnityChat.cs` for `CreateInstance` changes, `UnityListenerFactory.cs`, snippets and samples).

## 5. Tests

Add tests to the matching fixture in `c-sharp-chat/PubnubChatApi/PubNubChatApi.Tests/` (see `.cursor/rules/tests.mdc`). Cover the success path, the error result and the event, if there is one.

All tests are live, so build them with `dotnet build c-sharp-chat/PubnubChatApi/PubnubChatApi.sln`, but run them only if the user asks: `dotnet test c-sharp-chat/PubnubChatApi/PubNubChatApi.Tests/PubNubChatApi.Tests.csproj --filter "FullyQualifiedName~<Fixture>.<Test>"`.

## 6. Sync into the Unity package

Follow the `sync-unity-chat` skill: run `Sync-UnityChat.ps1`, then do the Unity-only follow-ups.

## 7. Snippets

User-facing APIs get a docs snippet in `unity-chat/PubnubChatUnity/Assets/Snippets/`.

- **Where:**
  - Add to the feature's existing `<Feature>Sample.cs`, or create a new one with the same layout: a `// snippet.using` block, a `static Chat chat` initialized in `Init()` with `UnityChat.CreateInstance(...)` and `"demo"` keys, then one `public static` method per example.
  - A new file needs its `.meta`, which Unity creates during `Test-UnityCompile.ps1`.
- **Markers** are `snake_case` (`// snippet.append_text_example` ... `// snippet.end`). The docs team references them by name, so ask the user for names when the docs page already exists, and never rename existing ones.
- **Changed APIs:** update every snippet that uses an API you changed.

## 8. Verify

Follow the `run-checks` skill. The Unity compile step matters for any public API change, because the snippets and the Unity-only files only compile there.

## 9. Changelog

Don't edit `.pubnub.yml`, the `package.json` version or `UnityChatPNSDKSource.cs`; the release bot writes them. Propose the entry for the PR description in the `.pubnub.yml` style, for example `type: feature`, `text: "Added Channel.SetTopic() for storing a channel topic."`. Breaking changes start with `BREAKING CHANGE:`. In your summary, also list any differences from the other Chat SDKs, with the reason for each.
