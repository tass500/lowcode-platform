# Konténer / Helm (Iter 52)

## Docker Compose (helyi / lab)

A repo gyökeréből:

```bash
docker compose -f deploy/docker/docker-compose.yml up --build
```

- **UI + API proxy:** http://localhost:8080 — az nginx a `/api/*` kéréseket a backend konténernek adja (8080).
- **Tenant:** `localhost` host esetén a backend a **`default`** tenant slugot használja (`TenantContext`), a compose példa ehhez ad `Tenancy__Secrets__default` connection stringet.
- **Adat:** SQLite fájlok a `lowcode-data` Docker volume-ban (`/data` a backend konténerben).
- **JWT signing key:** a compose-ban **csak** demó — élesben Secret / env injektálás.

## Image build (külön)

```bash
docker build -f deploy/docker/Dockerfile.backend -t lowcode-platform/backend:latest .
docker build -f deploy/docker/Dockerfile.frontend -t lowcode-platform/frontend:latest .
```

## Helm

Chart: `deploy/helm/lowcode-platform/`

```bash
helm install lcp deploy/helm/lowcode-platform --namespace lowcode --create-namespace
```

- **Backend:** `emptyDir` SQLite — fejlesztői / demó; élesben **PVC** vagy **SQL Server** connection string (`docs/live/sqlserver-platform.md`).
- **Frontend:** nginx ConfigMap; az API felé a `ClusterIP` backend service neve kerül a proxy `proxy_pass`-ba.
- **Ingress:** alapból `ingress.enabled: false`; bekapcsolás: `values.yaml` / `--set ingress.enabled=true`.

## CI

A GitHub Actions (`ci.yml`) lefuttatja a két image Docker buildjét (push nélkül) és a `helm template` ellenőrzést.

## Kapcsolódó

- SQL Server platform DB: [`docs/live/sqlserver-platform.md`](sqlserver-platform.md)
- Inbound webhook: [`docs/live/workflow-inbound-trigger.md`](workflow-inbound-trigger.md)
