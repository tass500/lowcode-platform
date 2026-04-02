# Dokumentáció és változás — governance (enterprise irányelvek)

Ez a fájl kiegészíti a [`DEVELOPMENT_WORKFLOW.md`](DEVELOPMENT_WORKFLOW.md)-t: **ki mit tart karban**, **milyen dokumentum-osztályok** vannak, és **mikor** kell kiterjesztett figyelem (biztonság, szerződés).

## 1) Célok

- **Egy igazság forrása** (single source of truth) per kontextus — elkerülni az ellentmondó Slack/chat/README szövegeket.
- **Verziózott történet**: stratégiai döntések Gitben maradnak; a régi állapot nem „vész el”.
- **Auditálhatóság**: fontos architektúra- és biztonsági döntések visszakövethetők (ADR / döntésnapló).
- **Skálázható review**: a változás kategóriája alapján elvárható ellenőrzés (önellenőrzés vs. peer review).

## 2) Dokumentum-osztályok és hierarchia

| Prioritás | Tartalom | Fő fájl / hely |
|-----------|----------|----------------|
| **P0 — Folyamat** | Branch, PR, DoD, gate-ek, handoff | `DEVELOPMENT_WORKFLOW.md` |
| **P0 — Review / doc-sáv** | Emberi minimum merge előtt, anti-drift, eszkaláció | `DOCUMENTATION_EXCELLENCE.md` (kiegészíti ezt a fájlt) |
| **P0 — Termék/platform invariáns** | Portok, auth összkép, error contract, admin konvenciók | `PROJECT_CONTEXT.md` |
| **P1 — Operatív állapot** | WIP, következő lépés, kész tények | `live/02_allapot.md`, `live/03_kovetkezo_lepesek.md` |
| **P1 — Téma-specifikus** | OIDC, BFF, CI, rate limit, stb. | `live/<topic>.md` |
| **P2 — Sablon / blueprint** | Hosszú távú vízió, ADR táblázat, iteráció-becslés | `00_truth_files_template/*` |
| **P2 — ADR** | Egy-egy architektúra-döntés rögzítése | `adr/000-title.md` (lásd [`adr/README.md`](adr/README.md)) |

**Konfliktusfeloldás:** Ha két dokumentum ellentmond, a sorrend: **`DEVELOPMENT_WORKFLOW.md`** (folyamat) → **`DOCUMENTATION_EXCELLENCE.md`** (review-minimum, nem írja felül a folyamatot, hanem részletezi) → **`PROJECT_CONTEXT.md`** (platform) → **`live/02` + `live/03`** (aktuális szállítás) → téma-doc `live/*` → sablonok.

## 3) Szerepkörök (RACI — egyszerűsített)

Enterprise környezetben a pontos szereposztás a szervezettől függ. A repó **minimum elvárása**:

| Tevékenység | **R**esponsible (csinálja) | **A**ccountable (jóváhagyja / merge) |
|-------------|---------------------------|-------------------------------------|
| Kód + teszt + gate | Fejlesztő / AI-asszisztencia (felügyelet mellett) | PR reviewer (csapatpolitika szerint) |
| `live/02`, `live/03` frissítés | Ugyanaz a változtató | Ugyanaz a PR review (DoD része) |
| Architektúra-döntés (új integráció, auth modell) | Tech lead / tulajdonos | Explicit jóváhagyás + ADR ajánlott |
| Titkok / credential | Soha commit | Secret store / CI változók |

## 4) Változás-kategóriák és review-trigger

| Kategória | Példa | Minimum ellenőrzés |
|-----------|--------|---------------------|
| **Alacsony kockázat** | Szöveg, typo, komment | Build + teszt a [`01_quality_gates.md`](01_quality_gates.md) szerint |
| **Közepes** | Új integrációs teszt, refaktor | `dotnet test` + releváns gate |
| **Magas — szerződés** | API válasz alak, `errorCode`, header | Backend + frontend build; API megjegyzés a PR-ban |
| **Magas — biztonság** | Auth, rate limit, audit log, dependency | CI security jobok figyelése; [`live/security-*.md`](live/) érintett doc frissítése |
| **Kritikus** | Új külső függőség, adatbázis séma | Explicit jóváhagyás a csapatpolitika szerint; dependency review |

## 5) Architecture Decision Records (ADR)

Strukturált, **egy döntés = egy fájl** megközelítés: [`adr/README.md`](adr/README.md).  
A `00_truth_files_template/01_dontesek.md` **policy index** és **ADR** hosszú formátuma továbbra is érvényes referencia; új döntéseknél érdemes a `adr/` formátumot használni, és a policy táblázatba hivatkozni.

## 6) Biztonsági és megfelelőségi minimum (dokumentációs oldalról)

- **Ne commitolj** titkot, tokent, jelszót; lásd `DEVELOPMENT_WORKFLOW.md` §8.
- **Security audit / logging** viselkedés: [`live/security-audit-logging.md`](live/security-audit-logging.md).
- **Supply chain / SBOM / titok-szűrés**: [`live/ci-supply-chain.md`](live/ci-supply-chain.md), [`live/ci-secret-scanning.md`](live/ci-secret-scanning.md), [`live/ci-sbom.md`](live/ci-sbom.md).

## 7) Karbantartási ciklus (ajánlott)

- **Minden merge-elhető PR után:** `live/02` + `live/03` ellenőrzése (DoD).
- **Negyedévente / nagy release előtt:** `PROJECT_CONTEXT.md` és a legfontosabb `live/*` összhangja a kóddal; elavult állítások törlése vagy `03_ARCHIVE`-ba helyezése.
- **Archívum:** hosszú történet [`live/03_ARCHIVE.md`](live/03_ARCHIVE.md) — ne növeljük feleslegesen a `03` aktív fájlt.

## 8) PR review — emberi minimum (a CI nem elég)

A zöld build/test **szükséges**, de **nem elégséges** a [`DEVELOPMENT_WORKFLOW.md`](DEVELOPMENT_WORKFLOW.md) DoD-jéhez. A merge előtt legalább ezeket erősítse meg a **reviewer** (vagy önellenőrzés egy tulajdonosnál):

1. **Scope:** egy koherens egység; kevert téma / túl nagy diff esetén szétvágás vagy explicit indoklás.
2. **Kockázat-sáv:** ez a §4 táblázat szerinti minimum ellenőrzés megvan (beleértve a PR szövegét, ha szerződés / API változott).
3. **Dokumentáció:** `live/02` + `live/03` és szükség szerint téma-doc / `PROJECT_CONTEXT` összhangban van a változással.
4. **AI-generált kód:** auth, szerződés, biztonság — emberi értelmezés történt-e.

Részletes check-lista és anti-drift: [`DOCUMENTATION_EXCELLENCE.md`](DOCUMENTATION_EXCELLENCE.md).

## 9) Megfelelőség, adatvédelem, jogi eszkaláció

A repó dokumentációja **technikai** minimumot és folyamatot rögzít. **Jogász / DPO / szerződéses** vélemény** nem** pótolható Markdownnal. Ha a változás érinti: új adatkezelési cél, érintetti jogok termékfunkcióban, harmadik ország, DPIA, SLA/audit jog — a szervezet szabályai szerint **kötelező eszkaláció**; a PR-ban rövid **compliance note** ajánlott (mi változott, milyen szakmai jóváhagyás szükséges / megvan).

Részletek: [`DOCUMENTATION_EXCELLENCE.md`](DOCUMENTATION_EXCELLENCE.md) §5.

## 10) Kapcsolódó

- Központi index: [`README.md`](README.md)
- Minőségkapuk: [`01_quality_gates.md`](01_quality_gates.md)
- Dokumentációs kiválóság és 9–10-es sáv: [`DOCUMENTATION_EXCELLENCE.md`](DOCUMENTATION_EXCELLENCE.md)
