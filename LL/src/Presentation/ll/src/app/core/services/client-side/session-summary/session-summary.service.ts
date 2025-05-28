import { Injectable, signal } from '@angular/core';
import { CombatSessionDto } from '../../../../shared/models/Dtos/combatResultDto';
import { TemperingSessionDto } from '../../../../shared/models/Dtos/temperingSessionDto';
import { GatheringSessionDto } from '../../../../shared/models/Dtos/gatheringSessionDto';

@Injectable({
  providedIn: 'root',
})
export class SessionSummaryService {
  readonly combatSession = signal<CombatSessionDto | null | undefined>(null);
  readonly temperingSession = signal<TemperingSessionDto | null | undefined>(
    null,
  );
  readonly gatheringSession = signal<GatheringSessionDto | null | undefined>(
    null,
  );

  loadCombatSince(session: CombatSessionDto | undefined) {
    if (!session?.combatSummary) return;
    if (session.combatSummary.totalBattles <= 2) return;
    this.combatSession.set(session);
  }

  loadCraftingSince(session: TemperingSessionDto) {
    if (!session?.temperingSummary) return;
    if (session.temperingSummary.totalActions <= 6) return;
    this.temperingSession.set(session);
  }

  loadGatheringSince(session: GatheringSessionDto) {
    if (!session?.gatheringSummary) return;
    if (session.gatheringSummary.totalActions <= 6) return;
    this.gatheringSession.set(session);
  }

  dismiss() {
    this.combatSession.set(undefined);
    this.temperingSession.set(undefined);
    this.gatheringSession.set(undefined);
  }
}
