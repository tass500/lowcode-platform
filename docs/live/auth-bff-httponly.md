# BFF + httpOnly cookie session (iter **62c**)

> **Állapot:** **Fázis B + C kész** a backendben: `BffAuthController` (login / callback / session / meta / logout), **Data Protection** session süti, és **`BffSessionBearerMiddleware`** — ha nincs `Authorization` fejléc, a session sütiből **Bearer** kerül a kérésre → meglévő JWT pipeline. **Fázis D:** Angular (`meta`, `withCredentials`). Összhang: [`oidc-jwt-bearer.md`](oidc-jwt-bearer.md) § keményítés, [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md) **ACTIVE 62c**.

## Cél

- A böngésző **ne tároljon** OAuth **access** / **refresh** tokeneket `localStorage` / `sessionStorage`-ban (XSS esetén ellophatók).
- A kliens csak **httpOnly** (és **Secure**, megfelelő **SameSite**) sütiket kapjon, amelyek egy **BFF** (Backend-for-Frontend) réteghez kötődnek; a BFF végzi az IdP-vel a token cserét / frissítést, és a **backend API** felé már **Authorization: Bearer**-t (vagy belső session azonosítót) tesz.

## Nem cél (ebben az iterációban)

- Teljes „zero trust” enterprise SSO minden edge case-sel.
- A jelenlegi **public SPA + PKCE** út **eltávolítása** dev környezetben — reálisabb: **feature flag** vagy külön „hardened” deploy profil.

## Feltételezett topológia

```text
[Böngésző] --same-site--> [BFF / API host, pl. egy origin alatt]
                              |
                              +--> [IdP] (authorization + token)
                              +--> [LowCode API] (meglévő JWT / policy séma)
```

- Élesben gyakori: **ugyanazon site** alatt Angular + ASP.NET (reverse proxy egy hostnév alá téve), hogy a süti **first-party** legyen.
- Külön SPA + API origin esetén a süti **cross-site** lesz → **SameSite=None; Secure** + egyéb keményítés (és gyakran mégis BFF ugyanazon origin alatt).

## Konfiguráció (`Auth:Bff`)

| Kulcs | Leírás |
|--------|--------|
| `Enabled` | `true` esetén a BFF végpontok aktívak (lásd még környezeti kapu). |
| `AllowNonDevelopment` | `true` → **Production** / egyéb környezetben is engedélyezi (alapból csak Development + Testing). |
| `CallbackPath` | OAuth `redirect_uri` path (alap: `/api/auth/bff/callback`) — **regisztráld az IdP-n** teljes URL-ként: `{origin}{CallbackPath}`. |
| `PostLoginRedirectPath` | Sikeres callback után (alap: `/lowcode/workflows`). |
| `PostLoginErrorQueryParam` | Hiba esetén query kulcs (alap: `bff_error`) a `/lowcode/auth` oldalon. |
| `SessionCookieName` / `StateCookieName` / `VerifierCookieName` | Süti nevek (alapértelmezések: `lcp.bff.*`). |

OIDC paraméterek: ugyanazok mint a SPA-hoz: `Auth:Oidc:Authority`, `Auth:Oidc:SpaClientId`, `Auth:Oidc:SpaScope`.

## Backend (ASP.NET) — végpontok (`BffAuthController`)

| Végpont | Leírás |
|--------|--------|
| `GET /api/auth/bff/meta` | `{ enabled, configured, loginPath, sessionPath, callbackPath }` — SPA egy helyen lássa, elérhető-e a BFF login. |
| `GET /api/auth/bff/login` | 302 az IdP `authorize` URL-re; PKCE verifier + state **httpOnly** rövid élettartamú sütikben (`Path=/api/auth/bff`). |
| `GET /api/auth/bff/callback` | Authorization code → token exchange; **httpOnly** session süti (`Path=/`), payload **ASP.NET Data Protection** (access + refresh + exp). |
| `GET /api/auth/bff/session` | `{ authenticated, accessTokenExpiresAtUtc, tenantHint, subjectHint }` — **nem** ad vissza nyers tokent. |
| `POST /api/auth/bff/logout` | Session süti törlése. |

A **meglévő** workflow/entity API-k változatlan `[Authorize]` / policy mellett működnek: a **`BffSessionBearerMiddleware`** (authentication **előtt**) beállítja a `Authorization: Bearer …` fejlécet a httpOnly session sütiből, ha a kliens nem küldött saját Bearert (explicit header **nyer**).

## Süti és biztonság — checklist

- **httpOnly:** kötelező (JS ne olvassa).
- **Secure:** élesben kötelező (HTTPS).
- **SameSite:** alapértelmezés **Lax** vagy **Strict** (topológia szerint); csak ha muszáj cross-site, **None + Secure**.
- **Élettartam / lejárat:** rövid élettartamú session süti + szerver oldali refresh tárolás (pl. titkosított server-side cache vagy rotáló refresh család).
- **CSRF:** ha a süti **automatikus** megy `POST` API-kra ugyanazon originon, kell **anti-forgery** (double-submit cookie vagy synchronizer token) a **state-changing** BFF végpontokra; **GET** callback csak `state` + PKCE ellenőrzéssel.
- **Titkok:** client secret csak szerveren; SPA-ban továbbra is csak **publikus** client id.

## Frontend (Angular) — irány

- OIDC redirect helyett / mellett: **teljes oldalas** navigáció a BFF login URL-re; callback **nem** Angular route-on tartja a tokeneket, hanem a szerver válaszában **Set-Cookie**.
- `api-auth.interceptor`: `withCredentials: true` a saját API felé; **ne** másoljon `Authorization`-t `sessionStorage`-ból, ha BFF mód aktív.
- Konfig: pl. `environment.useBffAuth` vagy backend `GET` feature flag — egy helyen döntsön a kliens.

## Együttélés a jelenlegi rendszerrel

- **dev-token**, **tenant API key**, **szimmetrikus JWT**, **közvetlen OIDC Bearer** maradhat párhuzamosan konfiguráció szerint.
- **62b2** SPA flow változtatása: BFF módban kikapcsolni / elrejteni a token-mentő UI-t, hogy ne legyen két „igazság”.

## Implementációs fázisok

1. **Fázis A** — dokumentáció + konfig séma ✅
2. **Fázis B** — BFF login / callback / session süti / `meta` / `session` / `logout` + integrációs tesztek ✅ (`BffAuthEndpointsTests`)
3. **Fázis C** — `BffSessionBearerMiddleware` + `IBffSessionReader` / `BffSessionReader`; integrációs teszt: cookie → `/api/workflows` ✅ (`BffSessionBearerWorkflowTests`)
4. **Fázis D** — Angular: `meta` alapján BFF login gomb, `withCredentials`, SPA token UI opcionális elrejtése BFF módban.

## DoD (implementáció PR-hez)

- Threat model röviden leírva a PR-ban (XSS, CSRF mitigáció).
- Tesztek: legalább egy **integrációs** út (login flow vagy session parse); frontend `npm run build`.
- CodeQL: ne vezessünk be új „cleartext token a Storage-ban” útvonalat BFF módban; a régi SPA útvonal maradhat opcionális.

## Kapcsolódó kód

- **BFF:** `backend/Controllers/BffAuthController.cs`, `backend/Auth/Bff/*`, `backend/Middleware/BffSessionBearerMiddleware.cs`, `Program.cs` (`BffAuthOptions`, `IOidcHttpForBff`, `IBffSessionReader`).
- **Fázis D (frontend):** `frontend/.../api-auth.interceptor.ts`, `lowcode-session.store.ts`, `lowcode-auth-*.component.ts`.

**Megjegyzés:** élesben a **Data Protection** kulcsok perzisztenciája kötelező (különben restart után érvénytelenek a sütik) — lásd ASP.NET Data Protection key ring.
