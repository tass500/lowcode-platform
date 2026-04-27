<!--
  Használat: másold a repo gyökerébe `pr-body.md` névre (gitignore-olt), töltsd ki, majd:
  gh pr create --base main --head <branch-nev> --title "..." --body-file pr-body.md

  A GitHub webes „New PR” űrlap ugyanezt a struktúrát kapja: lásd .github/PULL_REQUEST_TEMPLATE.md
-->

## Summary

-

## Scope

-

## AI / ügynök + reviewer (gyors)

- [ ] **Scope:** egyezik a `docs/live/03` ACTIVE / felhasználói szándékkal; nincs kapcsolódó „drive-by” szerkesztés
- [ ] **Szerződés / API:** ha változott a nyilvános HTTP, header vagy `errorCode` alak → Summary-ben jelezve (`PROJECT_CONTEXT` ha kell)
- [ ] **Auth / tenant / titok:** ha érintett → **Risk** medium vagy high; diffben / logban nincs titok és PII
- [ ] **Live doc:** `docs/live/02` + `03` frissült, ha a milestone tényei vagy a következő lépések változtak
- [ ] **Teszt / E2E:** lenti gate-ek lefutva (ahogy releváns); jelezd, ha `e2e:seeded` vagy időzítés-érzékeny UI volt
- [ ] **Emberi review:** merge előtt kötelező, ha a kockázat **medium** vagy **high** — [`GOVERNANCE.md`](../GOVERNANCE.md) §4

## Quality gates

- [ ] Frontend: `npm run lint`
- [ ] Frontend: `npm run build`
- [ ] Backend: `dotnet build`
- [ ] Backend: `dotnet test`

## Documentation / review (DoD)

- [ ] `docs/live/02_allapot.md` + `docs/live/03_kovetkezo_lepesek.md` frissítve (ha meaningful változás)
- [ ] Reviewer minimum: scope + kockázat-sáv + téma-doc / `PROJECT_CONTEXT` ha viselkedés változott — [`DOCUMENTATION_EXCELLENCE.md`](../DOCUMENTATION_EXCELLENCE.md)

## Risk / rollout

- Risk level: low / medium / high
- Rollback plan:
  -

## Notes

-
