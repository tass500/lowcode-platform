# Frontend

This project was generated with [Angular CLI](https://github.com/angular/angular-cli) version 17.3.17.

## Development server

Run `ng serve` for a dev server. Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## Code scaffolding

Run `ng generate component component-name` to generate a new component. You can also use `ng generate directive|pipe|service|class|guard|interface|enum|module`.

## Build

Run `ng build` to build the project. The build artifacts will be stored in the `dist/` directory.

## Running unit tests

Run `ng test` to execute the unit tests via [Karma](https://karma-runner.github.io).

## End-to-end tests (Playwright)

Tests live in `e2e/`. The **CI smoke** suite is `e2e/smoke.spec.ts` only.

1. Install Chromium (once per machine): `npm run e2e:install-browsers`
2. **Recommended (repo root):** start backend + dev server via `bash scripts/e2e-smoke-ci.sh` (Unix) or `powershell -File scripts/e2e-smoke-ci.ps1` (Windows), which set `PW_NO_WEBSERVER=1` and run `npm run e2e:smoke` in this directory.
3. **Manual:** run the backend on **:5002** and `npm start` here on **:4200**, then in this directory:  
   - **Unix:** `PW_NO_WEBSERVER=1 npm run e2e:smoke`  
   - **Windows (cmd):** `set PW_NO_WEBSERVER=1&& npm run e2e:smoke`

- `npm run e2e:smoke` — smoke file only (matches **`frontend-e2e`** in CI).
- `npm run e2e:smoke:ui` / `npm run e2e:smoke:debug` — **UI mode** / **debug** for the smoke file only (same as `e2e:ui` / `e2e:debug` with `e2e/smoke.spec.ts` passed explicitly).
- `npm run e2e` — all specs under `e2e/` (may spawn servers via `playwright.config.ts` unless `PW_NO_WEBSERVER=1`).
- `npm run e2e:ui` / `npm run e2e:debug` — Playwright **UI mode** / **debug** for all specs (set `PW_NO_WEBSERVER=1` when servers are already running).
- `npm run e2e:report` — open the last **HTML** report (written under `playwright-report/` on non-CI runs; gitignored).

Full scenario list and backlog: [`docs/live/e2e-smoke-plan.md`](../docs/live/e2e-smoke-plan.md) (repo root). **CodeQL** (build-mode): [`docs/live/ci-codeql.md`](../docs/live/ci-codeql.md).

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.io/cli) page.
