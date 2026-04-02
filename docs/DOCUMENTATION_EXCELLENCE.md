# Dokumentációs kiválóság — cél, review-sáv, anti-drift

**Cél:** a repó írásos anyaga ne csak *legyen*, hanem **megbízható**, **ellenőrzött** és **AI-asszisztált fejlesztés mellett is** visszakövethető legyen — összhangban a [`GOVERNANCE.md`](GOVERNANCE.md) és a [`DEVELOPMENT_WORKFLOW.md`](DEVELOPMENT_WORKFLOW.md) szerinti osztályokkal.

**Célszint (irodalmi skála):** a folyamat + live doc + PR-disciplína együttes betartásával a **9–10-es** sáv azt jelenti: **zöld gate + kötelező emberi review-minimum + driftmentes operatív igazság** (`02`/`03`/`PROJECT_CONTEXT` / téma-doc), nem csak „a CI lefutott”.

---

## 1) Mi számít „kiváló” dokumentációnak ebben a repóban?

| Követelmény | Jele |
|-------------|------|
| **Hierarchia tisztelete** | Ellentmondás esetén a [`GOVERNANCE.md`](GOVERNANCE.md) §2 sorrendje érvényesül. |
| **Operatív igazság** | A [`docs/live/02_allapot.md`](live/02_allapot.md) és [`docs/live/03_kovetkezo_lepesek.md`](live/03_kovetkezo_lepesek.md) a mergehez közeli állapotot tükröznek; az ACTIVE fókusz egyértelmű (WIP=1). |
| **Téma-docok** | Ha a kód viselkedése változik (auth, CI, BFF, stb.), az érintett `docs/live/<téma>.md` frissül vagy a PR indokolja, miért nem kell. |
| **Szerződés / platform** | API alak, `errorCode`, portok, auth-összkép: [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) és/vagy PR-összefoglaló összhangban. |
| **ADR** | Jelentős architektúra-döntés: [`adr/README.md`](adr/README.md) szerint rögzítés vagy PR-ban explicit „később ADR” indoklás (csak kivételesen). |

---

## 2) PR review — emberi minimum (nem helyettesíthető a CI-vel)

A merge előtt a **reviewer** (vagy önellenőrzés önálló tulajdonosnál) erősítse meg szövegesen vagy check-listként:

1. **Scope:** a PR egy koherens egység; nincs véletlenül becsúszott, független téma (ha igen → szétvágás javasolt; lásd `DEVELOPMENT_WORKFLOW.md` §5a).
2. **Kockázat:** a [`GOVERNANCE.md`](GOVERNANCE.md) §4 kategória szerint megvan a **minimum ellenőrzés** (build, integrációs teszt, szerződés-összefoglaló, security doc).
3. **Dokumentáció:** meaningful változásnál `02` + `03` frissült (DoD); téma-doc / `PROJECT_CONTEXT` érintett, ha a viselkedés változott.
4. **AI-generált diff:** a szerződés-, auth- és biztonságérzékeny sorok **emberi értelmezést** kaptak (nem csak „zöld a teszt”).
5. **Titkok:** nincs token, jelszó, PII commit; secret scan / supply-chain jobok figyelése a változás súlyához igazítva.

**Olcsó, de kötelező eszköz:** a [`templates/pr-body.example.md`](templates/pr-body.example.md) kitöltése — összefoglaló + **test plan pontos parancsokkal**.

---

## 3) AI-asszisztált fejlesztés — felelősség megosztása

- **Fejlesztő / prompt:** szűk scope, Handoff (`DEVELOPMENT_WORKFLOW.md` §7), gate-ek futtatása.
- **Reviewer:** a fenti §2 minimum; különösen **magas kockázat** (§4) és **új dependency** — explicit jóváhagyási sáv a [`GOVERNANCE.md`](GOVERNANCE.md) szerint.
- **Asszisztens:** nem helyettesíti a `live/02` + `03` frissítését „automatikusan”; a DoD része a **szándékos** doc-frissítés milestone után.

---

## 4) Anti-drift szabályok

| Forrás | Drift jele | Teendő |
|--------|------------|--------|
| `live/03` | ACTIVE nem egyezik a tényleges branch/munkával | Azonnal javítás vagy Handoff frissítés |
| `live/02` | „Kész” állítás ellentmond a kódnak | Javítás vagy archívumba helyezés (`03_ARCHIVE`) |
| `PROJECT_CONTEXT.md` | Port / auth / API leírás elavult | Frissítés vagy ADR + doc |
| Téma `live/*.md` | Kód mást csinál, mint a doc | Doc vagy kód — válassz egy igazságot, a másikat igazítsd |

**Negyedévente / nagy release előtt:** [`GOVERNANCE.md`](GOVERNANCE.md) §7 szerinti felülvizsgálat.

---

## 5) Megfelelőség, adatvédelem, jogi eszkaláció

A repó **technikai** minimumjait a [`GOVERNANCE.md`](GOVERNANCE.md) §6 és a security live docok írják le. **Szervezeti** (jogász / DPO / szerződéses) réteg nem helyettesíthető dokumentációval; a következő esetekben **kötelező** a szervezeten belüli szabály szerinti eszkaláció (a szerepkörök neve szervezetenként változik):

- Új **adatkezelési** cél, profiling, harmadik országba továbbítás, DPIA szükségessége.
- **Szerződéses** SLA / felelősség / audit jog változása a termék viselkedésében.
- **Érintetti jogok** (törlés, export) új automatizálása termékfunkcióban.

A PR-ban ilyenkor: **rövid „compliance note”** — mi változott, ki adott szakmai véleményt (ha már megvan).

---

## 6) Csak dokumentációt érintő PR

- Linkek ellenőrzése (relatív útvonalak).
- Konfliktus a hierarchiával? → [`GOVERNANCE.md`](GOVERNANCE.md) §2.
- Ha `02`/`03` változik: az ACTIVE és a kész tények továbbra is **egyértelműek**.

---

## 7) Kapcsolódó

- Folyamat: [`DEVELOPMENT_WORKFLOW.md`](DEVELOPMENT_WORKFLOW.md) (DoD: §4, AI: §10, Handoff: §7)
- Governance: [`GOVERNANCE.md`](GOVERNANCE.md)
- Minőségkapuk: [`01_quality_gates.md`](01_quality_gates.md)
- Index: [`README.md`](README.md)
- AI takarékosság: [`live/ai-cursor-token-efficiency.md`](live/ai-cursor-token-efficiency.md)
