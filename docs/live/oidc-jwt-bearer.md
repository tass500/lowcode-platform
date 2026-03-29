# OIDC JWT Bearer (iter 62b, MVP)

## Cél

Külső **OpenID Connect** (OAuth2) kibocsátó által aláírt **access tokenek** elfogadása ugyanazzal a `Authorization: Bearer …` fejléccel, mint a lokális **szimmetrikus** JWT / `dev-token` esetén.

## Konfiguráció (`appsettings` / env)

| Kulcs | Kötelező | Leírás |
|--------|----------|--------|
| `Auth:Oidc:Authority` | Igen (OIDC használathoz) | IdP base URL (issuer prefix egyeztetéshez is használjuk). |
| `Auth:Oidc:Audience` | Ajánlott | JWT `aud` (pl. API App ID URI). |
| `Auth:Oidc:MetadataAddress` | Nem | Felülírja a discovery URL-t, ha nem a szokásos `{Authority}/.well-known/openid-configuration`. |
| `Auth:Oidc:ValidIssuers` | Nem | Vesszővel elválasztott **pontos** `iss` értékek; ha van, ezek is irányítják az OIDC sémára a forwardert (authority prefix helyett / mellett). |
| `Auth:Oidc:RequireHttpsMetadata` | Nem | `true`/`false` — discovery meta HTTPS igénylése. |

Ha **`Auth:Oidc:Authority` üres**, az OIDC JWT séma regisztrálva marad, de **nincs** `Authority` kötve; a kérések a szimmetrikus `Bearer` sémára esnek.

## Működés

- **Policy scheme** `LcpJwtForwarder`: a Bearer token **olvasatlan aláírású** `iss` mezője alapján választ a **szimmetrikus** `Bearer` és az **`OidcJwt`** séma között (authority prefix vagy `ValidIssuers` egyezés).
- **Szimmetrikus** JWT továbbra is `JwtBearerIssuerAudiencePostConfigure`-on keresztül kap kulcsot / opcionális `Auth:Jwt:Issuer` + `Audience`-t.
- **OIDC** JWT: `OidcJwtBearerPostConfigure` tölti az `Authority` / `Audience` / meta mezőket a teljes konfigurációból (integrációs tesztek in-memory felülírásaival összhangban).

## Üzemeltetői megjegyzések

- A **`tenant_user`** policy továbbra is **`tenant` claim**-et vár — az IdP-nak vagy a tokennek tartalmaznia kell (vagy később claim mapping / B2C policy).
- **`admin`** szerep: a tokenben szereplő **role** claim(ek)nek egyezniük kell a platform elvárásával (`ClaimTypes.Role` / `roles`).

## Példa (Azure AD–stílus, illusztráció)

```json
"Auth": {
  "Oidc": {
    "Authority": "https://login.microsoftonline.com/{tenantId}/v2.0",
    "Audience": "api://{application-id-uri}"
  }
}
```

Éles beállítás előtt ellenőrizd az **issuer** és **audience** pontos értékét a tenant és az app regisztráció szerint.
