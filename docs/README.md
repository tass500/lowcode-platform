# Dokumentáció — központi index

Ez a könyvtár a **lowcode-platform** írásos igazságának és folyamatának központja. Cél: **iparági best practice**-hez igazodó, **kereshető**, **verziózott** (Git) dokumentáció, amely csökkenti a kontextus-driftet és gyorsítja az onboardingot.

## Olvasási sorrend (ajánlott)

| Közönség | Első lépés | Aztán |
|----------|------------|--------|
| **Új fejlesztő / PM** | [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) | [`DEVELOPMENT_WORKFLOW.md`](DEVELOPMENT_WORKFLOW.md), [`live/03_kovetkezo_lepesek.md`](live/03_kovetkezo_lepesek.md) |
| **Reviewer / merge** | [`DOCUMENTATION_EXCELLENCE.md`](DOCUMENTATION_EXCELLENCE.md) | [`GOVERNANCE.md`](GOVERNANCE.md) §8–9, [`01_quality_gates.md`](01_quality_gates.md) |
| **Napi munka** | [`live/03_kovetkezo_lepesek.md`](live/03_kovetkezo_lepesek.md) + [`live/02_allapot.md`](live/02_allapot.md) | Iteráció-specifikus `live/roadmap-*.md` |
| **AI / Cursor reset** | [`live/03_kovetkezo_lepesek.md`](live/03_kovetkezo_lepesek.md) | [`live/ai-cursor-token-efficiency.md`](live/ai-cursor-token-efficiency.md) |
| **Üzemeltetés / incidens** | [`runbooks/upgrade_v0.md`](runbooks/upgrade_v0.md) | [`live/ops/upgrade.md`](live/ops/upgrade.md) |

## Dokumentum-osztályok (összefoglaló)

Részletes szabályok: **[`GOVERNANCE.md`](GOVERNANCE.md)**.

| Osztály | Hely | Szerep |
|--------|------|--------|
| **Folyamat (authoritative)** | `DEVELOPMENT_WORKFLOW.md` | PR, branch, DoR/DoD, handoff, AI §10 |
| **Review + doc-kiválóság** | `DOCUMENTATION_EXCELLENCE.md` | Emberi review-minimum, anti-drift, eszkaláció; kiegészíti a `GOVERNANCE.md`-et |
| **Kanonikus termék / platform** | `PROJECT_CONTEXT.md` | Invariánsok, portok, auth-összefoglaló, API-konvenciók |
| **Futó állapot (WIP)** | `live/02_*.md`, `live/03_*.md` | Rövid, gyakran frissül; egy ACTIVE fókusz |
| **Mély / feature** | `live/*.md` (topic) | Pl. OIDC, BFF, CI, workflow — változáskor frissül |
| **Sablon (truth template)** | `00_truth_files_template/*` | Hosszú távú vízió / ADR minták; nem helyettesítik a `live` napi igazságát |
| **ADR (döntésnapló)** | `adr/` | Opcionális strukturált architektúra-döntések |

## Gyors linkek

- **Dokumentációs kiválóság (review-minimum, anti-drift):** [`DOCUMENTATION_EXCELLENCE.md`](DOCUMENTATION_EXCELLENCE.md)
- **Minőségkapuk:** [`01_quality_gates.md`](01_quality_gates.md)
- **Munkamód (batch / credit):** [`00_workmode.md`](00_workmode.md)
- **Kódbeli tájékozódás:** [`CODEMAP.md`](CODEMAP.md)
- **PR szöveg sablon:** [`templates/pr-body.example.md`](templates/pr-body.example.md)
- **Governance & változás-menedzsment:** [`GOVERNANCE.md`](GOVERNANCE.md)

## Live dokumentumok (`docs/live/`)

A `live/` mappa a **napi operatív** dokumentáció helye (roadmapok, CI, security, auth, workflow). Új fájlok ide kerüljenek, ha egy **konkrét feature/hullám** tartós leírást igényel.

## Kapcsolódó repo-elemek

- **Cursor szabályok:** `.cursor/rules/*.mdc` — rövid emlékeztetők; a hosszú folyamat a `DEVELOPMENT_WORKFLOW.md`-ben van.
- **CI:** `.github/workflows/` — a [`live/ci-*.md`](live/) fájlok írják le a szándékot és a parancsokat.
