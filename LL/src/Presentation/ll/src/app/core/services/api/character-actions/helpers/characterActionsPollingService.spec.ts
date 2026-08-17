import { fakeAsync, tick } from '@angular/core/testing';
import { Observable, Subject, of } from 'rxjs';
import { CharacterActionDto } from '../../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';
import { TimeSyncService } from '../../time-sync/time-sync.service';
import { CharacterActionsPollingService } from './characterActionsPollingService';

describe('CharacterActionsPollingService', () => {
  const now = Date.parse('2026-08-17T12:00:00Z');

  function createService(): CharacterActionsPollingService {
    return new CharacterActionsPollingService({
      now: () => now,
    } as TimeSyncService);
  }

  function action(
    overrides: Partial<CharacterActionDto> = {},
  ): CharacterActionDto {
    return {
      characterActionType: CharacterActionType.Crafting,
      lootTableId: '',
      updatedAt: new Date(now - 60_000),
      nextResolutionAtUtc: new Date(now + 2_500),
      resolutionIntervalMs: 37_000,
      hasMoreDueWork: false,
      revision: '1',
      isDeleted: false,
      ...overrides,
    };
  }

  it('schedules crafting from the server boundary rather than a client duration', fakeAsync(() => {
    const service = createService();
    const fetch = jasmine.createSpy('fetch').and.returnValue(of(null));

    service.start(fetch, () => undefined, action());
    tick(2_499);
    expect(fetch).not.toHaveBeenCalled();

    tick(1);
    expect(fetch).toHaveBeenCalledTimes(1);
    service.stop();
  }));

  it('continues promptly only when the backend reports more due work', fakeAsync(() => {
    const service = createService();
    const fetch = jasmine.createSpy('fetch').and.returnValue(of(null));

    service.start(fetch, () => undefined, action({ hasMoreDueWork: true }));
    tick(99);
    expect(fetch).not.toHaveBeenCalled();
    tick(1);
    expect(fetch).toHaveBeenCalledTimes(1);
    service.stop();
  }));

  it('never overlaps resolution requests', fakeAsync(() => {
    const service = createService();
    const pending = new Subject<CharacterActionDto | null>();
    const fetch = jasmine
      .createSpy<() => Observable<CharacterActionDto | null>>('fetch')
      .and.returnValue(pending);

    service.start(fetch, () => undefined, action({ hasMoreDueWork: true }));
    tick(100);
    expect(fetch).toHaveBeenCalledTimes(1);

    tick(30_000);
    expect(fetch).toHaveBeenCalledTimes(1);
    pending.next(null);
    pending.complete();
    service.stop();
  }));

  it('cancels the previous action timer when an action is replaced', fakeAsync(() => {
    const service = createService();
    const oldFetch = jasmine.createSpy('oldFetch').and.returnValue(of(null));
    const newFetch = jasmine.createSpy('newFetch').and.returnValue(of(null));

    service.start(oldFetch, () => undefined, action());
    service.start(
      newFetch,
      () => undefined,
      action({ nextResolutionAtUtc: new Date(now + 1_000), revision: '2' }),
    );

    tick(1_000);
    expect(newFetch).toHaveBeenCalledTimes(1);
    tick(2_000);
    expect(oldFetch).not.toHaveBeenCalled();
    service.stop();
  }));
});
