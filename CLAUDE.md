# Project 51 - working rules (token efficiency)

Keep this file short: it is loaded on every session.

## How to work
- Reuse the existing architecture and make the smallest change that works (Ponytail principles). No new systems without the user's go-ahead.
- Unity state (scenes, prefabs, GameObjects, Inspector, Console, Play Mode, assets): check it live with Unity-MCP, not by guessing from YAML. If no Editor is connected, ask the user to open Unity.
- Architecture/dependency questions: query Graphify (`graphify-out/`) before broad source scans.
- External API/package behaviour (Unity, Photon, PlayFab, DOTween, TMP): check Context7 instead of relying on memory.
- Never call an asset missing before checking the inventory.

## Project skills (.claude/skills)
- `unity-builder`: any scene/prefab/UI hierarchy change (Editor builders, never hand-edited YAML).
- `ui-verify`: before calling any visual change done (Simulator + pixel measurement against the mockup).
- `asset-check`: before declaring an asset missing, and to close every visual task.
- `wrap-up`: end of every round of changes (tests, version bump, backlog, graph refresh, memory).

## Model and effort routing
- Default is `opus` at effort `high` (the user prefers no quality risk over model savings; savings come from context hygiene).
- Trivial edits, renames, lookups: work directly, effort low/medium. No plan mode.
- Multi-file features, multiplayer/networking, root-causing a hard bug: use plan mode first, or delegate to the `deep-reviewer` subagent.
- Searching/reading many files or logs: delegate to the `explorer` subagent (Haiku), don't read them in the main conversation.
- In the desktop app you may adjust session model/effort yourself when the task clearly warrants it; say so in one line.

## Context hygiene
- Auto-compact is set at 500k tokens (see .claude/settings.json). Do not fight it.
- When a task is finished: save anything durable to memory, give a 3-line summary, and tell the user a fresh session/`/clear` is a good idea before an unrelated task.
- Never paste large files or long command output back; grep or read line ranges.

## Usage report
- `python Tools/claude-usage/usage_report.py` compares sessions before/after 2026-09-21 (baseline in `baseline.json`).
