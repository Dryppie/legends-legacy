import { Injectable } from '@angular/core';
import {
  isStateSyncScope,
  StateSyncScope,
  StateVersionMap,
} from './game-realtime-contracts';

@Injectable({ providedIn: 'root' })
export class DomainVersionTracker {
  private readonly latestVersions = new Map<StateSyncScope, number>();

  observe(versions: StateVersionMap): void {
    for (const [scope, version] of Object.entries(versions)) {
      if (
        !isStateSyncScope(scope) ||
        !Number.isSafeInteger(version) ||
        version < 1
      ) {
        continue;
      }
      this.latestVersions.set(
        scope,
        Math.max(this.latestVersions.get(scope) ?? 0, version),
      );
    }
  }

  isCurrent(scope: StateSyncScope, version: number | undefined): boolean {
    if (version === undefined) return true;
    return version >= (this.latestVersions.get(scope) ?? 0);
  }

  latest(scope: StateSyncScope): number {
    return this.latestVersions.get(scope) ?? 0;
  }

  resetScope(scope: StateSyncScope): void {
    this.latestVersions.delete(scope);
  }

  reset(): void {
    this.latestVersions.clear();
  }
}
