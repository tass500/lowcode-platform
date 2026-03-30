# Minőség — iter 68+ (API szerződés, regresszió)

> **Cél:** a low-code **tenant API** viselkedését integrációs tesztekkel rögzíteni, különösen ott, ahol a **frontend (PR #134)** közvetlenül támaszkodik a query paraméterekre.  
> **WIP=1:** [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).

## Iter 68a — `GET /api/workflows/runs` (kész)

**Backend:** `WorkflowRunEndpointsTests` — új esetek:

- Szűrés **`workflowDefinitionId`** szerint (`totalCount` + egy elem).
- **Lapozás** `take` / `skip` (`totalCount` konzisztens).
- **`startedAfterUtc`** jövőbeli időponttal → üres lista.
- **`state=succeeded`** → minden visszaadott sor állapota `succeeded` (noop futás után).

Meglévő: lista alap, érvénytelen `take`, érvénytelen `state` — változatlan.

## Iter 68b — UTC query validáció + `startedBeforeUtc` (kész)

**Backend:** `WorkflowRunEndpointsTests` — további esetek:

- **`startedAfterUtc`** időzóna nélküli query érték (pl. `2020-01-01T00:00:00`) → **400** (`Kind` nem UTC), ha a binder `Unspecified`/`Local`-t ad.
- Ugyanígy **`startedBeforeUtc`** időzóna nélkül → **400**.
- **`startedBeforeUtc`** érvényes UTC (`…Z` / `o`) → legalább egy noop futás szerepel a szűrt halmazban (inkluzív felső korlát a kontroller szerint).

## Kapcsolódó

- Termék 67: [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md) · Frontend run lista: `frontend/.../lowcode-workflow-runs-page.component.ts`.
