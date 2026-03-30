# Termék — iter 67+ (low-code platform demo)

> **Cél:** a repo fókuszát a **platform / low-code workflow**, **tenant + auth**, **entitások** és **futások** körüli demó-élményre helyezni; az **Upgrade** UI továbbra is elérhető, de nem elsődleges belépési pont.  
> **WIP=1:** [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).

## Iterációk (prioritás szerint)

| Iter | Név | Mit ad | DoD (minimum) |
|------|-----|--------|----------------|
| **67a** | **Shell + belépés** | Globális fejléc: aktuális **tenant** (élő frissítés session mentéskor); nav: Workflows / Runs / Entities előrébb; **`/` → `/lowcode/workflows`** | `npm run build`; manuális: tenant változik auth után a fejlécben |
| **67b** | **Workflow lista UX** | Szűrés / keresés **név** szerint (kliens oldali MVP) + üres állapot szöveg | build; gyors smoke |
| **67c** | **Entity lista / rekordok** | Lista üres állapot, konzisztens nav; opcionális: egyszerű szűrő | build |
| **67d** | **Run lista** | Állapot szerinti szűrő (ha API már ad / könnyen bővíthető); különben csak UX copy | build + integráció, ha API változik |

## Hullám státusz

- **67a** — ✅ shell + default route (fejléc tenant + `/` → workflows).
- **67b** — ✅ workflow lista: **név** szerinti kliens oldali szűrő + üres állapot szövegek.
- **67c** — ✅ **Entities** lista: név szerinti szűrő + nav (Workflows / All runs) + üres állapotok; **Entity records**: szűrő vs. üres lista szétválasztva, „Showing X of Y”, nav; következő: **67d**.

## Kapcsolódó

- Nagy döntések (frontend irány): [`02_allapot.md`](02_allapot.md) · Upgrade oldal parkolva: ugyanott.  
- Enterprise / CI (66+): [`ci-supply-chain.md`](ci-supply-chain.md), [`ci-sbom.md`](ci-sbom.md).  
- Angular upgrade + `npm audit` kapu: külön hullám — [`ci-supply-chain.md`](ci-supply-chain.md).
