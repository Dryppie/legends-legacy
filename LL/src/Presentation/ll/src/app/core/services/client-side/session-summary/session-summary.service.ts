import { Injectable, signal } from '@angular/core';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';

@Injectable({
  providedIn: 'root',
})
export class SessionSummaryService {
  readonly session = signal<CombatSessionDto | null | undefined>(null);

  // When fetching character action data, show a display info for how much has happened while offline
  loadSince(session: CombatSessionDto | undefined) {
    if (!session?.combatSummary) return;
    if (session.combatSummary.totalBattles <= 1) return;
    this.session.set(session);
  }

  dismiss() {
    this.session.set(undefined);
  }
}
