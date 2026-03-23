# Fejlesztési folyamat és szabályok (authoritative)

**Ezt a fájlt** használjuk elsődleges forrásként a folyamatokra, PR-ra, gate-ekre és kontextusvesztés utáni folytatásra.  
A **`.windsurf/` könyvtár nem authoritative** (lásd alul).

---

## 1) Kétszintű dokumentáció (kevesebb drift)

| Szint | Fájlok | Mikor frissül |
|--------|--------|----------------|
| **Szerződés / világmodell** | `docs/PROJECT_CONTEXT.md` | Csak ha API, port, core invariáns változik |
| **Futás / WIP / iteráció** | `docs/live/02_allapot.md`, `docs/live/03_kovetkezo_lepesek.md` | Minden lezárt milestone / meaningful változás után |
| **Truth template** | `docs/00_truth_files_template/*` | **Soha ne szerkeszd** (read-only sablonok) |

**Kontextusvesztés után (asszisztens / fejlesztő):** olvasd ezt a fájlt, majd `docs/live/03_kovetkezo_lepesek.md`, majd `docs/live/02_allapot.md`.

---

## 2) WIP = 1 és „safe bundling”

- Egyszerre **egy aktív fókusz** (iteráció / milestone), kivéve ha **alacsony kockázatú**, ugyanazon a területen lévő feladatok **biztonságosan** összevonhatók egy körben.
- Ne keverj egy PR-ba: nagy auth változás + DB törés + nagy refaktor.

---

## 3) Definition of Ready (DoR) — mielőtt kódolsz

- Van **ACTIVE** iteráció a `docs/live/03_kovetkezo_lepesek.md`-ben (vagy explicit user scope).
- **Branch név** igazodik az iterációhoz, pl. `feat/iter-43-workflow-lint-ux`.
- **Protected `master`:** közvetlen push a `master`-re nincs; minden változás **PR-on** keresztül.

---

## 4) Definition of Done (DoD) — mielőtt PR-t zársz / mergeolsz

- **Backend:** `dotnet test backend/LowCodePlatform.Backend.Tests/LowCodePlatform.Backend.Tests.csproj`
- **Frontend:** `npm run build` (cwd: `frontend/`)
- Ha érinti a megosztott utilt / logikát: célszerű **`npx ng test --watch=false --browsers=ChromeHeadless`**
- **`docs/live/02_allapot.md`** és **`docs/live/03_kovetkezo_lepesek.md`** frissítve (kész pipák, következő ACTIVE).
- PR leírásban: **rövid összegzés + test plan** (pontos parancsok).

---

## 5) Branch, commit, PR

- Branch mindig a legfrissebb **`master`**-ről: `git switch master && git pull --ff-only`, majd `git switch -c feat/...`
- **Ritmus:** tipikusan **1 PR = 1 milestone**; ha a diff túl nagy, **2 kisebb PR** ugyanabban a témában.
- **Commitok:** érdemes 1–3 review-barát commit (backend+teszt / frontend / docs+chore).
- PR megnyitható **Draft**-ként is; a további commitok ugyanabba a PR-ba mennek.
- Merge után: lokális feature branch **törölhető** (`git branch -d`).

---

## 6) GitHub CLI (`gh`) és token

- PR nyitás: GitHub web **Compare & pull request**, vagy `gh pr create` (ha telepítve és bejelentkezve).
- Ha `Resource not accessible by personal access token`: a tokennek legyen **repo** / megfelelő **fine-grained** jog a repóra (Contents + Pull requests).
- **Teljes útvonal** (ha `gh` nincs a PATH-ban):  
  `& "C:\Program Files\GitHub CLI\gh.exe" ...`

---

## 7) Handoff blokk (új chat / kontextus reset)

Másold be a `docs/live/03_kovetkezo_lepesek.md` tetejére vagy a PR-ba:

```text
Handoff
- Repo: lowcode-platform
- ACTIVE iteráció: (másold be a 03 fájlból)
- Branch: <branch-nev>
- Utolsó zöld gate: dotnet test ... ; npm run build ...
- Nyitott PR: <link vagy nincs>
- Következő lépés: (1 mondat a 03-ból)
```

---

## 8) Minőség, biztonság, stílus (rövid)

- **SOLID / DRY**, meglévő repo stílus; **szerződés** (`ErrorResponse`, API-k) ne törjön jóváhagyás nélkül.
- **Ne logolj** titkokat, tokeneket, jelszavakat, PII-t.
- **Új NuGet/npm dependency** csak **explicit jóváhagyással**.
- **Refaktor:** alapból **no behavior change**, kivéve ha a scope azt kéri.

---

## 9) Cursor szabályfájlok (`.cursor/rules/*.mdc`)

- Rövid, **automatikus** emlékeztetők; a **teljes folyamat** ebben a fájlban van (`DEVELOPMENT_WORKFLOW.md`).
- Kontextusvesztéskor ne csak a `.mdc`-ket olvasd — **ez a fájl az elsődleges hosszú leírás**.

---

## 10) Modellválasztás (nem kötelező, tanács)

A Cursor UI-ban állítod. Irányelv: **Auto** a napi kis munkára; **erősebb modell** architektúra / nagy refaktorra; **hosszú kontextus** nagy log / doksi elemzésre; **drága** top modell csak ha kifejezetten kell.

---

## 11) Karbantartási sweep (opcionális)

Nagyobb backend+frontend milestone előtt / után: `dotnet build` + `npm run build`; ismétlődő minták (pl. error mapping duplikáció) — ha **3+** előfordulás, javasolj kis helper refaktort.

---

## 12) `.windsurf/` könyvtár

**Nem authoritative.** A folyamat és a szabályok innen: **`docs/DEVELOPMENT_WORKFLOW.md`** + **`docs/live/*`**.  
A `.windsurf/` megtartható történeti okból; új szabályt **ne** oda írj.
