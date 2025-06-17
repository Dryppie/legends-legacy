import { Injectable } from '@angular/core';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { ProfessionType } from '../../../../../shared/models/Dtos/characterProfession';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { LevelingService } from '../../../client-side/leveling/leveling.service';
import { SessionSummaryService } from '../../../client-side/session-summary/session-summary.service';
import { CraftingService } from '../../crafting/crafting.service';
import { CurrencyService } from '../../currency/currency.service';

@Injectable({ providedIn: 'root' })
export class CraftingActionHandler {
  constructor(
    private readonly craftingService: CraftingService,
    private readonly sessionSummaryService: SessionSummaryService,
    private readonly levelingService: LevelingService,
    private readonly currencyService: CurrencyService,
  ) {}

  handle(action: CharacterActionDto): void {
    if (action.characterActionType !== CharacterActionType.Crafting) return;

    const queue = action.craftingActionDetails?.craftingQueueItems ?? [];
    this.craftingService.setQueue(queue);

    const tempering = action.temperingSession;
    if (!tempering) return;

    this.sessionSummaryService.loadCraftingSince(tempering);
    const summary = tempering.temperingSummary;

    if (summary.armorForgingExperience > 0) {
      this.levelingService.gainProfessionExperience(
        ProfessionType.ArmorForging,
        summary.armorForgingExperience,
      );
    }

    if (summary.jewelryCraftingExperience > 0) {
      this.levelingService.gainProfessionExperience(
        ProfessionType.JewelryCrafting,
        summary.jewelryCraftingExperience,
      );
    }

    if (summary.weaponSmithingExperience > 0) {
      this.levelingService.gainProfessionExperience(
        ProfessionType.WeaponSmithing,
        summary.weaponSmithingExperience,
      );
    }

    this.currencyService.gainSoulstones(summary.totalSoulstones);
  }
}
