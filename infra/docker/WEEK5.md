# Week 5 Docker stack

From the repository root, copy `infra/docker/.env.example` to `infra/docker/.env` and replace both passwords with private values. Do not commit `.env`.

```powershell
docker compose -p pmai-t5 -f infra/docker/docker-compose.yml up -d --build --wait
docker compose -p pmai-t5 -f infra/docker/docker-compose.yml ps
pwsh tests/smoke/smoke-test.ps1 -Mode Baseline
```

Web: http://localhost:3000; API readiness: http://localhost:5000/health/ready; MailHog: http://localhost:8025. The API applies migrations when `Database__MigrateOnStartup=true`. `VITE_API_URL` is embedded into the web build and must be reachable from the browser.

Use the same project name on every Compose command. This keeps the database volume separate from unrelated Compose projects. Published ports must be free. Changing POSTGRES_PASSWORD does not change a password already stored in an existing volume; do not delete an existing volume to fix authentication errors.

The API image checks HTTP /health/ready, including database connectivity. The web image checks nginx HTTP and MailHog checks its HTTP UI. The API runs as the built-in non-root app user.

The smoke script defaults to Week5 and fails if the contracted endpoints are missing. Baseline explicitly covers the currently merged Identity, Projects, Tasks, Sprints and RiskAI endpoints. Week5 additionally asserts estimated hours, edits, notifications and unread counts, ownership checks, dashboard totals, and Done progress. It polls notifications for at most five seconds. Both modes verify UTC normalization and negative authorization cases.

To check the AI outage path, stop only this project's ai-service, run the script with -AiState Unavailable, then start ai-service again. Each run creates unique users and a project; use a disposable database.

CI builds all images, waits for the stack, verifies HTTP and the API user, runs Baseline with AI on and off, uploads container logs and removes only its own disposable stack. After Notifications, Task v2 and Dashboard land, run workflow_dispatch with smoke_mode=Week5 and make Week5 the default CI mode. The Notifications EF pending-model check must be added with the real DbContext/factory, not a placeholder. Until that full run passes, PMS-168 remains incomplete.
