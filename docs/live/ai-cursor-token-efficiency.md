# AI / Cursor — takarékos fejlesztés (token + kontextus)

## Miért van ez a fájl?

A Cursor / „Auto” használat **token-alapon** számolódik. A repó minősége nem attól függ, hogy **minden** fájl bekerül-e a chatbe, hanem attól, hogy a feladat **szűk**, a handoff **rövid**, és az indexelt zaj **minimális**.

**Normatív részletek:** [`docs/DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md) **§10** + **§7 Handoff** (szöveges + opcionális YAML-blokk). **Dokumentáció index:** [`docs/README.md`](../README.md).

## Projekt-szintű beállítások (repo)

- **`.cursorignore`** — build output, `node_modules`, nagy artefaktok: ne kerüljenek feleslegesen az indexbe / keresésbe (lásd repo gyökér).
- **Live docok** — kontextus reset után: **[`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md)** (karcsú) + [`02_allapot.md`](02_allapot.md); a régi, hosszú napló: [`03_ARCHIVE.md`](03_ARCHIVE.md) — **ne** húzd be alapból a chatbe.

## Checklist (fejlesztő + asszisztens)

1. **Egy szál = egy scope** (egy iteráció / egy PR / egy bug). Új téma → **új chat** + §7 Handoff blokk.
2. **`@` fájlok / mappák minimálisan** — csak ami közvetlenül kell; kerüld a teljes repo behúzást.
3. **Nem faliszöveg log** — fájl a repóban vagy egy rövid idézet + sorhivatkozás.
4. **Composer** több fájlhoz; **Chat** egy szűk edithez (lásd §10).
5. **Iteráció végén** `02` + `03` + `pr-body.md` — kevesebb „mi volt a DoD?” kör a chatben.
6. **Modell** (UI): gyors/Auto a napi kis munkára; erősebb modell csak architektúra / kockázatos domain logikához (részletek §10a).

## Mit nem jelent?

- Nem **spórolunk** a **gate-eken** (`dotnet test`, `npm run build`) vagy a **DoD**-on.
- Nem **törünk** API-szerződést vagy biztonsági alapokat azért, hogy kevesebb token legyen.
