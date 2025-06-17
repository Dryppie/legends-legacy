import { Injectable } from '@angular/core';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { LevelingService } from '../../../client-side/leveling/leveling.service';
import { SessionSummaryService } from '../../../client-side/session-summary/session-summary.service';
import { CurrencyService } from '../../currency/currency.service';

@Injectable({ providedIn: 'root' })
export class GatheringActionHandler {
  constructor(
    private readonly sessionSummaryService: SessionSummaryService,
    private readonly levelingService: LevelingService,
    private readonly currencyService: CurrencyService,
  ) {}

  handle(action: CharacterActionDto): void {
    if (
      action.characterActionType !== CharacterActionType.Gathering ||
      !action.gatheringSession
    )
      return;

    const session = action.gatheringSession;
    const summary = session.gatheringSummary;

    this.sessionSummaryService.loadGatheringSince(session);
    this.levelingService.gainProfessionExperience(
      summary.professionType,
      summary.totalExperience,
    );

    this.currencyService.gainSoulstones(summary.totalSoulstones);
  }
}
