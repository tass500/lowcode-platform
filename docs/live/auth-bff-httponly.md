# BFF + httpOnly cookie session (iter **62c**, terv)

> **Állapot:** specifikáció / megvalósítás **nincs** ebben a fázisban — a cél, hogy az implementáció ne induljon „fejben üresen”. Összhang: [`oidc-jwt-bearer.md`](oidc-jwt-bearer.md) § keményítés, [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md) backlog **62c**.

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

## Backend (ASP.NET) — vázlatos végpontok

*(nevek illusztráció; implementációkor egyeztetni a meglévő `AuthController` / routing konvenciókkal.)*

| Lépés | Ötlet |
|--------|--------|
| Login indítás | `GET /api/auth/bff/login` → 302 az IdP `authorize` URL-re (state, PKCE **server-side** tárolás). |
| Callback | `GET /api/auth/bff/callback?code&state` → token exchange **szerveren**, majd session süti beállítása. |
| Logout | `POST /api/auth/bff/logout` → IdP session + saját süti törlés. |
| Session állapot | `GET /api/auth/bff/session` → **nem** token, csak pl. bejelentkezve van-e, opcionális `tenant` hint (nem titok). |

A **meglévő** workflow/entity API-k továbbra is a jelenlegi **`tenant_user`** / JWT elvárásokat használhatják: a BFF (vagy egy közös middleware) a süti alapján **feltölti** a `HttpContext.User`-t vagy továbbításkor ráteszi a **Bearer** tokent **belső** hívásra (localhost / pod hálózat).

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

## Implementációs fázisok (javaslat)

1. **Fázis A — Csak dokumentáció + flag** (ez a fájl + `Auth:Bff:*` üres kötés).
2. **Fázis B — BFF login/callback + süti** (nincs még workflow hívás), integrációs teszt cookieval (ha a teszt harness engedi).
3. **Fázis C — API kérések** BFF-en keresztül Bearer-rel a meglévő policy-knek megfelelően.
4. **Fázis D — Angular** kapcsoló, E2E füstteszt.

## DoD (implementáció PR-hez)

- Threat model röviden leírva a PR-ban (XSS, CSRF mitigáció).
- Tesztek: legalább egy **integrációs** út (login flow vagy session parse); frontend `npm run build`.
- CodeQL: ne vezessünk be új „cleartext token a Storage-ban” útvonalat BFF módban; a régi SPA útvonal maradhat opcionális.

## Kapcsolódó kód (áttekintendő implementációkor)

- `backend/Controllers/AuthController.cs`, `Program.cs` (auth séma), `frontend/src/app/lowcode/api-auth.interceptor.ts`, `lowcode-session.store.ts`, `lowcode-auth-*.component.ts`.
