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
      event: { EventQuestChangedMsg: changed },
    };
    const eventBus = { logout: signal(0) };
    const stateSync = { register: jasmine.createSpy('register') };
    const state = TestBed.runInInjectionContext(
      () =>
        new EventQuestStateService(
          api as never,
          events as never,
          eventBus as never,
          stateSync as never,
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
});
