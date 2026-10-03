---
name: wrap-up
description: Use at the end of every round of code/scene changes delivered to the user in project 51 - bump version, run tests, update SPRINT_BACKLOG.md, refresh the Graphify graph, save memory. Also when the user says "chiudi", "fine giro", or asks for a summary of a finished task.
---

# End-of-round checklist (project 51)

Once per round delivered to the user, not per file.

1. **Compile clean**: `refresh_unity` + `read_console` with 0 errors (Unity-MCP; needs the Editor open).
2. **Tests**: `run_tests` EditMode (plus the relevant PlayMode/runtime checks). Report the real counts and any failure verbatim.
3. **Version bump**: `ProjectSettings/ProjectSettings.asset` field `bundleVersion:` +0.01
   (2.06 -> 2.07). It shows at startup via `Application.version`.
4. **Backlog**: update `SPRINT_BACKLOG.md`: item states (☐ ◐ ☑ ⏸ ❓) and the `PUNTO DI RIPRESA`
   block at the top (version, what's done, what's next). `docs/archivio/` (old roadmap, UIV2-era plans) is frozen history: do not update it.
5. **Deferred items**: list every part of the request not done, one per line, each needing explicit confirmation.
   Never hide them inside a summary.
6. **Visual work**: include the `asset-check` report.
7. **Graph refresh** (if C# files changed):
   run it from the PowerShell tool (from Bash it segfaults, exit 139):
   `$env:PYTHONIOENCODING='utf-8'; & "$env:APPDATA/uv/tools/graphifyy/Scripts/python.exe" -m graphify update . --force`
8. **Memory**: save only durable, non-obvious facts. Then give a 3-line summary and suggest `/clear` before an unrelated task.

Do not commit or push unless asked.
