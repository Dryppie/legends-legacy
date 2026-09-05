import { of } from 'rxjs';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';
import { QuestJournal } from '../../../../shared/models/quest';
import { QuestService } from './quest.service';

describe('QuestService', () => {
  it('turns in a quest and leaves reward scopes available for synchronization', () => {
    const journal: QuestJournal = { quests: [] };
    const api = {
      postVersioned: jasmine.createSpy('postVersioned').and.returnValue(
        of({
          data: { isSuccess: true, data: journal },
          domainVersions: { quests: 8, inventory: 9, character: 10 },
        }),
      ),
    };
    new QuestService(api as never).turnIn('quest/one').subscribe((result) => {
      expect(result.data).toBe(journal);
      expect(result.domainVersions['inventory']).toBe(9);
    });
    expect(api.postVersioned).toHaveBeenCalledWith(
      'Quest/quest%2Fone/turn-in',
      {},
      { stateSyncScopesHandledByResponse: ['quests'] },
    );
  });

  it('returns the quest generation and marks a complete choice response as owned', () => {
    const journal: QuestJournal = { quests: [], pinnedQuestId: null };
    const api = {
      postVersioned: jasmine.createSpy('postVersioned').and.returnValue(
        of({
          data: { isSuccess: true, data: journal },
          domainVersions: { quests: 7 },
        }),
      ),
    };
    const service = new QuestService(api as never);

    service.selectChoice('quest/one', 'option-a').subscribe((result) => {
      expect(result.data).toBe(journal);
      expect(result.domainVersions['quests']).toBe(7);
    });

    expect(api.postVersioned).toHaveBeenCalledWith(
      'Quest/quest%2Fone/choice',
      { optionKey: 'option-a' },
      { stateSyncScopesHandledByResponse: ['quests'] },
    );
  });

  it('returns encounter loot as a versioned inventory delta', () => {
    const combatResult = { loot: [] } as unknown as CombatResultDto;
    const api = {
      postVersioned: jasmine.createSpy('postVersioned').and.returnValue(
        of({
          data: { isSuccess: true, data: combatResult },
          domainVersions: { inventory: 9 },
        }),
      ),
    };
    const service = new QuestService(api as never);

    service.startEncounter('training/day', 'skeleton').subscribe((result) => {
      expect(result.data).toBe(combatResult);
      expect(result.domainVersions['inventory']).toBe(9);
    });

    expect(api.postVersioned).toHaveBeenCalledWith(
      'Quest/training%2Fday/encounters/skeleton/start',
      {},
      { stateSyncScopesHandledByResponse: ['inventory'] },
    );
  });
});
