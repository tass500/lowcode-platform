---
description: Keep docs/live updated (context-reset safe)
---

# Goal
Keep a small, authoritative “ground truth” inside the repo so progress survives chat context resets.

# Files (ground truth)
- `docs/live/02_allapot.md` (Current state / done)
- `docs/live/03_kovetkezo_lepesek.md` (Next steps / WIP)

# Policy
1. After every completed implementation (feature/fix), update these two files automatically.
2. Do not ask for confirmation to update them.
3. Keep updates short and factual.

# Update rules
## `docs/live/02_allapot.md`
- Add or adjust only “done/current” facts.
- Prefer small bullets.
- If behavior changed (e.g. button semantics), capture the new invariant.

## `docs/live/03_kovetkezo_lepesek.md`
- Keep it actionable and short.
- When a milestone becomes done, mark it `✅ Kész`.
- Avoid long explanations; link to file/function names if needed.

# When context resets
If the conversation context usage drops/reset happens:
1. Re-read both files.
2. Treat them as ground truth for what is done and what is next.
3. Continue from `03_kovetkezo_lepesek.md`.
