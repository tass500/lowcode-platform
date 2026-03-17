---
description: Periodic maintenance sweep (drift-proof)
---

# Goal
Run a quick, repeatable health check that surfaces emerging noise/complexity early.

# When to run
- After a milestone touching both backend + frontend
- Before merging a larger set of changes
- Any time you feel the Upgrade/Observability area is getting “busy”

# Steps

1. Backend compile gate
   - Run: `dotnet build`
   - CWD: `backend/`

2. Frontend compile gate
   - Run: `npm run build`
   - CWD: `frontend/`

3. Fast "duplication / drift" scan (no code changes)
   - Search for repeated patterns that should be centralized:
     - `e?.error?.message ??` (error mapping duplication)
     - `observe: 'response'` (header capture patterns)
     - `applyServerTimeUtc(` (time sync calls)
   - If a pattern is repeated 3+ times, propose a small helper and refactor only that.

4. Upgrade page complexity check
   - Open: `frontend/src/app/pages/upgrade-page.component.ts`
   - Confirm:
     - no leftover deprecated flags (e.g. old `isBlocked`)
     - related state + actions are near each other (polling, exports, debug pack)

5. Backend headers sanity
   - Confirm `NoStoreNoCacheAttribute` still sets:
     - `Cache-Control`, `Pragma`, `Expires`
     - and drift-proof headers `X-LCP-Server-*`

# Output
- If everything passes: note "maintenance sweep OK".
- If issues found: create 1 small milestone per fix (avoid big-bang refactors).
