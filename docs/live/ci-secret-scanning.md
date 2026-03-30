# CI — titkok / credential szivárgás ellenőrzés (Gitleaks)

**Cél:** a repó történetében és a jelenlegi fájlokban **ne maradhasson** véletlenül becommitolt titok (API kulcs, JWT signing key, jelszó), az iparági **shift-left** és supply-chain gyakorlat szerint.

## GitHub Actions: `gitleaks.yml`

A workflow a **[Gitleaks](https://github.com/gitleaks/gitleaks)** `gitleaks/gitleaks-action` integrációját futtatja **push** és **pull_request** eseményre a `main` / `master` ágakon.

- A checkout **teljes history**-val történik (`fetch-depth: 0`), hogy a PR-ekben a változásokhoz képest is értelmezhető legyen az ellenőrzés.
- A repo gyökerében lévő **`.gitleaks.toml`** finomhangolja az alap szabálykészletet (`useDefault = true`) és **allowlist**-et ad a nem éles anyagra.

## Allowlist (`.gitleaks.toml`)

- **`backend/LowCodePlatform.Backend.Tests/`** — integrációs tesztekben szándékosan szerepelnek hamis `Auth:Jwt:SigningKey` / `Admin:ApiKey` stringek (`WebApplicationFactory`).
- **`backend/appsettings.Development.json`** — lokális dev JWT placeholder.
- **Regex:** a kódban előforduló fix dev/test placeholder szövegek (pl. `dev-insecure-signing-key-change-me`), hogy ne legyen zaj a valódi titkok kiszűrése mellett.

Új teszt- vagy példafájloknál, ha Gitleaks false positive-ot ad, előbb érdemes **allowlist**-et szűken bővíteni (útvonal vagy fingerprint), nem kikapcsolni a szabályt.

## Lokálisan

Gitleaks [telepítése](https://github.com/gitleaks/gitleaks#installing) után a repo gyökeréből:

```bash
gitleaks detect --source . --verbose
```

Kapcsolódó: [`ci-supply-chain.md`](ci-supply-chain.md) (NuGet), [`DEVELOPMENT_WORKFLOW.md`](../DEVELOPMENT_WORKFLOW.md).
