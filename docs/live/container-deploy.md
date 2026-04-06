# Konténer / Helm (Iter 52 + 53)

## Docker Compose (helyi / lab)

A repo gyökeréből:

```bash
docker compose -f deploy/docker/docker-compose.yml up --build -d
```

Háttérben futtatáshoz a `-d` (detached) zászló; logok: `docker compose -f deploy/docker/docker-compose.yml logs -f`.

**Windows (Docker Desktop):** indítsd a **Docker Desktop**ot és várd meg, amíg az engine fut (System tray: *Docker Desktop is running*). Ha a `docker compose` parancs nem érhető el (régebbi CLI), használd ugyanígy a **`docker-compose`** binárist (kötőjel):

```bash
docker-compose -f deploy/docker/docker-compose.yml up --build -d
```

A `.dockerignore` kizárja a **`frontend/.angular`** cache mappát — nélküle a build context Windows-on könnyen több száz MB, és a `docker build` megbízhatatlan lehet.

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

Chart: `deploy/helm/lowcode-platform/` (chart verzió **0.3.0+**: opcionális backup CronJob + Secret + PVC alapértelmezés).

```bash
helm install lcp deploy/helm/lowcode-platform --namespace lowcode --create-namespace
```

- **JWT:** a chart alapból **Kubernetes Secret**-et hoz létre (`…-backend-secret`, kulcs `jwt-signing-key`), a Deployment `valueFrom`-mal olvassa. Prod: `backend.secrets.existingSecret` + előre létrehozott Secret; ne commitolj erős kulcsot a `values.yaml`-ba.
- **Adat:** alapból **PVC** (`…-backend-data`), SQLite fájlok `/data`-n. Gyors demó pod újraindítás nélkül: `backend.dataVolume.type=emptyDir`.
- **Tenant platform DB — SQL Server:** `backend.database.tenantProvider=sqlserver` + connection string a chart Secretben (`backend.secrets.sqlserverTenantConnectionString`) vagy a `existingSecret` `tenant-connection-string` kulcsában; opcionálisan `LCP_SQLSERVER_ENSURE_CREATED` a chart `backend.database.sqlserver.ensureCreated` flaggel. Részletek: [`sqlserver-platform.md`](sqlserver-platform.md).
- **Frontend:** nginx ConfigMap; az API felé a `ClusterIP` backend service neve kerül a proxy `proxy_pass`-ba.
- **Ingress:** alapból `ingress.enabled: false`; bekapcsolás: `values.yaml` / `--set ingress.enabled=true`.
- **SQLite backup (opcionális):** `backup.enabled=true` — CronJob `kubectl cp` a backend pod `/data` fájljaira egy backup PVC-re (`backup.persistence`). Feltétel: `backend.replicaCount=1`. SQL Server tenant adat: natív DB backup / üzemeltetői policy, nem ez a CronJob.

**k3s / k3d / Pi / arm64 build példák:** [`k3s-home-lab.md`](k3s-home-lab.md)

## CI

A GitHub Actions (`ci.yml`) lefuttatja a két image Docker buildjét (push nélkül) és a `helm lint` / `helm template` ellenőrzést.

## Kapcsolódó

- SQL Server platform DB: [`sqlserver-platform.md`](sqlserver-platform.md)
- Inbound webhook: [`workflow-inbound-trigger.md`](workflow-inbound-trigger.md)
- Home-lab Helm: [`k3s-home-lab.md`](k3s-home-lab.md)
