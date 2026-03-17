---
description: Commit javaslat + PR-branch flow (protected master)
---

# Cél

- Protected `master` miatt közvetlen push nincs; minden változás PR-on keresztül megy.
- Cascade (én) minden jól körülhatárolt milestone végén automatikusan ad egy "Proposed commit" blokkot.
- Te lefuttatod a parancsokat (commit/push/PR).

# Standard flow (minden változásnál)

1. **Branch indítás (mindig a legfrissebb masterről)**

```bash
git switch master
git pull --ff-only

git switch -c <branch-nev>
```

Ajánlott branch név minták:

- `chore/<tema>`
- `fix/<tema>`
- `refactor/<tema>`
- `docs/<tema>`

2. **Változtatások után (Cascade javaslata alapján) commit**

```bash
git status
# (opcionális) git diff

git add <fajlok...>

git commit -m "<uzenet>"
```

3. **Push branch + PR**

```bash
git push -u origin <branch-nev>
```

- GitHub UI-ban: "Compare & pull request" → töltsd ki → Create PR.
- Várd meg, hogy az összes required check zöld legyen.
- Merge (squash/merge policy szerint).

4. **PR merge után takarítás**

```bash
git switch master
git pull --ff-only

git branch -d <branch-nev>
```

# Megjegyzés: ha véletlenül masterre commitoltál (és nem tudtál pusholni)

1. Hozz létre egy branch-et a jelenlegi HEAD-ről és pushold azt:

```bash
git branch <branch-nev>
git push -u origin <branch-nev>
```

2. (Opcionális) igazítsd vissza a lokális mastert a remote-hoz, hogy ne maradjon "ahead":

```bash
git switch master
git fetch origin

git reset --hard origin/master
```

Figyelem: a `reset --hard` a lokális masterről eldobja az eltérést, de a commit már a branch-en megvan.
