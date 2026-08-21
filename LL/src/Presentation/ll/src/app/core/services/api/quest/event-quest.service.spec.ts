import { of } from 'rxjs';
import { EventQuestJournal } from '../../../../shared/models/event-quest';
import { EventQuestService } from './event-quest.service';

describe('EventQuestService', () => {
  it('returns the event-quest generation and leaves inventory reconciliation enabled', () => {
    const journal: EventQuestJournal = { events: [] };
    const api = {
      postVersioned: jasmine.createSpy('postVersioned').and.returnValue(
        of({
          data: { isSuccess: true, data: journal },
          domainVersions: { 'event-quests': 4, inventory: 9 },
        }),
      ),
    };
    const service = new EventQuestService(api as never);

    service.claim('event/one').subscribe((result) => {
      expect(result.data).toBe(journal);
      expect(result.domainVersions['event-quests']).toBe(4);
    });

    expect(api.postVersioned).toHaveBeenCalledWith(
      'EventQuest/event%2Fone/claim',
      {},
      { stateSyncScopesHandledByResponse: ['event-quests'] },
    );
  });
});
