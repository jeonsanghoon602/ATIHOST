# ATIHOST Agent Instructions

## Read first

- Read `README.md`, `DEVELOPMENT_STEPS.md`, and the relevant GitHub Issue before changing files.
- Use `AGENT_WORK_REQUEST_TEMPLATE.md` when requirements or acceptance criteria are incomplete.
- Treat the equipment Interface Specification as the authority for SECS-II/GEM message details.

## Technical constraints

- Target C# projects to .NET Framework 4.7.2 and WPF/MVVM.
- Keep all projects on the same target framework and explicitly select x86 or x64.
- If Secs4Net is used, pin version 2.4.4. Do not upgrade to 3.x.
- Keep the communication SDK behind a Protocol Adapter; UI code must not call it directly.
- New C# and documentation files use UTF-8.
- Existing BCB `.cpp`, `.h`, and `.dfm` files remain CP949/Windows-949. Never rewrite or normalize their Korean text as UTF-8.

## Scope and safety

- Make the smallest change that fully satisfies the Issue and its acceptance criteria.
- Do not add speculative features, broad refactors, or dependency upgrades.
- Do not guess unspecified CEID, RPTID, VID, ALID, RCMD, data types, timers, or state transitions.
- Test with an Equipment Simulator before any real equipment test.
- Never control production equipment or use production data without explicit human approval.
- Do not commit credentials, production IPs, equipment secrets, or sensitive logs.

## Workflow

- Use one Issue per HOST task and one focused branch/worktree per Issue.
- Avoid concurrent edits to the same file, shared interface, or state machine.
- Keep implementer and final reviewer separate.
- Follow the branch prefix required by the active tool; Codex branches default to `codex/`.
- Do not merge until the Issue acceptance criteria, review, and required simulator tests pass.

### Codex-Claude A2A workflow

- Claude writes and fixes application code, tests, and implementation documentation. Codex coordinates requirements and must not substitute its own implementation when Claude is unavailable.
- After each Claude implementation or fix, Codex independently reviews the diff and runs the required Debug/Release builds and relevant tests.
- Codex automatically sends actionable review findings back to Claude. Repeat the Claude fix and Codex review cycle until no required findings remain.
- After Codex verification passes for an authorized Issue, Claude creates the commit, pushes the branch, and creates or updates the Pull Request.
- Codex verifies the resulting commit and Pull Request. Neither agent merges without explicit human approval.

## Completion report

- List changed files and why they changed.
- Report Debug/Release build and relevant test results.
- State unverified conditions, remaining risks, and equipment-test requirements.
- Preserve unrelated user changes and never discard them without approval.
