# Week 5 Docker stack

From the repository root, copy `infra/docker/.env.example` to `infra/docker/.env` and replace both passwords with private values. Do not commit `.env`.

```powershell
docker compose -f infra/docker/docker-compose.yml up -d --build
docker compose -f infra/docker/docker-compose.yml ps
pwsh tests/smoke/smoke-test.ps1
```

Web: http://localhost:3000; API readiness: http://localhost:5000/health/ready; MailHog: http://localhost:8025. The API applies migrations when `Database__MigrateOnStartup=true`. `VITE_API_URL` is embedded into the web build and must be reachable from the browser.

The current smoke script covers the Task API baseline only. Notifications, Task v2, and Dashboard are not implemented on this branch; the complete Week 5 smoke flow must be added after those endpoints land.
