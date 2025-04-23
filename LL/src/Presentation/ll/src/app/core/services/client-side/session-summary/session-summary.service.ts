import { Injectable, signal } from '@angular/core';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';

@Injectable({
  providedIn: 'root',
})
export class SessionSummaryService {
  readonly session = signal<CombatSessionDto | null | undefined>(null);

  /** Call immediately after a successful login */
  loadSince(session: CombatSessionDto | undefined) {
    if (!session?.combatSummary) return;
    if (session.combatSummary.totalBattles <= 1) return;
    this.session.set(session);
  }

  /** Mark as shown so the popup disappears */
  dismiss() {
    this.session.set(undefined);
  }
}
