import { Injectable, signal } from '@angular/core';
import { APP_VERSION } from '../../../app-version';

type VersionResponse = {
  version?: string;
};

const isoTimestampVersionPattern = /^\d{4}-\d{2}-\d{2}T/;

export function isNewerAppVersion(
  availableVersion: string,
  currentVersion: string,
): boolean {
  const available = availableVersion.trim();
  const current = currentVersion.trim();
  if (!available || available === current) return false;

  if (
    isoTimestampVersionPattern.test(available) &&
    isoTimestampVersionPattern.test(current)
  ) {
    const availableTimestamp = Date.parse(available);
    const currentTimestamp = Date.parse(current);
    if (
      Number.isFinite(availableTimestamp) &&
      Number.isFinite(currentTimestamp)
    ) {
      return availableTimestamp > currentTimestamp;
    }
  }

  // Non-timestamp build identifiers cannot be ordered reliably. Preserve the
  // existing behavior for hashes, semantic versions, and custom build labels.
  return true;
}

@Injectable({
  providedIn: 'root',
})
export class AppUpdateService {
  private readonly pollIntervalMs = 60_000;
  private readonly currentVersion = APP_VERSION;
  private started = false;

  private readonly _updateAvailable = signal(false);
  readonly updateAvailable = this._updateAvailable.asReadonly();

  start(): void {
    if (this.started) return;

    this.started = true;
    this.checkForUpdate();
    window.setInterval(
      () => this.checkForUpdate(),
      this.pollIntervalMs,
    );
    document.addEventListener('visibilitychange', this.onVisibilityChange);
  }

  refresh(): void {
    window.location.reload();
  }

  private readonly onVisibilityChange = (): void => {
    if (!document.hidden) {
      this.checkForUpdate();
    }
  };

  private async checkForUpdate(): Promise<void> {
    if (document.hidden || this._updateAvailable()) return;

    try {
      const response = await fetch(`/assets/version.json?v=${Date.now()}`, {
        cache: 'no-store',
      });

      if (!response.ok) return;

      const result = (await response.json()) as VersionResponse;
      if (!result.version) return;

      if (isNewerAppVersion(result.version, this.currentVersion)) {
        this._updateAvailable.set(true);
      }
    } catch {
      // A failed version check should never interrupt the game.
    }
  }
}
