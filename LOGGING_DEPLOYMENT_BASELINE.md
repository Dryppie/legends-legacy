# LegendsLegacy Logging Deployment Baseline

Status: Phase 0 source inspection complete; live-cluster measurements pending  
Date: 2026-08-18  
Infrastructure source: `C:\repos\Legends-Legacy\ll-infrastructure` (inspected read-only)

## Confirmed deployment conventions

- The only checked-in environment is `dev`. The platform chart is `ll-platform-dev` and its Argo CD child application is `ll-app`.
- Argo CD runs the application in namespace `ll`, enables namespace creation, and consumes the aggregate `ll-app` Helm chart from the private GHCR OCI registry.
- The aggregate chart pins separate frontend, backend, chat, and LiveOps component chart versions. At inspection time they were backend `0.1.245`, chat `0.1.29`, frontend `0.1.284`, and LiveOps `0.0.4`.
- Desired dev replicas are one each for backend, worker, chat, and LiveOps. The cloudflared Deployment requests two replicas.
- Public traffic enters through Traefik. The Cloudflare tunnel is remotely managed; Git contains the cloudflared workload but not hostname routing policy.
- The main API and Chat explicitly run with `ASPNETCORE_ENVIRONMENT=Development` in the checked-in dev values. LiveOps explicitly runs as Production. The generic-host worker has no `DOTNET_ENVIRONMENT` override and therefore uses its normal Production default unless the live environment adds one.
- Application Secrets for registry access, system chat, and LiveOps use a 1Password-to-SealedSecret generation workflow. Existing infrastructure documentation separately identifies legacy database credentials that still need rotation into that workflow; no credential values are recorded here.
- PostgreSQL uses a static 5 GiB `hostPath` PersistentVolume, `Retain` reclaim policy, and no StorageClass. No volume-expansion or snapshot convention is present in Git.
- No Loki, Grafana, Alloy, Prometheus, or other observability manifests are currently checked in.

## Decisions for the next infrastructure change

1. Keep the logging stack in `ll-infrastructure`; this application repository supplies only JSON output, correlation, tests, and collector-friendly pod metadata.
2. Use the existing `ll` application namespace for discovery, but deploy Loki, Alloy, and Grafana into a dedicated `observability` namespace managed by Argo CD.
3. Use the existing SealedSecret workflow for Loki object-storage credentials and Grafana bootstrap credentials. Do not add plaintext Secret data to Git.
4. Do not place durable Loki chunks on the current 5 GiB PostgreSQL hostPath volume. Prefer a private, bucket-scoped Cloudflare R2 bucket; keep only Loki working data on a small local PVC after the live StorageClass is confirmed.
5. Set `cluster=ll-dev` and `environment=dev` explicitly in Alloy configuration for this single-environment cluster. If another environment is added, introduce an explicit pod/environment label rather than inferring it from image tags.
6. Use the pod labels emitted by the application charts for `service`, `component`, and `version`; keep version, pod, node, route, trace ID, and account/character IDs out of Loki labels.
7. Pin Loki, Alloy, and Grafana chart and image versions before implementation. Do not use floating tags for the observability stack.

## Live checks still required

`kubectl` is not installed in the current workspace, so none of the following were claimed from source alone:

- actual namespace, Argo health, pod, and node state;
- node count, allocatable CPU/memory/disk, container runtime, or container-log rotation;
- available StorageClasses, expansion, CSI snapshot support, or free disk capacity;
- current pod labels after the latest component release;
- representative 30–60 minute log byte rates by workload;
- current Traefik and cloudflared runtime versions or live tunnel routes.

Before sizing or deploying Phase 2, run the read-only inventory in section 7 of `LOGGING_SYSTEM_PLAN.md`, measure byte counts without retaining raw logs, and compare live Argo desired state to this source baseline.

## Initial capacity gate

Do not choose Loki limits from guesses. Record bytes per 30–60 minutes for backend, worker, chat, LiveOps, frontend, Traefik, cloudflared, Argo CD, and Kubernetes events; extrapolate daily volume with a peak factor; then size retention, ingestion limits, R2 lifecycle, and the local working PVC. Phase 2 remains blocked until those measurements and external R2 authorization exist.
