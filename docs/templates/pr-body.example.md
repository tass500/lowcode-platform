<!--
  Használat: másold a repo gyökerébe `pr-body.md` névre (gitignore-olt), töltsd ki, majd:
  gh pr create --base main --head <branch-nev> --title "..." --body-file pr-body.md

  A GitHub webes „New PR” űrlap ugyanezt a struktúrát kapja: lásd .github/PULL_REQUEST_TEMPLATE.md
-->

## Summary

-

## Scope

-

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
