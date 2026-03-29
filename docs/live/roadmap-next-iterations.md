# Következő iterációk — javasolt megvalósítási sorrend (63+)

> **Cél:** egy **világos, sorba tett** ütemterv a jelenlegi repo állapotához (62× lezárva, BFF + builder 58c+ kész, CDK defer, Playwright **nem** elsődleges).  
> **WIP=1:** egyszerre **csak egy** aktív iteráció a táblázatból (lásd még [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md)).

## Irányválasztás (röviden)

1. **Előbb** kanonikus **doksi / kontextus** (`PROJECT_CONTEXT`, README) — olcsó, nagy drift-megelőzés.  
2. **Aztán** **integrációs minőség** (kritikus API-utak lefedettsége a `Backend.Tests`-ben) — stabilabb, mint korai teljes böngészős e2e.  
3. **Utána** **dev / üzemeltetési UX** (egy oldalas local quickstart, opcionális script).  
4. **Böngészős e2e (Playwright)** csak **külön döntéssel** (CI költség, flakiness, karbantartás) — lásd iter **63d** opcionális.

## Iterációk

| Iter | Név | Mit ad | DoD (minimum) |
|------|-----|--------|----------------|
| **63a** | **Kanonikus kontextus** | `docs/PROJECT_CONTEXT.md` auth/BFF szekció frissítése (62c kész); `README` mutat erre + erre a roadmapre; `03` ACTIVE = 63a → 63b | PR `main`-re; nincs ellentmondás `02`/`03`-mal |
| **63b** | **Integrációs keményítés** | 1 konkrét hiány vagy gyenge pont lezárása **backend integrációs tesztekben** (pl. BFF meta + egy védett API út, vagy workflow smoke), *vagy* rövid **gap lista** doc + 1 új teszt | `dotnet test …Backend.Tests` zöld; releváns fájlok listája a PR-ban |
| **63c** | **Local dev / onboarding** | Egy **A4-es** „Local dev” blokk: backend port, `ng serve` + proxy, opcionális BFF; hivatkozás: [`auth-bff-httponly.md`](auth-bff-httponly.md), [`CONTRIBUTING.md`](../../CONTRIBUTING.md) | `npm run build` + szöveges review; README / CONTRIBUTING koherencia |
| **63d** *(opcionális)* | **E2e előkészület** | Playwright / más e2e: **csak** ha termék/CI igény van; külön PR, függőség-jóváhagyás | CI job + 1 smoke spec vagy dokumentált „nem most” döntés |

## ACTIVE (aktuális fókusz)

- **63a** — amíg nincs mergeelve; utána **63b**, stb.  
- Rövid állapot: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).

## Megjegyzés

- A **58c+ @angular/cdk** továbbra is **elhalasztva** — [`workflow-visual-builder.md`](workflow-visual-builder.md) § *Döntés*.  
- **62c+ e2e** backlog: iter **63d** vagy külön milestone; nem blokkolja a **63a–c**-t.
