import { of } from 'rxjs';
import { QuestJournal } from '../../../../shared/models/quest';
import { QuestService } from './quest.service';

describe('QuestService', () => {
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
});
