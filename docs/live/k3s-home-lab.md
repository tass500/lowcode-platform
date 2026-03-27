# k3s / k3d — home-lab telepítés (Iter 53)

## Előfeltétel

- [k3s](https://docs.k3s.io/) vagy [k3d](https://k3d.io/) klaszter, `kubectl` és [Helm 3](https://helm.sh/).

## Helm (chart a repóban)

```bash
helm upgrade --install lcp deploy/helm/lowcode-platform \
  --namespace lowcode --create-namespace \
  --set backend.secrets.jwtSigningKey="$(openssl rand -base64 48)"
```

- **PVC:** alapból a backend `/data` alá kap egy `PersistentVolumeClaim`-et (SQLite: `management.db`, `tenant-default.db`). Demóhoz: `--set backend.dataVolume.type=emptyDir`.
- **JWT:** érdemes nem a `values.yaml`-ban hagyni a kulcsot; fenti példa parancssorban adja át. Prod: külön Secret (`backend.secrets.existingSecret`) + [external-secrets](https://external-secrets.io/) / Sealed Secrets stb.

### Meglévő Secret

```bash
kubectl -n lowcode create secret generic lcp-backend-secrets \
  --from-literal=jwt-signing-key='YOUR_LONG_RANDOM_SECRET' \
  --dry-run=client -o yaml | kubectl apply -f -

helm upgrade --install lcp deploy/helm/lowcode-platform \
  --namespace lowcode \
  --set backend.secrets.create=false \
  --set backend.secrets.existingSecret=lcp-backend-secrets
```

SQL Server tenant esetén a ugyanebben a Secretben legyen még:

- `tenant-connection-string` — lásd [`sqlserver-platform.md`](sqlserver-platform.md)

```bash
kubectl -n lowcode create secret generic lcp-backend-secrets \
  --from-literal=jwt-signing-key='...' \
  --from-literal=tenant-connection-string='Server=tcp:...;Database=...;User Id=...;Password=...;TrustServerCertificate=True' \
  --dry-run=client -o yaml | kubectl apply -f -

helm upgrade --install lcp deploy/helm/lowcode-platform \
  --namespace lowcode \
  --set backend.secrets.create=false \
  --set backend.secrets.existingSecret=lcp-backend-secrets \
  --set backend.database.tenantProvider=sqlserver \
  --set backend.database.sqlserver.ensureCreated=true
```

(`ensureCreated` csak greenfield / lab; élesben cél a provider-specifikus EF migráció, lásd `03` ütemterv **56**.)

### Opcionális SQLite fájl backup (Helm 0.3.0+)

- `helm upgrade ... --set backup.enabled=true` — heti/időzített CronJob `kubectl cp`-vel: `management.db` (+ `tenant-default.db`, ha `tenantProvider=sqlite`) → backup PVC vagy `emptyDir` (nem marad meg podon kívül).
- **Feltétel:** `backend.replicaCount=1` (a chart `helm install` ezt kényszeríti, ha `backup.enabled`).
- **SQL Server** tenant DB: natív backup / snapshot / üzemeltetői szabály — [`sqlserver-platform.md`](sqlserver-platform.md).

## k3d példa (egy gépen)

```bash
k3d cluster create lowcode --port "8080:80@loadbalancer"
# várj, amíg a LB kész; majd Ingress vagy port-forward a frontend service-re
kubectl create namespace lowcode
helm upgrade --install lcp deploy/helm/lowcode-platform -n lowcode --create-namespace
kubectl -n lowcode port-forward svc/lcp-lowcode-platform-frontend 8080:80
```

## Raspberry Pi / arm64

A CI jelenleg **amd64** image buildet futtat. arm64-hez helyben:

```bash
docker buildx build --platform linux/arm64 -f deploy/docker/Dockerfile.backend -t lowcode-platform/backend:arm64 .
docker buildx build --platform linux/arm64 -f deploy/docker/Dockerfile.frontend -t lowcode-platform/frontend:arm64 .
```

A chart `image.repository` / `image.tag` mezőivel mutass a saját registry-dre, ha nem a default `latest` amd64 image-et használod.

## Kapcsolódó

- [`container-deploy.md`](container-deploy.md) — Docker Compose + Helm összefoglaló
- [`sqlserver-platform.md`](sqlserver-platform.md) — SQL Server tenant DB
