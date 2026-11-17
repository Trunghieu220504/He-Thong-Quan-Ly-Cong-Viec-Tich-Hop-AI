# PMS-168 acceptance

Run with PowerShell 7 against a disposable Docker database:

```powershell
./tests/smoke/smoke-test.ps1 -Mode Week5 -AiState Available
```

Stop only that Compose project's ai-service, repeat with `-AiState Unavailable`, then start ai-service again. Each run creates unique users and a project. Both runs must pass for full acceptance. `Baseline` deliberately tests only currently merged endpoints and cannot close PMS-168.

## Dependencies checked on 2026-11-15

Develop at `179c5df` contains task statistics and event publishing (PRs #35/#36). Docker and baseline CI landed in PR #34.

Still required before full Week5 acceptance:

- Notifications persistence/REST/event handlers: PR #38 is open. The CI migration check runs when its real DbContext lands.
- Active Sprint contract: PR #37 is open, needed by Dashboard.
- Task v2 fields and PUT endpoint: absent from develop.
- Dashboard endpoint: absent from develop.

After the dependency PRs land, update the integration branch from develop, reconcile actual endpoint contracts, run Week5 smoke with AI on/off, and change CI's default `SMOKE_MODE` from Baseline to Week5. Require successful Notifications migration validation and retained smoke transcripts, then attach that Actions run to the review. Do not replace missing modules with mocks to obtain an acceptance pass.

The Week5 script verifies edited fields again through GET, Viewer dashboard access, outsider denial, unknown-project handling, notification ownership/unread state, sprint burndown, UTC persistence and AI availability. Failure at the Task v2 prerequisite is explicit instead of a PowerShell missing-property exception.
