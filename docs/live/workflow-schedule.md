# Workflow ütemezés — MVP (Iter 54)

## Összefoglaló

A backend **hosted service** (`WorkflowScheduleHostedService`, ~15 s tick) **tenantenként** végignézi a `workflow_definition` sorokat, ahol `schedule_enabled=1` és `schedule_next_due_utc <= UTC now`. Ilyenkor előre lépteti a következő esedékességet, majd **aszinkron** elindít egy futást (`WorkflowRunnerService.StartAsync`), ugyanazzal a motorral, mint a manuális **Start run** vagy az **inbound** trigger.

- **Kikapcsolás:** `LCP_WORKFLOW_SCHEDULE_DISABLED=1` env.
- **Teszt környezet:** a hosted service **nem** regisztrálódik (`Testing`), így az integrációs tesztek determinisztikusak maradnak.

## API

- `PUT /api/workflows/{id}/schedule` — body: `{ "enabled": true|false, "cron": "..." }` (JWT, `tenant_user`). Ha `enabled=true`, a `cron` kötelező.
- `GET /api/workflows/{id}` — válaszban: `scheduleEnabled`, `scheduleCron`, `scheduleNextDueUtc`.

## Korlátozott cron (UTC)

Öt mező: `perc óra nap hónap hétnap`. A **nap, hónap, hétnap csak `*`** lehet (MVP).

Támogatott minták:

| Minta | Jelentés |
|--------|----------|
| `* * * * *` | minden percben (a következő teljes perc) |
| `*/N * * * *` | N percenként, 1 ≤ N ≤ 59 |
| `M * * * *` | óránként az M. percben (0–59) |
| `M H * * *` | naponta H:M UTC (0–23, 0–59) |

## Megjegyzések

- **Több példány** (horizontális skálázás): ugyanaz a definíció kétszer is elindulhat; élesben érdemes egyetlen scheduler példány vagy DB-s zárolás (későbbi iteráció).
- **Hosszú futások:** a tick nem várja be a futás végét; a következő esedékesség már a start előtt beíródik.

## Kapcsolódó

- Inbound webhook: [`workflow-inbound-trigger.md`](workflow-inbound-trigger.md)
