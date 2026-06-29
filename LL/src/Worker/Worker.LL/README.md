# Worker.LL

`Worker.LL` is the durable background worker for the main Legends Legacy backend.

Run it as a separate deployable process beside `API.LL`:

```text
API.LL     -> HTTP/API/SignalR
Worker.LL  -> Quartz scheduled jobs
PostgreSQL -> game data, BackgroundJobExecutions, and Quartz QRTZ tables
```

Initial deployment should use one worker replica. Quartz clustering is enabled so the worker can later scale to multiple replicas against the same PostgreSQL job store.

Production and local connection strings should come from the same secret or environment-variable flow as the API. The checked-in `appsettings.json` leaves `ConnectionStrings:LegendsLegacyDB` empty on purpose.

The worker registers no-op realtime publisher/broadcaster adapters. Scheduled jobs should persist authoritative state; API/SignalR clients can observe that state through normal API refresh/realtime paths rather than the worker hosting a SignalR hub.

The Helm chart supports shared backend env vars through `env` and worker-only env vars through `worker.env`.

`Worker.LL` is the canonical Quartz host. The older placeholder `Services.Quartz` and `Persistence.Quartz` projects were removed to avoid ambiguity.
