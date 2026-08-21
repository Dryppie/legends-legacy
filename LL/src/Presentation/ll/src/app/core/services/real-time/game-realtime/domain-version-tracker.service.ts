import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DomainVersionTracker {
  private readonly latestVersions = new Map<string, number>();

  observe(versions: Readonly<Record<string, number>>): void {
    for (const [scope, version] of Object.entries(versions)) {
      if (!Number.isSafeInteger(version) || version < 1) continue;
      this.latestVersions.set(
        scope,
        Math.max(this.latestVersions.get(scope) ?? 0, version),
      );
    }
  }

  isCurrent(scope: string, version: number | undefined): boolean {
    if (version === undefined) return true;
    return version >= (this.latestVersions.get(scope) ?? 0);
  }

  latest(scope: string): number {
    return this.latestVersions.get(scope) ?? 0;
  }

  resetScope(scope: string): void {
    this.latestVersions.delete(scope);
  }

  reset(): void {
    this.latestVersions.clear();
  }
}
