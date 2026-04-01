---
description: Working style (low noise)
---

# Working style (low noise)

## Ground truth (context reset safe)

- Documentation portal: `docs/README.md`
- Authoritative live status + next steps:
  - `docs/live/02_allapot.md`
  - `docs/live/03_kovetkezo_lepesek.md`
- Credit-aware batching rules: `docs/00_workmode.md`
- Minimum quality gates: `docs/01_quality_gates.md`
- Change governance: `docs/GOVERNANCE.md`
- Git / PR workflow: `docs/DEVELOPMENT_WORKFLOW.md` (Windsurf: `.windsurf/workflows/commit-and-pr.md` — nem kanonikus)

If chat context is lost:
- Re-read `docs/live/02_allapot.md` and `docs/live/03_kovetkezo_lepesek.md`
- Continue from the ACTIVE iteration at the top of `docs/live/03_kovetkezo_lepesek.md`

## Iterations (WIP=1)

- Exactly **one** active iteration at a time.
- After an iteration/milestone is implemented:
  - update `docs/live/02_allapot.md` (current/done facts)
  - update `docs/live/03_kovetkezo_lepesek.md` (ACTIVE + deliverables + demo/example JSON)

## Progress tracking

- **Milestone-based** progress: one cohesive feature / improvement slice at a time.
- Each milestone is tracked as **one item** in the shared `todo_list`.
- We avoid noisy updates:
  - set to `in_progress` when started
  - set to `completed` when done (build/smoke OK)

## Commits (optional)

- If/when committing, prefer **one commit per milestone**.
- Keep commit messages short and outcome-oriented.

Examples:
- `Upgrade queue strict policy + UI blocking`
- `Upgrade UI: run/step durations + last refreshed`
