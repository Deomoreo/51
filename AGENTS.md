# Project 51 - working rules (token efficiency)

Keep this file short: it is loaded on every session.

## Model and effort routing
- Default is `opus` at effort `high` (the user prefers no quality risk over model savings; savings come from context hygiene).
- Trivial edits, renames, lookups: work directly, effort low/medium. No plan mode.
- Multi-file features, multiplayer/networking, root-causing a hard bug: use plan mode first, or delegate to the `deep-reviewer` subagent.
- Searching/reading many files or logs: delegate to the `explorer` subagent (Haiku), don't read them in the main conversation.
- In the desktop app you may adjust session model/effort yourself when the task clearly warrants it; say so in one line.

## Context hygiene
- Auto-compact is set at 150k tokens (see .Codex/settings.json). Do not fight it.
- When a task is finished: save anything durable to memory, give a 3-line summary, and tell the user a fresh session/`/clear` is a good idea before an unrelated task.
- Never paste large files or long command output back; grep or read line ranges.

## Usage report
- `python Tools/Codex-usage/usage_report.py` compares sessions before/after 2026-09-21 (baseline in `baseline.json`).
