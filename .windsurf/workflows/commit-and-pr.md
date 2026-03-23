---
description: Commit javaslat + PR-branch flow (protected master)
auto_execution_mode: 3
---

# Cél

- Protected `master` miatt közvetlen push nincs; minden változás PR-on keresztül megy.
- Cascade (én) minden jól körülhatárolt milestone végén automatikusan ad egy "Proposed commit" blokkot.
- Cascade (én) milestone-oknál automatikusan ad egy "Proposed PR" blokkot is (mikor érdemes PR-t nyitni + javasolt cím/leírás).
- Te lefuttatod a parancsokat (commit/push/PR).
- Milestone/iteráció lezárásakor mindig frissítjük a live dokumentumokat: `docs/live/03_kovetkezo_lepesek.md`.

# Mikor nyiss PR-t? (cadence)

- PR-t érdemes nyitni, amikor van egy jól körülhatárolt, review-olható egység (milestone).
- Tipikus ritmus: **milestone-onként 1 PR** (vagy ha a diff túl nagy, akkor 2 kisebb PR ugyanazon témán belül).
- Ha a branch már pusholva van GitHubra, a PR-t bármikor meg lehet nyitni (akár Draft-ként), és utána a commitok ugyanabba a PR-ba kerülnek.

# Branch név best practice (ACTIVE iterációból)

- A `docs/live/03_kovetkezo_lepesek.md` fájl **ACTIVE** iterációja legyen a branch név alapja.
  - Példa: `feat/iter-42-workflow-validation-error-details`
- Ez segít kontextusvesztés után is gyorsan visszatalálni, hogy melyik iterációhoz tartozott a munka.

# Credit-alapú fejlesztés (költséghatékonyság)

- Ebben a projektben a kommunikáció/iteráció **credit-alapú**: egy egyszerű kérdés-válasz (pl. "hány óra van?") és egy teljes iteráció (pl. backend+frontend+tesztek) is ugyanannyiba kerülhet.
- Ezért a cél, hogy egy iterációban/menetben **minél több összefüggő, review-olható értéket** csomagoljunk:
  - backend implementáció
  - integrációs tesztek
  - minimál UI/demo template
  - live docs frissítés (`docs/live/03_kovetkezo_lepesek.md`)
  - git parancsok commit/push/PR-hez
- Ha túl kockázatos (sok ismeretlen, nagy refactor, DB migrációk összeakadása, stb.), akkor inkább kisebb szeletekben haladunk.

# "Proposed PR" sablon (amit Cascade használ)

Amikor úgy látom, hogy a változás már egyben review-olható, adok egy blokkot ilyen formában:

```text
Proposed PR
- base: master
- head: <branch-nev>
- title: <rovid-cim>
- summary:
  - <1-3 bullet a változásokról>
- notes:
  - no behavior change
  - checks: várd meg a zöld CI/CodeQL-t
PR létrehozás:
- GitHub UI: https://github.com/tass500/lowcode-platform/pull/new/<branch-nev>
```

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

2. **Változtatások után: live docs frissítés (minden iteráció végén kötelező)**

- Frissítsd a `docs/live/03_kovetkezo_lepesek.md` fájlt az adott iteráció deliverables + demo/példa JSON alapján.
- Ha külön branch/PR kell (pl. már merge-ölve volt a feature PR), csinálj `docs/<tema>` vagy `chore/<tema>` branch-et.

3. **Commit (Cascade javaslata alapján)**

```bash
git status
# (opcionális) git diff

git add <fajlok...>

git commit -m "<uzenet>"
```

4. **Push branch + PR**

```bash
git push -u origin <branch-nev>
```

- GitHub UI-ban: "Compare & pull request" → töltsd ki → Create PR.
- Alternatíva: közvetlen link: `https://github.com/tass500/lowcode-platform/pull/new/<branch-nev>`
- Várd meg, hogy az összes required check zöld legyen.
- Merge (squash/merge policy szerint).

## Ki futtatja a git parancsokat?

- Alapértelmezés: te futtatod a commit/push/PR parancsokat.
- Ha külön kéred, a fejlesztő asszisztens is végigfuttathatja a standard flow-t (branch → commit → push → PR),
  **ugyanazokat a lépéseket követve**, és a végén visszaadva a PR linket.

5. **PR merge után takarítás**

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
