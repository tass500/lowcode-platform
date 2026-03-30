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

## Kapcsolódó

- Termék 67: [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md) · Frontend run lista: `frontend/.../lowcode-workflow-runs-page.component.ts`.
