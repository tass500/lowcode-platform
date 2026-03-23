# Workmode — credit-hatékony batch fejlesztés (repo-szint)

Kapcsolódó (PR, merge, DoD): **[DEVELOPMENT_WORKFLOW.md](./DEVELOPMENT_WORKFLOW.md)**.

## Cél

- Minél kevesebb chat kör / credit felhasználás mellett minél nagyobb haladás.

## Alapelv

- Az asszisztens a feladatokat **max haszon / min kockázat** szerint priorizálja.
- Ha a feladatok **no behavior change** jellegűek és alacsony kockázatúak, akkor az asszisztens **automatikusan csomagol** több szeletet egy menetbe.
- Az asszisztens alapértelmezésben **autonóm módon folytatja**, és nem kérdez rá, hogy “mehetünk-e tovább?”.
- Csak akkor kérdez, ha tényleg blokkol (jóváhagyás, vagy nem eldönthető specifikáció).

## Fontos rendszer-korlát (credit / chat működés)

- Az asszisztens nem tud önállóan új üzenetet indítani a chatben.
- 1 credit = 1 válasz: amikor a válasz elküldésre kerül, a további lépésekhez kell egy új user üzenet.
- Következmény: a “folyamatos haladás” úgy érhető el, hogy az asszisztens 1 válaszon belül batch-eli a lehető legtöbb alacsony kockázatú feladatot (refaktor + docs + gate), és minimalizálja a megállásokat.

## Batch-refaktor szabályok (biztonságos credit-optimalizálás)

- Default: **3–8 alacsony kockázatú / no behavior change szelet** egy válaszban.
- Build gate: alapból **csak a batch végén** (pl. frontend: `npm run build`, backend: `dotnet build`).
  - Kivétel: ha közben TS/import/lint error jön elő, azt azonnal javítani kell ugyanabban a menetben.
- Live docs: alapból **csak a batch végén** frissül.
- Batch-et kisebbre kell venni érzékeny területeknél:
  - polling / state machine / load runner-ek
  - komplex async `try/catch/finally` + több UI flag / error/info üzenet
  - template változtatás

## Risk-tier (batch méret + ellenőrzés)

- Tier 0 — **pure / determinisztikus** (string/number utils, egyszerű getter-ek)
  - Batch: 6–12 szelet
  - Gate: a batch végén
- Tier 1 — **wiring / delegálás** (komponens metódus → helper hívás, state setter-ek)
  - Batch: 3–8 szelet
  - Gate: a batch végén
- Tier 2 — **async/state érzékeny** (polling, load runner, több flag + try/catch/finally)
  - Batch: 1–3 szelet
  - Gate: gyakrabban (akár szeletenként)
- Tier 3 — **viselkedés változtatás / prod bugfix**
  - Batch: 1 szelet
  - Gate: szeletenként + rövid manuális smoke, ha releváns

## Gate-mátrix (minimum ellenőrzés)

- Enterprise-minimum minőségkapuk (build/lint/test/smoke): `docs/01_quality_gates.md`.

- Frontend változás:
  - `npm run build`
- Backend változás:
  - `dotnet build`
- Full-stack / shared kontrakt (DTO, API, auth, headers) változás:
  - `npm run build`
  - `dotnet build`

## Definition of done (egy batch végén)

- Minden érintett TODO státusz frissítve.
- Live docs frissítve (ha van ilyen elvárás az adott workstreamen).
- A választott gate(ek) zöldek.
- Ha elérhető: lint/test gate-ek is zöldek (lásd `docs/01_quality_gates.md`).
- Nincs félbehagyott átnevezés / broken import / típushiba.

## Ha hiba van (gyors visszaállás szabály)

- Ha a batch közben build/típushiba jön elő, azonnal javítani kell ugyanabban a menetben.
- Ha a hiba oka nem egyértelmű 1–2 lépésből, a batch-et meg kell állítani és a következő menetben célzottan debuggolni.

## Kezdő prompt (általános)

```
Te Cascade vagy, senior pair programmer.

Projekt: /home/zoli/lowcode-platform.

Munkamód: a docs/00_workmode.md alapján dolgozz (credit-hatékony batch).

Kérlek:
- Prioritás: max haszon / min kockázat.
- No behavior change refaktorokat batch-elj 3–8 szeletben.
- Gate + live docs update csak a batch végén.
- Csak blokknál kérdezz.
```
