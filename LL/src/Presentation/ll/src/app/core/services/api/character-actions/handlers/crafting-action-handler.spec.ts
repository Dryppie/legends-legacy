import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { ProfessionType } from '../../../../../shared/models/Dtos/characterProfession';
import { LevelingService } from '../../../client-side/leveling/leveling.service';
import { SessionSummaryService } from '../../../client-side/session-summary/session-summary.service';
import { CraftingService } from '../../crafting/crafting.service';
import { CraftingActionHandler } from './crafting-action-handler';

describe('CraftingActionHandler', () => {
  it('processes the final Tempering session when the resulting action is Combat', () => {
    const crafting = jasmine.createSpyObj<CraftingService>('CraftingService', [
      'setQueue',
      'recordTemperingOutcomes',
    ]);
    const summaries = jasmine.createSpyObj<SessionSummaryService>(
      'SessionSummaryService',
      ['loadCraftingSince'],
    );
    const leveling = jasmine.createSpyObj<LevelingService>('LevelingService', [
      'gainProfessionExperience',
    ]);
    const handler = new CraftingActionHandler(crafting, summaries, leveling);
    const temperingSession = {
      from: new Date('2026-08-24T12:00:00Z'),
      to: new Date('2026-08-24T12:00:10Z'),
      outcomes: [],
      temperingSummary: {
        totalItemsCrafted: 1,
        masterpieces: 0,
        levelingItems: 0,
        cursedOutcomes: 0,
        qualityIncreases: 0,
        totalActions: 1,
        totalSoulstones: 0,
        craftingExperience: 25,
        totalExperience: 25,
      },
    };
    const action: CharacterActionDto = {
      characterActionType: CharacterActionType.Combat,
      lootTableId: 'first-area',
      updatedAt: new Date('2026-08-24T12:00:10Z'),
      revision: 'auto-resumed-combat',
      isDeleted: false,
      autoResumedFromTempering: true,
      temperingSession,
      temperingQueueItems: [],
    };

    handler.handle(action);

    expect(crafting.setQueue).toHaveBeenCalledOnceWith([]);
    expect(crafting.recordTemperingOutcomes).toHaveBeenCalledOnceWith([]);
    expect(summaries.loadCraftingSince).toHaveBeenCalledOnceWith(
      temperingSession,
    );
    expect(leveling.gainProfessionExperience).toHaveBeenCalledOnceWith(
      ProfessionType.Crafting,
      25,
    );
  });
});
