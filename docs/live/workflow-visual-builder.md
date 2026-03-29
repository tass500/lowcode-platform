# Vizuális workflow builder (Iter 58 — első szelet)

## Cél

A JSON definíció marad az igazság forrása; a **Builder** nézet a workflow **részletek** oldalon (`/lowcode/workflows/:id`) kiegészíti a meglévő **Viewer** és **JSON** nézetet; az **új workflow** oldalon (`/lowcode/workflows/new`) a **Builder | JSON** váltó ugyanazt a palette / sorrend / törlés / JSON → viselkedést adja (58b).

## MVP viselkedés (jelenleg)

- **+ típus** gombok: minimal lépés objektum hozáfűzése a `steps` tömbhöz (`lowcode-workflow-builder-utils.ts`).
- Lépéslista: futásidejű kulcs **`000`**, **`001`**, … (tömb sorrend) — megjelenítve.
- **↑ / ↓** átrendezés, **Remove** törlés, **JSON →** ugrás a JSON szerkesztőhöz (meglévő `findCaretIndexForWorkflowStep`).

## Korlátok / következő lépések

- **58c (MVP):** sorrend **natív HTML5** drag&drop a builderben (`⋮⋮` fogó; sor fele felett/alatt ejtés = elé / mögé); **új dependency nélkül**. Opcionális később: `@angular/cdk` (touch / animáció / lista UX).
- Átrendezés után a **`${000.*}`** hivatkozások elcsúszhatnak — a szöveg ezt jelzi; finomhangolás továbbra is **JSON** nézetben.
- **New workflow** (58b): Builder + JSON váltó, template gombok továbbra is a `definitionJson`-t írják; további opcionális szelet: CDK finomítás vagy jobb mobil UX.

## Fájlok

- `frontend/src/app/pages/lowcode-workflow-builder-utils.ts` (+ `.spec.ts`)
- `frontend/src/app/pages/lowcode-workflow-details-page.component.ts` — `viewMode === 'builder'`
- `frontend/src/app/pages/lowcode-workflow-new-page.component.ts` — `viewMode === 'builder' | 'json'`
