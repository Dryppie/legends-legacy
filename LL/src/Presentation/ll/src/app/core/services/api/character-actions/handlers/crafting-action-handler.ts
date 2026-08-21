import { Injectable } from '@angular/core';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { ProfessionType } from '../../../../../shared/models/Dtos/characterProfession';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
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

    if (action.characterActionType !== CharacterActionType.Crafting) return;

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

    // Crafting Soulstones are applied from the authoritative SoulstoneDrop event.
    // Adding the session total here as well makes the displayed balance depend
    // on whether the action response or the real-time event arrives first.
  }

  clear(): void {
    this.craftingService.setQueue([]);
  }
}
