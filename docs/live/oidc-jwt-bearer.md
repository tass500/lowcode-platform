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
| `Auth:Oidc:TenantClaimSource` | Nem | Vesszővel elválasztott claim típusok; az **első nem üres** érték a tokenből **`tenant`** claimként hozzáadódik (ha még nincs `tenant`). |
| `Auth:Oidc:GrantAdminIfRoleContains` | Nem | Vesszővel elválasztott részsztringek; ha bármely **role** jellegű claim értéke tartalmaz valamelyiket (nem case-sensitive), hozzáadódik a **`admin`** szerep. |
| `Auth:Oidc:SpaClientId` | SPA OIDC UI-hoz | Publikus SPA client id; ha üres, a `GET /api/auth/spa-oidc-config` 404. |
| `Auth:Oidc:SpaScope` | Nem | Alapértelmezés: `openid profile offline_access` (refresh tokenhez érdemes `offline_access`). |
| `Auth:Oidc:SpaRedirectPath` | Nem | Relatív útvonal a böngésző redirect URI-hoz; alap: `/lowcode/auth/callback` (regisztráld az IdP-n: `{origin}{path}`). |
| `Auth:SpaOidcConfig:Enabled` | Nem | Ha `true`, a `spa-oidc-config` endpoint **nem Development/Testing** környezetben is elérhető (egyébként csak ott). |

Ha **`Auth:Oidc:Authority` üres**, az OIDC JWT séma regisztrálva marad, de **nincs** `Authority` kötve; a kérések a szimmetrikus `Bearer` sémára esnek.

## Működés

- **Policy scheme** `LcpJwtForwarder`: a Bearer token **olvasatlan aláírású** `iss` mezője alapján választ a **szimmetrikus** `Bearer` és az **`OidcJwt`** séma között (authority prefix vagy `ValidIssuers` egyezés).
- **Szimmetrikus** JWT továbbra is `JwtBearerIssuerAudiencePostConfigure`-on keresztül kap kulcsot / opcionális `Auth:Jwt:Issuer` + `Audience`-t.
- **OIDC** JWT: `OidcJwtBearerPostConfigure` tölti az `Authority` / `Audience` / meta mezőket a teljes konfigurációból (integrációs tesztek in-memory felülírásaival összhangban), és OIDC esetén **`OnTokenValidated`**-ben fut a **`OidcJwtClaimMapping`** (tenant / admin, lásd fenti kulcsok).

## SPA (frontend, code + PKCE)

- **`GET /api/auth/spa-oidc-config`**: nem titkos mezők (`authority`, `clientId`, `scope`, `redirectPath`, `tenantClaimSources`) — csak ha Development, Testing, vagy `Auth:SpaOidcConfig:Enabled`, **és** be van állítva `Auth:Oidc:Authority` + `Auth:Oidc:SpaClientId`.
- Útvonal: **`/lowcode/auth`** (gomb) → IdP → **`/lowcode/auth/callback`** → munkamenet (**`sessionStorage`**, tab hatókör) + opcionális **refresh token** + token endpoint az interceptor számára (régi `localStorage` kulcs egyszer átmásolódik).
- Ha az id tokenben nincs tenant a konfigurált claim források szerint, a **`tenantSlug` üres** maradhat; a low-code auth oldalon kézzel megadható, majd „Save token” / dev token flow frissíti.

## Üzemeltetői megjegyzések

- A **`tenant_user`** policy továbbra is **`tenant` claim**-et vár az **access tokenben** (JWT bearer) — mappinggel vagy natív claimmel.
- **`admin`** szerep: `GrantAdminIfRoleContains` **vagy** tokenben lévő **`admin`** role claim.

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

## Keményítés később (BFF + httpOnly) — backlog **62c**

A jelenlegi Angular flow **szándékosan** a klasszikus **public SPA + PKCE** modellt követi: a token(ek) a böngészőben maradnak (`sessionStorage`), ami devhez és gyors iterációhoz egyszerű, de **XSS** esetén sérülékenyebb mint a **Backend-for-Frontend (BFF)** minta, ahol a böngésző csak **httpOnly, Secure, SameSite** süti(ket) kap, a refresh/access csak a szerver oldalon mozog, az API felé pedig a BFF tesz ki sessiont vagy rövid élettartamú szervert oldali tokent.

**Most nem „hibás” kihagyni:** ez új végpontokat, cookie policyt, CSRF védelmet (ha szükséges), gyakran reverse proxy / same-site routing döntéseket igényel — külön PR-érdemű (**62c**), ne a 62b2 funkciócsomagba keverjük.

Rögzítve a következő lépések között: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md) backlog **62c**.
