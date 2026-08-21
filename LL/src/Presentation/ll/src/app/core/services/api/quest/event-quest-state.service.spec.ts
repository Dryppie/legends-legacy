import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { EventQuestStateService } from './event-quest-state.service';

describe('EventQuestStateService', () => {
  it('defers realtime refreshes while no event-quest view is active', () => {
    TestBed.configureTestingModule({});
    const changed = signal<object | null>(null);
    const api = {
      getJournal: jasmine
        .createSpy('getJournal')
        .and.returnValue(of({ events: [] })),
    };
    const events = {
      event: { EventQuestChanged: changed },
    };
    const eventBus = { logout: signal(0) };
    const stateSync = { register: jasmine.createSpy('register') };
    const domainVersions = {
      isCurrent: jasmine.createSpy('isCurrent').and.returnValue(true),
    };
    const state = TestBed.runInInjectionContext(
      () =>
        new EventQuestStateService(
          api as never,
          events as never,
          eventBus as never,
          stateSync as never,
          domainVersions as never,
        ),
    );
    TestBed.flushEffects();
    state.load();
    expect(api.getJournal).toHaveBeenCalledTimes(1);

    changed.set({ eventQuestId: 'event.active' });
    TestBed.flushEffects();

    expect(api.getJournal).toHaveBeenCalledTimes(1);

    state.activateView();

    expect(api.getJournal).toHaveBeenCalledTimes(2);

    changed.set({ eventQuestId: 'event.active', revision: 2 });
    TestBed.flushEffects();

    expect(api.getJournal).toHaveBeenCalledTimes(3);
  });

  it('does not let an older claim response replace a newer event journal', () => {
    TestBed.configureTestingModule({});
    const currentJournal = { events: [{ eventQuestId: 'current' }] };
    const staleJournal = { events: [{ eventQuestId: 'stale' }] };
    const api = {
      getJournal: jasmine
        .createSpy('getJournal')
        .and.returnValue(of(currentJournal)),
      claim: jasmine.createSpy('claim').and.returnValue(
        of({
          data: staleJournal,
          domainVersions: { 'event-quests': 1 },
        }),
      ),
    };
    const state = TestBed.runInInjectionContext(
      () =>
        new EventQuestStateService(
          api as never,
          { event: { EventQuestChanged: signal(null) } } as never,
          { logout: signal(0) } as never,
          { register: jasmine.createSpy('register') } as never,
          { isCurrent: () => false } as never,
        ),
    );

    state.load();
    state.claim('event.active');

    expect(state.journal()).toBe(currentJournal as never);
  });
});
