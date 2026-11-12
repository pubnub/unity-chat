# CLAUDE.md

@AGENTS.md

## Claude Code notes

- **Where guidance goes:** put guidance that applies to all agents in `AGENTS.md`. Keep only Claude Code-specific notes in this file.
- **Area rules:** Claude Code doesn't attach `.cursor/rules/*.mdc` automatically. Before editing an area, read the rule listed for it in the "Where to find more" table in `AGENTS.md`.
- **Skills** in `.claude/skills/`: `sync-unity-chat`, `run-checks`, `add-chat-feature` and `update-unity-sdk`. Use `run-checks` before reporting a code change as done.
- **Permissions** (`.claude/settings.json`):
  - Pre-approved: `dotnet build`, `dotnet restore`, the skill scripts (including the sync, which writes the Unity copy) and read-only git commands. `dotnet test` isn't pre-approved, because every test runs against a live keyset.
  - Blocked: hand edits to the Unity copy (`Runtime/PubnubChatApi/`; change the C# Chat and run the sync script), and edits to Unity's generated folders.
  - Personal overrides go in `.claude/settings.local.json` and personal notes in `CLAUDE.local.md`; both are git-ignored.
- **PubNub MCP server:** `.mcp.json` defines it at project scope. Approve it when Claude Code asks, then run `/mcp`, select `pubnub` and authenticate.
- **Shell:** the scripts are PowerShell. Use `powershell -ExecutionPolicy Bypass -File <script>` on Windows, or `pwsh -File <script>` on macOS and Linux.
- **Unity:** `Test-UnityCompile.ps1` runs the Unity Editor in batch mode and can take minutes. If no editor is installed, ask the user to open `unity-chat/PubnubChatUnity` in Unity and check the Console, and say what you couldn't verify.
