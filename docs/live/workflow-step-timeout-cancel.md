# Workflow step — timeout, megszakítás, beágyazott lépések (Iter 55)

## Összefoglaló

- **`timeoutMs`** (lásd még [`workflow-step-retry.md`](workflow-step-retry.md)): próbán belüli időkorlát, **linked** `CancellationToken`-nel. Időtúllépéskor a lépés **`workflow_step_timed_out`**, és a run details-ben a step **`lastErrorConfigPath`** tipikusan **`$.timeoutMs`** (ha máshol nem volt specifikusabb path).
- **Kooperatív megszakítás**: `POST /api/workflows/{id}/runs` a `CancellationToken`-t továbbadja a runnernek; kliens oldali lemondás (pl. `HttpClient` + `CancellationTokenSource`) esetén a futás **`canceled`** állapotba kerülhet, a lépés **`canceled`** kóddal (lásd integrációs teszt a teszt projektben).
- **Beágyazott lépések** (`foreach` → `do`, `switch` → `do` / `default`): ugyanaz a **`retry` / `timeoutMs` / backoff** logika érvényes, mint a legfelső szintű lépéseknél (`TryExecuteStepWithRetryAndTimeoutAsync`). Korábban a belső lépés csak a „nyers” `ExecuteStepBodyAsync`-ot kapta, ezért a **`timeoutMs` nem védett** a belső `delay` ellen.

## Megjegyzések

- **Több példány / háttér indítás** (ütemezés, inbound ugyanabban a folyamatban): ha nincs `CancellationToken`, a megszakítás csak a step timeouttal érhető el.
- **Admin „cancel run”** API jelenleg nincs (ellentétben az upgrade futásokkal); iterációs bővítés lehet külön milestone.

## Kapcsolódó

- Retry / backoff + `timeoutMs` JSON: [`workflow-step-retry.md`](workflow-step-retry.md)
- Ütemezés: [`workflow-schedule.md`](workflow-schedule.md)
