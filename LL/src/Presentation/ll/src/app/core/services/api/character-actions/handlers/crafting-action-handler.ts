import { Injectable } from '@angular/core';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { ProfessionType } from '../../../../../shared/models/Dtos/characterProfession';
import { LevelingService } from '../../../client-side/leveling/leveling.service';
import { SessionSummaryService } from '../../../client-side/session-summary/session-summary.service';
import { CraftingService } from '../../crafting/crafting.service';

@Injectable({ providedIn: 'root' })
export class CraftingActionHandler {
  constructor(
    private readonly craftingService: CraftingService,
    private readonly sessionSummaryService: SessionSummaryService,
    private readonly levelingService: LevelingService,
  ) {}

  handle(action: CharacterActionDto): void {
    this.craftingService.setQueue(action.temperingQueueItems ?? []);

    const tempering = action.temperingSession;
    if (!tempering) return;

    this.craftingService.recordTemperingOutcomes(tempering.outcomes ?? []);
    this.sessionSummaryService.loadCraftingSince(tempering);
    const summary = tempering.temperingSummary;

    if (summary.totalExperience > 0) {
      this.levelingService.gainProfessionExperience(
        ProfessionType.Crafting,
        summary.totalExperience,
      );
    }

    // The versioned character invalidation refreshes the authoritative balance.
    // Applying this session total locally would race that snapshot.
  }

  clear(): void {
    this.craftingService.setQueue([]);
  }
}
