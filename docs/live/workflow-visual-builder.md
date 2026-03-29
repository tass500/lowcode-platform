# Vizuális workflow builder (Iter 58 — első szelet)

## Cél

A JSON definíció marad az igazság forrása; a **Builder** nézet a workflow **részletek** oldalon (`/lowcode/workflows/:id`) kiegészíti a meglévő **Viewer** és **JSON** nézetet; az **új workflow** oldalon (`/lowcode/workflows/new`) a **Builder | JSON** váltó ugyanazt a palette / sorrend / törlés / JSON → viselkedést adja (58b).

## MVP viselkedés (jelenleg)

- **+ típus** gombok: minimal lépés objektum hozáfűzése a `steps` tömbhöz (`lowcode-workflow-builder-utils.ts`).
- Lépéslista: futásidejű kulcs **`000`**, **`001`**, … (tömb sorrend) — megjelenítve.
- **↑ / ↓** átrendezés, **Remove** törlés, **JSON →** ugrás a JSON szerkesztőhöz (meglévő `findCaretIndexForWorkflowStep`).

## Döntés: `@angular/cdk` — **most nem** vezetjük be

**Aktuális állás:** a **`@angular/cdk`** (pl. `DragDropModule`) **nem** kerül a projektbe ebben a körben.

**Indoklás (röviden):**

- A builder elsődleges használata **asztali / dev** jellegű; **mobil-first** szerkesztés nem az aktuális fókusz.
- A **natív HTML5 DnD + nagyobb érintési cél + ↑↓** elég a jelen kockázat–haszon arányhoz; a CDK **új függőség**, **refaktor** és **regressziós felület** lenne.
- A CDK fő előnye itt a **finomabb touch / lista-DnD UX** — **újraértékelendő**, ha mobil/tablet builder **termék** prioritás lesz, vagy **ismétlődő** natív DnD bug jegyek jönnek célzott böngészőkön.

**Újraértékelés:** roadmap / issue alapján; külön PR: `npm i @angular/cdk`, modul import, builder lista átírása, `npm run build` + smoke.

## Korlátok / következő lépések

- **58c (MVP):** sorrend **natív HTML5** drag&drop a builderben (`⋮⋮` fogó; sor fele felett/alatt ejtés = elé / mögé); **új dependency nélkül**. ~~Opcionális később: `@angular/cdk`~~ — lásd fenti **Döntés** blokk.
- Átrendezés után a **`${000.*}`** hivatkozások elcsúszhatnak — a szöveg ezt jelzi; finomhangolás továbbra is **JSON** nézetben.
- **New workflow** (58b): Builder + JSON váltó, template gombok továbbra is a `definitionJson`-t írják; további szelet: jobb mobil UX (CDK nélkül vagy később, lásd **Döntés**).

## Fájlok

- `frontend/src/app/pages/lowcode-workflow-builder-utils.ts` (+ `.spec.ts`)
- `frontend/src/app/pages/lowcode-workflow-details-page.component.ts` — `viewMode === 'builder'`
- `frontend/src/app/pages/lowcode-workflow-new-page.component.ts` — `viewMode === 'builder' | 'json'`
