import { Injectable, signal } from '@angular/core';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';
import { TemperingSessionDto } from '../../../../shared/models/Dtos/temperingSessionDto';

@Injectable({
  providedIn: 'root',
})
export class SessionSummaryService {
  readonly combatSession = signal<CombatSessionDto | null | undefined>(null);
  readonly temperingSession = signal<TemperingSessionDto | null | undefined>(
    null,
  );

  loadCombatSince(session: CombatSessionDto | undefined) {
    if (!session?.combatSummary) return;
    if (session.combatSummary.totalBattles <= 1) return;
    this.combatSession.set(session);
  }

  loadCraftingSince(session: TemperingSessionDto) {
    if (!session?.temperingSummary) return;
    if (session.temperingSummary.totalActions <= 1) return;
    this.temperingSession.set(session);
  }

  dismiss() {
    this.combatSession.set(undefined);
    this.temperingSession.set(undefined);
  }
}
