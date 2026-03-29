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

### 5a) Több iteráció egy feature ágon — alapértelmezett ritmus (emberek + AI)

A roadmap **iterációi** és a **PR-ok** nem feltétlenül 1:1-ben esnek egybe. **Optimalizált alapértelmezés** ebben a repóban:

- **Egy PR = egy lezárható milestone** (egy üzletileg / technikailag összefüggő egység). **Nem** cél: a teljes backlog egy PR-ban; **nem** kötelező minden apró szelethez külön PR.
- **Ugyanazon a feature ágon** több roadmap-iteráció is végrehajtható **egymás után**: iterációnként (vagy rétegenként) **külön commit**, majd **egy PR**, amikor a milestone **kész** és a diff még **ésszerűen reviewolható**.
- **Commit-sorrend** ugyanazon a PR-on belül: tipikusan **iterációnként 1 commit**; ha egy iteráció több réteget érint, marad a **1–3 commit / egység** minta (backend+teszt → frontend → docs+chore).
- **Vágd több PR-ra**, ha:
  - a diff **túl nagy** ahhoz, hogy jó review jöjjön belőle;
  - **keverednek a témák** (pl. nagy auth / biztonság + független feature);
  - **eltérő kockázat** indokolja a szétválasztást (példa: **import/export** és **auth bővítés** külön PR).
- **Kerülendő:** hetek vagy hónapok munkája egyetlen PR-ban; egy PR-ban **auth** + össze nem függő nagy feature.

**AI / Cursor:** ha a felhasználó **nem ad másik** utasítást, ezt a ritmust kövesse (egy milestone ↔ egy branch, több commit, egy PR a milestone lezárásakor; szétvágás a fenti feltételek szerint). **Explicit kérés** felülírja (pl. „minden iteráció külön PR”, „csak egy commit az egészre”).

**Megjegyzés:** A szkriptek (`iter-end`, `gh-pr-push-merge`) **nem** döntenek helyettünk milestone vs. PR szétvágásról — a fenti szabályt a **doksi + review** rögzíti; a szkriptek a már eldöntött ágat viszik végig (push, PR, gate-ek).

---

## 6) GitHub CLI (`gh`) és token

- PR nyitás: GitHub web **Compare & pull request**, vagy `gh pr create` (ha telepítve és bejelentkezve).
- Ha `Resource not accessible by personal access token`: a tokennek legyen **repo** / megfelelő **fine-grained** jog a repóra (Contents + Pull requests).
- **Teljes útvonal** (ha `gh` nincs a PATH-ban):  
  `& "C:\Program Files\GitHub CLI\gh.exe" ...`
- **Nem interaktív / CI / másik gép:** állítsd a **`GITHUB_TOKEN`** vagy **`GH_TOKEN`** környezeti változót (ugyanazok a jogok). A repóban **ne** commitolj tokent — lokálisan másold a **`.env.example`** fájlt **`.env`** névre, töltsd ki, a **`.env` gitignore-olt** (lásd `.gitignore`). A `scripts/gh-pr-push-merge.*` szkriptek opcionálisan beolvassák a `.env`-et.

**Perzisztens bejelentkezés (tipikusan Windows):**

- **`gh auth login`** egyszer (HTTPS ajánlott) → a token a **Windows Credential Manager**-ben marad (`gh auth status` → `keyring`); újraindítás után is érvényes.
- **Git push/pull HTTPS-sel:** futtasd **`gh auth setup-git`** (ha még nem), hogy a GitHub a **`gh auth git-credential`** helperrel hitelesítsen — ne duplikáld külön PAT-tal a `git config`-ban, ha a `gh` kezeli.
- **Gyors teszt:** `gh auth status` → `gh pr list` (üres lista is OK) vagy `git ls-remote origin`.

**PR szöveg asszisztens / CLI-ból:** sablon: **`docs/templates/pr-body.example.md`** (másold ki, töltsd, pl. `pr-body.md` a repo gyökerében, gitignore-olható). Push után például:  
`gh pr create --base master --head <branch> --title "..." --body-file pr-body.md`  
(vagy `--body "..."`). A Cursor asszisztens a commit diff + `docs/live` alapján tud szöveget generálni; a parancsot csak akkor futtasd, ha `gh` elérhető és be vagy jelentkezve. Webes PR: **`.github/PULL_REQUEST_TEMPLATE.md`** ugyanez a struktúra.

### 6a) Automatizált push → PR → CI várakozás → merge (opcionális)

**Cél:** egy feature ágon (commitokkal) végigvinni ugyanazt a folyamatot, mint manuálisan: `git push`, nyitott PR újrahasználása vagy `gh pr create`, `gh pr checks --watch`, `gh pr merge`, lokálisan `master` frissítése.

| Fájl | Platform |
|------|-----------|
| `scripts/gh-pr-push-merge.ps1` | Windows (PowerShell) |
| `scripts/gh-pr-push-merge.sh` | Linux / macOS / Git Bash |
| `scripts/iter-end.ps1` | Windows: **DoD gate-ek** (`dotnet test`, `npm run build`) + utána `gh-pr-push-merge` |
| `scripts/iter-end.sh` | Unix: ugyanígy (`SKIP_TESTS=1` / `SKIP_FRONTEND=1` opcionális) |

**PR szöveg fájlból:** `gh-pr-push-merge` elfogad **`-BodyFile path`**; ha nincs megadva, de létezik a repo gyökerében **`pr-body.md`**, azt használja. A bash változat: **`BODY_FILE`** env + opcionális **`pr-body.md`**.

**Feltételek:**

- `gh` telepítve, bejelentkezve (`gh auth login`) **vagy** `GH_TOKEN` / `GITHUB_TOKEN` + `.env`.
- Nem állsz a **`master`** ágon; a változtatások commitolva vannak.
- A GitHub **branch protection** / kötelező review **nem** blokkolja a merge-et (különben a script hibázhat vagy figyelmeztet).

**Példa (Windows):**

```powershell
git switch master; git pull --ff-only origin master
git switch -c feat/my-topic
# ... kódolás, commit ...
.\scripts\gh-pr-push-merge.ps1              # teljes folyamat
.\scripts\gh-pr-push-merge.ps1 -NoMerge     # csak push + PR, merge kézzel
.\scripts\gh-pr-push-merge.ps1 -Squash        # squash merge
```

**Példa (bash):**

```bash
chmod +x scripts/gh-pr-push-merge.sh
NO_MERGE=1 ./scripts/gh-pr-push-merge.sh   # csak push + PR
./scripts/gh-pr-push-merge.sh
```

**Megjegyzés:** Cursor / asszisztens ugyanezt a folyamatot tudja futtatni **csak akkor**, ha a környezetben elérhető a `gh` és megfelelő jogosultság van — ez dokumentáció + szkript, nem kötelező „magától” merge-elni.

### 6b) Iteráció végén — egy menetben (ajánlott sorrend)

1. Frissítsd **`docs/live/02_allapot.md`** és **`docs/live/03_kovetkezo_lepesek.md`** (kész pipák, következő ACTIVE).
2. Állítsd össze a PR leírást: **`docs/templates/pr-body.example.md`** → másold **`pr-body.md`**-nek a repo gyökerébe (gitignore-olt).
3. Commit + feature ágon maradva: **`.\scripts\iter-end.ps1`** (vagy `-NoMerge` / `-SkipTests` ha szükséges).  
   Ez lefuttatja a gate-eket, majd push → PR → `gh pr checks --watch` → merge → lokális **`master`** pull.

```powershell
.\scripts\iter-end.ps1
.\scripts\iter-end.ps1 -NoMerge
.\scripts\iter-end.ps1 -BodyFile .\pr-body.md
```

```bash
chmod +x scripts/iter-end.sh
./scripts/iter-end.sh
SKIP_TESTS=1 NO_MERGE=1 ./scripts/iter-end.sh
```

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
- Kontextusvesztéskor ne csak a `.mdc`-ket olvasd — **ez a fájl az elsődleges hosszú leírás**; AI-használathoz lásd még **§10**.

---

## 10) AI asszisztens (Cursor): modell + keret-takarékos használat

A Cursor **Pro** „Auto + Composer” (és hasonló) funkciói **token-alapon** számolódnak a ciklusban. A repó folyamata úgy van kialakítva, hogy **kevesebb felesleges kontextussal** is működjön — ezt érdemes a chatben is tartani.

### 10a) Modellválasztás (UI)

A Cursor UI-ban állítod. Irányelv: **Auto / gyorsabb modell** a napi kis munkára; **erősebb modell** architektúra / nagy refaktorra; **hosszú kontextus** nagy log / doksi elemzésre; **drága** top modell csak ha kifejezetten kell.

### 10b) Kontextus — mitől fogy kevesebb token (ajánlott szokások)

- **Egy szál = egy fókusz:** egy iteráció / egy PR / egy konkrét bug; ne keverd össze a teljes roadmapot egy üzenetben.
- **Hosszú szál → új chat:** ha a thread már több témát / milestone-t kever, vagy csak a „összefoglaló + következő lépés” kellene, nyiss **újat** Handoff-fal — olcsóbb, mint újraküldeni a teljes előzményt.
- **Minimális @ hivatkozás:** csak azokat a fájlokat / mappákat add meg (`@path`), amik **közvetlenül** kellenek a feladathoz — ne az egész repót; a `@Codebase` / teljes workspace keresés csak akkor, ha tényleg szükséges.
- **`.cursorignore`:** a repo gyökerében szűrjük a build outputot, `node_modules`-t, `*.db`-t, stb. — kevesebb zaj az indexben és a véletlen kontextusban (nem helyettesíti a `.gitignore`-t).
- **Handoff + live docs először:** új chatnél másold be a **§7 Handoff** blokkot + mutasd meg az **ACTIVE** sort a `docs/live/03_kovetkezo_lepesek.md`-ből — így nem kell újra felépíteni a történetet. Rövid összefoglaló: `docs/live/ai-cursor-token-efficiency.md`.
- **Nagy log helyett:** ne illessz be tízezer sort; tedd **fájlba** a repóban (vagy csatolj **egy** fájlt) + írd meg, melyik sorra vagy kíváncsi.
- **Iteráció végén:** `docs/live/02` + `03` frissítése + **`pr-body.md`** + **`iter-end`** / `gh pr create` — kevesebb „mi volt a DoD?” vissza-vissza a chatben.
- **Composer:** több fájl / nagy diff esetén érdemes; egyszerű egyfájlos edithez elég a **Chat** — így is lehet spórolni.

### 10c) Mit jelent ez a repó konkrét folyamatára?

| Cél | Hogyan segít |
|-----|----------------|
| Kevesebb „ismételd el a kontextust” | §7 Handoff, `03` ACTIVE, branch név, `ai-cursor-token-efficiency.md` |
| Kevesebb véletlen nagy diff | WIP=1, 1 PR ≈ 1 milestone (§2, §5, **§5a**); szétvágás ha túl nagy / kevert téma |
| Kevesebb manuális PR-csevegés | §6b `iter-end`, `pr-body.md`, `gh` |
| Kevesebb indexelt / véletlen kontextus | `.cursorignore` (build, `node_modules`, stb.) |

---

## 11) Karbantartási sweep (opcionális)

Nagyobb backend+frontend milestone előtt / után: `dotnet build` + `npm run build`; ismétlődő minták (pl. error mapping duplikáció) — ha **3+** előfordulás, javasolj kis helper refaktort.

---

## 12) `.windsurf/` könyvtár

**Nem authoritative.** A folyamat és a szabályok innen: **`docs/DEVELOPMENT_WORKFLOW.md`** + **`docs/live/*`**.  
A `.windsurf/` megtartható történeti okból; új szabályt **ne** oda írj.
