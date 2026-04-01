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

## Iter 68c — `GET /api/entities` lista (kész)

**Backend:** `EntityDefinitionEndpointsTests` — új esetek:

- Lista **név szerint rendezve** (két POST: Zebra, majd Apple → GET → **Apple**, **Zebra**).
- **Üres tenant** (nincs entitás): **200**, `items` üres tömb, **`serverTimeUtc`** string a JSON gyökérben.

Kapcsolódó UI: termék **67c** (Entities lista) — [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md).

## Iter 68d — `GET /api/entities/{entityId}/records` lista (kész)

**Backend:** `EntityRecordEndpointsTests` — új esetek:

- **Ismeretlen** `entityId` → **404**.
- Létező entitás, **nincs rekord**: **200**, `items` üres tömb, **`serverTimeUtc`**.
- **Sorrend:** `UpdatedAtUtc` csökkenő, aztán `EntityRecordId` csökkenő — két POST után a **régebbi** rekord **PUT**-tal frissítve → első helyen a listában.

Kapcsolódó UI: termék **67c** (Entity records) — [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md).

## Iter 68e — Entity rekord POST + GET részletek (kész)

**Backend:** `EntityRecordEndpointsTests` — további esetek:

- **POST** üres / whitespace **`dataJson`** → **400**, `errorCode` **`data_missing`**.
- **POST** nem objektum JSON (pl. tömb) → **400**, **`data_invalid`**.
- **GET** `/api/entities/{entityId}/records/{recordId}`: létrehozás után **200**, `entityRecordId` / `entityDefinitionId` / `dataJson` egyezik.
- **GET** ismeretlen rekord (létező entitásnál) → **404**, **`record_not_found`**.
- **GET** ismeretlen entitás → **404**, **`entity_not_found`**.

## Iter 68f — Entity definíció név + ütközés (kész)

**Backend:** `EntityDefinitionEndpointsTests` — további esetek:

- **POST** második entitás **ugyanazzal a névvel** → **409**, **`entity_already_exists`**.
- **POST** üres / whitespace **`name`** → **400**, **`name_missing`**.
- **POST** név **> 100** karakter → **400**, **`name_too_long`**.
- **PUT** másik entitás nevére váltás (már foglalt) → **409**, **`entity_already_exists`**.
- **PUT** **azonos** név marad (önmagára) → **200**.
- **GET** ismeretlen `id` → **404**, **`entity_not_found`**.

## Iter 68g — Entity mezők (`/api/entities/{entityId}/fields`) (kész)

**Backend:** `EntityFieldEndpointsTests` — új fájl / esetek:

- **POST** ismeretlen entitásnál → **404**, **`entity_not_found`**.
- **POST** második mező **ugyanazzal a névvel** → **409**, **`field_already_exists`**.
- **POST** hiányzó **`fieldType`** (whitespace) → **400**, **`field_type_missing`**.
- **POST** **`maxLength`: 0** → **400**, **`max_length_invalid`**.
- **PUT** másik mező nevére váltás → **409**, **`field_already_exists`**.
- **PUT** változatlan név + típus → **200**.
- **DELETE** ismeretlen mező (létező entitásnál) → **404**, **`field_not_found`**.

## Iter 68h — Workflow `domainCommand` futás szintű hibakódok (kész)

**Backend:** `WorkflowRunEndpointsTests` — további esetek:

- **`entityRecord.updateById`** nem létező **`recordId`**-ra → futás **`failed`**, run **`errorCode`**: **`entity_record_not_found`** (a lépés **`lastErrorCode`** megegyezik; a runner nem írja felül, ha már be van állítva).
- Ismeretlen **`command`** sztring → **`failed`**, **`domain_command_unknown`**.

Kapcsolódó motor: **`WorkflowRunnerService`** (`ExecuteDomainCommandAsync` + rekord frissítés).

## Iter 68i — `GET /api/workflows` lista (kész)

**Backend:** `WorkflowEndpointsTests` — új esetek:

- Lista **név szerint rendezve** (két POST: Zebra, majd Apple → GET → **Apple**, **Zebra**) — egyezik a `WorkflowsController.List` **`OrderBy(Name)`** viselkedésével.
- **Üres tenant** (nincs workflow): **200**, `items` üres tömb, **`serverTimeUtc`** a JSON gyökérben.

Kapcsolódó UI: termék **67b** (Workflow lista) — [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md).

## Kapcsolódó

- Termék 67: [`roadmap-iter-67-product.md`](roadmap-iter-67-product.md) · Frontend run lista: `frontend/.../lowcode-workflow-runs-page.component.ts`.
