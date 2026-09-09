# LiveOps Data Protection storage

The chart mounts a persistent volume claim at
`/home/app/.aspnet/DataProtection-Keys` and supplies
`LiveOps__DataProtection__KeyRingPath` to the API. The API requires an absolute
key directory outside Development and uses a stable application name scoped to
the ASP.NET environment. Every replica must use the same environment and claim.
Development can omit the path to use ASP.NET's local key storage defaults.

By default the chart creates a 1 GiB ReadWriteOnce claim using the cluster's
default storage class. The Recreate deployment strategy allows the old pod to
release the volume before its replacement starts. There is no ephemeral fallback:
a claim that cannot bind leaves the pod pending until storage is available.

The infrastructure repository can override these component values:

```yaml
dataProtection:
  existingClaim: "" # Set to an existing, dedicated writable PVC to reuse it.
  size: 1Gi
  storageClass: null # Cluster default; use "" for a pre-provisioned classless PV.
  accessModes:
    - ReadWriteOnce
```

The chart rejects multiple replicas unless `accessModes` includes ReadWriteMany.
For multiple replicas, the storage class or existing claim must actually support
ReadWriteMany across the nodes running LiveOps. With `existingClaim`, the chart
does not create or resize a PVC; the listed access modes describe that claim.

## Rollout and retention

- Publish the updated API image and chart together, then update the component
  version through the infrastructure repository's normal release process.
  No database migration is required. This repository change does not deploy it.
- Ensure a default storage class exists, or configure `storageClass` or
  `existingClaim`. The volume must allow the container UID/GID 1654 to read,
  create, and write keys; the chart already sets `fsGroup: 1654`.
- The first rollout starts a new durable key ring and application identity.
  Existing sessions require sign-in again; stale antiforgery cookies may log
  one last decryption error while ASP.NET replaces them. Missing old keys cannot
  be recovered from the cookie.
- Chart-created PVCs have `helm.sh/resource-policy: keep` to retain keys on Helm
  uninstall. Before reinstalling, select the retained claim via `existingClaim`.
  Preserve the claim when renaming a release or rolling back to an older chart;
  the old chart still uses temporary keys and will invalidate new sessions.
- Keep each environment's key ring separate. Restrict volume and backup access
  to LiveOps operators and its runtime identity, and use encrypted storage and
  backups. File-system persistence does not itself encrypt the key XML at rest.
  Do not delete retired keys while cookies protected by them may still be used.

After rollout, sign in, confirm `/auth/antiforgery` succeeds, replace the pod
through the normal deployment process, and confirm the same browser session
and subsequent cookie-authenticated actions still work. Check that the replacement
pod mounts the same PVC and that missing-key errors do not recur for new cookies.

References: [ASP.NET Data Protection configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0),
[Kubernetes persistent volumes](https://kubernetes.io/docs/concepts/storage/persistent-volumes/).
