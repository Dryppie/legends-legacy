import { effect, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { AuthService } from './auth/auth.service';

@Injectable({ providedIn: 'root' })
export class ActivityDayService {
  private inFlight = false;

  constructor(private readonly auth: AuthService, private readonly api: ApiService) {
    effect(() => {
      this.auth.isAuthenticated();
      this.auth.currentCharacter();
      void this.recordIfVisible();
    });
    document.addEventListener('visibilitychange', () => void this.recordIfVisible());
    window.addEventListener('focus', () => void this.recordIfVisible());
  }

  private async recordIfVisible(): Promise<void> {
    const identity = this.auth.currentCharacter()?.id;
    if (!this.auth.isAuthenticated() || !identity || document.visibilityState !== 'visible' || this.inFlight) return;
    const today = new Date().toISOString().slice(0, 10);
    const key = `ll:activity:${identity}`;
    try {
      if (localStorage.getItem(key) === today) return;
    } catch {
      // Storage may be disabled; the server still deduplicates activity days.
    }
    this.inFlight = true;
    try {
      const response = await firstValueFrom(this.api.post('Activity'));
      if (response?.isSuccess) {
        try { localStorage.setItem(key, today); } catch { /* Server uniqueness remains authoritative. */ }
      }
    } catch {
      // A later focus or authentication change retries; activity never blocks gameplay.
    } finally {
      this.inFlight = false;
    }
  }
}
