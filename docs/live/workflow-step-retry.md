# Workflow step retry / backoff (Iter 50)

## JSON (step objektum)

Opcionális **`retry`** objektum a lépés gyökerén (testvére a `type`-nak), és opcionális **`timeoutMs`** (egész, ≥ 1) ugyanitt.

```json
{
  "type": "domainCommand",
  "command": "entityRecord.updateById",
  "recordId": "...",
  "retry": {
    "maxAttempts": 5,
    "delayMs": 200,
    "backoffFactor": 2,
    "maxDelayMs": 2000
  },
  "timeoutMs": 30000
}
```

| Mező | Kötelező | Jelentés |
|------|----------|----------|
| `retry.maxAttempts` | nem (default: 1) | Hányszor fusson a lépés (beleértve az első próbát). Minimum 1. |
| `retry.delayMs` | nem | Két próba közötti késleltetés alapértéke (ms), első futásnál nincs várakozás. |
| `retry.backoffFactor` | nem (default: 1) | Exponenciális szorzó a késleltetésre (≥ 1). |
| `retry.maxDelayMs` | nem | Felső korlát egy várakozásra (ms). |
| `timeoutMs` | nem | Lépés szintű időkorlát (ms) egy próbán belül. |

## Beágyazott lépések (`foreach` / `switch`)

A **`do`** (és `switch` `default`) belsejében lévő lépésre ugyanúgy vonatkozik a **`retry`** és **`timeoutMs`**, mint a fő `steps` tömb elemeire. Részletek: [`workflow-step-timeout-cancel.md`](workflow-step-timeout-cancel.md).

## Viselkedés

- Az első próbánál **nincs** `delayMs` várakozás.
- A `delayMs` a 2. próbát megelőző várakozásra vonatkozik; a következő várakozások:  
  `delayMs * backoffFactor^(attempt-2)`, `maxDelayMs` cappinggel.
- A runner az **`attempt`** mezőt növeli próbánként; siker után a lépés `succeeded`.

## Lint

A `WorkflowDefinitionLinter` **figyelmeztet** (nem blokkol), ha a `retry` / `timeoutMs` formátuma nem egyezik a fentiekkel (`workflow_retry_config_invalid`, `workflow_step_timeout_invalid`).

## UI

A workflow **Viewer v2** kártyákon a `retry` rövid összefoglalója megjelenik az alcímben.
