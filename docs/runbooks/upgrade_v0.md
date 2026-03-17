# Upgrade runbook v0

## Cél

Ez a runbook az első, end-to-end upgrade vertical slice-hoz tartozik: upgrade run indítás, állapotfigyelés, failure triage és restore drill v0.

## Happy path

- Backend indítás:
  - `dotnet run` a `backend/` mappában (Swagger: `http://localhost:5002/swagger`)
- Frontend indítás:
  - `npm start` a `frontend/` mappában (UI: `http://localhost:4200/upgrade`)

- API smoke (Swagger vagy curl):
  - `GET /api/admin/installation/status`
  - `POST /api/admin/upgrade-runs` body: `{ "targetVersion": "0.2.0" }`
  - `GET /api/admin/upgrade-runs/{id}` amíg `succeeded`

## Failure triage

- `GET /api/admin/upgrade-runs/{id}`
  - nézd a `steps[].lastErrorCode/lastErrorMessage` mezőket
  - nézd a `traceId`-t

- UI shortcut (`/upgrade`):
  - Állíts be `Client trace id`-t (opcionális), hogy a kérések és a szerver audit összeköthető legyen.
  - `Copy curl snippets`: másold ki a releváns curl parancsokat a gyors repro-hoz.
  - `Copy incident bundle`: ticketbe illeszthető JSON snapshot (status/queue/observability/run/audit + curl snippetek).
  - `Run debug pack`: letölthető JSON csomag (incident bundle + ticket markdown + curl snippetek + audit export preview + filter snapshot).
  - Audit panel:
    - `Copy audit list URL` / `Copy audit export URL`: direkt linkek a jelenlegi filterekkel.
    - `Copy full audit json` / `Download full audit json`: export (max limitre figyelve).

- `POST /api/admin/upgrade-runs/{id}/retry` újrapróbálás.

### On-call checklist (gyors triage)

- UI: `http://localhost:4200/upgrade`
  - (Opcionális) állíts be `Client trace id`-t, ha reprodukálni / újrahívni fogsz.
  - `Load latest` / `Load run`: azonosítsd a releváns run-t.
  - `Copy incident bundle` és/vagy `Run debug pack`: csatold tickethez.
  - Audit panel:
    - `Copy audit list URL` / `Copy audit export URL`
    - (ha kell) `Copy full audit json` / `Download full audit json`

- API smoke (ha a UI nem elérhető)
  - `GET /api/admin/installation/status`
  - `GET /api/admin/observability`
  - `GET /api/admin/upgrade-runs/queue`
  - `GET /api/admin/upgrade-runs/latest`
  - (ha van runId) `GET /api/admin/upgrade-runs/{id}`

- Retry
  - `POST /api/admin/upgrade-runs/{id}/retry`

Megjegyzés:
- A backend minden válaszban visszaad `X-Trace-Id` header-t; ha küldesz is `X-Trace-Id`-t, akkor azzal fog menni végig.

## Restore drill v0 (checklist)

- Mentés/backup elérhetőségének ellenőrzése.
- Restore lépések dokumentálása (ki, mikor, milyen környezeten).
- Restore után smoke: `/health`, `/api/admin/installation/status`.
