# BFF + httpOnly cookie session (iter **62c**)

> **Állapot:** **Fázis B + C + D kész:** backend (`BffAuthController`, session süti, **`BffSessionBearerMiddleware`**) + Angular: **`BffAuthStateService`** + **`APP_INITIALIZER`** → `GET /api/auth/bff/meta`; **`api-auth.interceptor`**: BFF módban **`withCredentials: true`**, **nem** küld `Authorization`-t `sessionStorage`-ból, opcionálisan **`X-Tenant-Id`** ha van tenant a session store-ban; **`/lowcode/auth`**: BFF login link, session infó, logout, `bff_error` query. Összhang: [`oidc-jwt-bearer.md`](oidc-jwt-bearer.md), [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md).

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

## Frontend (Angular) — megvalósítás (Fázis D)

- **`bff-auth-state.service.ts`:** induláskor `GET /api/auth/bff/meta` → `useCookieAuth()` = `enabled && configured`.
- **`api-auth.interceptor.ts`:** ha `useCookieAuth()`, minden `/api/*` kérés **`withCredentials: true`**; **nincs** `Authorization` a low-code session store-ból; ha van **`tenantSlug`** a store-ban, megy az **`X-Tenant-Id`** (dev override).
- **`lowcode-auth-page.component.ts`:** BFF szekció (login link a `meta.loginPath`-ra), session JSON (`GET /api/auth/bff/session` + credentials), **`POST /api/auth/bff/logout`**; SPA OIDC blokk **elrejtve** BFF módban; figyelmeztetés, hogy a dev / paste token nem megy az API-ra BFF módban.

## Együttélés a jelenlegi rendszerrel

- **dev-token**, **tenant API key**, **szimmetrikus JWT**, **közvetlen OIDC Bearer** maradhat párhuzamosan konfiguráció szerint.
- **62b2** SPA flow változtatása: BFF módban kikapcsolni / elrejteni a token-mentő UI-t, hogy ne legyen két „igazság”.

## Implementációs fázisok

1. **Fázis A** — dokumentáció + konfig séma ✅
2. **Fázis B** — BFF login / callback / session süti / `meta` / `session` / `logout` + integrációs tesztek ✅ (`BffAuthEndpointsTests`)
3. **Fázis C** — `BffSessionBearerMiddleware` + `IBffSessionReader` / `BffSessionReader`; integrációs teszt: cookie → `/api/workflows` ✅ (`BffSessionBearerWorkflowTests`)
4. **Fázis D** — Angular: `meta` + `APP_INITIALIZER`, `withCredentials`, BFF login / session / logout UI, SPA OIDC elrejtése BFF módban ✅

## DoD (implementáció PR-hez)

- Threat model röviden leírva a PR-ban (XSS, CSRF mitigáció).
- Tesztek: legalább egy **integrációs** út (login flow vagy session parse); frontend `npm run build`.
- CodeQL: ne vezessünk be új „cleartext token a Storage-ban” útvonalat BFF módban; a régi SPA útvonal maradhat opcionális.

## Kapcsolódó kód

- **BFF:** `backend/Controllers/BffAuthController.cs`, `backend/Auth/Bff/*`, `backend/Middleware/BffSessionBearerMiddleware.cs`, `Program.cs` (`BffAuthOptions`, `IOidcHttpForBff`, `IBffSessionReader`).
- **Fázis D (frontend):** `frontend/src/app/lowcode/bff-auth-state.service.ts`, `frontend/src/app/app.config.ts` (`APP_INITIALIZER`), `frontend/src/app/lowcode/api-auth.interceptor.ts`, `frontend/src/app/pages/lowcode-auth-page.component.ts`.

**Megjegyzés:** élesben a **Data Protection** kulcsok perzisztenciája kötelező (különben restart után érvénytelenek a sütik) — lásd ASP.NET Data Protection key ring.

## 62c+ — helyi dev smoke (Angular proxy + opcionális IdP)

Cél: **ugyanazon böngésző-origin** alatt legyen az SPA és az `/api/*` hívás, hogy a **httpOnly** session süti (`Path=/`) a `localhost:4200`-hoz tartozzon és minden API kérésnél menjen (`withCredentials`).

### 1. Backend (Development)

- Indítás: `dotnet run` a `backend` mappból (alap URL: `http://localhost:5002` — `Properties/launchSettings.json`, profil `http`).
- **BFF kapcsoló:** `Auth:Bff:Enabled` = `true` (pl. `appsettings.Development.json`, vagy **User Secrets** / env, hogy ne kerüljön fel a repóba az éles IdP adat).
- **OIDC a BFF loginhez** (hogy a `GET /api/auth/bff/meta` **`configured: true`** legyen): ugyanaz a három, mint a SPA-hoz: `Auth:Oidc:Authority`, `Auth:Oidc:SpaClientId`, `Auth:Oidc:SpaScope` (opcionálisan `Auth:Oidc:TenantClaimSource` stb. — lásd [`oidc-jwt-bearer.md`](oidc-jwt-bearer.md)).

### 2. Angular dev szerver + proxy

- A repo `frontend/angular.json` már beállítja: `serve` → `proxyConfig`: `proxy.conf.json` (a `/api` kérések továbbítva a backendre, pl. `http://localhost:5002`).
- Indítás: `npm start` vagy `ng serve` a `frontend` mappból → tipikusan `http://localhost:4200`.

### 3. OAuth `redirect_uri` és a dev proxy `Host` fejléce

A BFF a **`Host` fejléc** alapján építi a `redirect_uri`-t (`BffAuthController`: `PublicBaseUrl()` + `CallbackPath`).

- A repo **`frontend/proxy.conf.json`** beállítása: **`changeOrigin: false`** az `/api` proxynál. Így a backend a böngésző által látott hostot kapja (tipikusan `localhost:4200`), ezért a **`redirect_uri`**:

  `http://localhost:4200/api/auth/bff/callback`

  Ezt kell az IdP alkalmazásban (publikus SPA client, PKCE) **szerepeltetni**.

- Ha valaki **`changeOrigin: true`**-ra állítja a proxyt, a backend gyakran **`localhost:5002`**-t lát `Host`-ként → a `redirect_uri` **`http://localhost:5002/api/auth/bff/callback`** lesz; akkor **azt** kell az IdP-n regisztrálni, és a callback is a **5002**-re érkezik (nem keverendő a **4200**-as SPA originnel).

- Közvetlenül a backendre (`http://localhost:5002/...`) böngészve — proxy nélkül — szintén a **5002**-es `redirect_uri` érvényes.

### 4. Smoke checklist

1. Backend fut + `GET http://localhost:5002/api/auth/bff/meta` (vagy proxyn át `4200`-ról) → `enabled` / `configured` elvárás szerint.
2. SPA: `http://localhost:4200/lowcode/auth` → BFF szekció, **„Belépés BFF-fel”** (ha `configured`), majd IdP → callback → süti → `POST /api/auth/bff/session` / UI „Session infó”.
3. **BFF kijelentkezés** után a süti törlődik; workflow API-k újra 401, amíg nincs más auth.

### 5. IdP nélküli gyors ellenőrzés

- `Auth:Bff:Enabled` = `true`, de OIDC hiányzik → `meta.configured` = `false`, BFF login link nem aktív — ezzel is ellenőrizhető, hogy a végpontok és kapu működnek, **teljes OAuth nélkül**.
