## Summary

- 

## Scope

- 

## AI / agent + reviewer (quick)

- [ ] **Scope:** matches `docs/live/03` ACTIVE / stated intent; no unrelated drive-by edits
- [ ] **Contract / API:** if public HTTP, headers, or `errorCode` shape changed, called out in Summary (and `PROJECT_CONTEXT` if needed)
- [ ] **Auth / tenant / secrets:** if touched → set **Risk** to medium or high; no secrets or PII in diff or logs
- [ ] **Live docs:** `docs/live/02` + `03` updated when milestone facts or next steps changed
- [ ] **Tests / E2E:** gates below run as applicable; note if `e2e:seeded` or timing-sensitive UI was exercised
- [ ] **Human review:** required before merge when risk is **medium** or **high** (`docs/GOVERNANCE.md` §4)

## Quality gates

- [ ] Frontend: `npm run lint`
- [ ] Frontend: `npm run build`
- [ ] Backend: `dotnet build`
- [ ] Backend: `dotnet test`

## Documentation / review (DoD)

- [ ] `docs/live/02_allapot.md` + `docs/live/03_kovetkezo_lepesek.md` updated if meaningful change
- [ ] Reviewer minimum: scope, risk tier, docs — see `docs/DOCUMENTATION_EXCELLENCE.md`

## Risk / rollout

- Risk level: low / medium / high
- Rollback plan:
  - 

## Notes

- 
