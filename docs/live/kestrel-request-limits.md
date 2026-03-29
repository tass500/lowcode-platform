# Kestrel kérés-méret limit (iter 64b)

## Cél

**OWASP / DoS** szemlélet: a bejövő HTTP törzs mérete legyen **felső korlátozott**, ne lehessen véletlenül vagy szándékosan **túl nagy JSON**-nal terhelni az API-t.

## Konfiguráció

| Kulcs | Alapértelmezés | Megjegyzés |
|-------|----------------|------------|
| `Kestrel:Limits:MaxRequestBodySize` | `10485760` (10 MiB) | `appsettings.json`; élesben env / Helm felülírhatja (pl. `Kestrel__Limits__MaxRequestBodySize`) |

A `Program.cs` csak akkor állítja be a Kestrel limitet, ha az érték **> 0**. Ha nincs megadva, a Kestrel **saját alapértelmezése** marad.

## Workflow import

A `POST /api/workflows/import` végponton **`[RequestSizeLimit(10 MiB)]`** is szerepel, hogy a kód szinten is látszódjon: nagy csomag importnál várható.

Ha a globális limitet **csökkented**, a `RequestSizeLimit` értékét is **igazítsd** (nem lehet nagyobb, mint a Kestrel felső korlátja).

## Kapcsolódó

- Ütemterv: [`roadmap-iter-64-plus.md`](roadmap-iter-64-plus.md)
