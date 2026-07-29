import { NavigationEnd, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { CharacterStateService } from '../../api/character/character-state.service';
import { LocalStorageService } from '../local-storage/local-storage.service';
import { FirstPartyTourActionWatcherService } from './first-party-tour-action-watcher.service';
import { FirstPartyTourService } from './first-party-tour.service';

describe('FirstPartyTourService', () => {
  let service: FirstPartyTourService;
  let routerEvents: Subject<unknown>;
  let router: { events: Subject<unknown>; url: string };

  beforeEach(() => {
    routerEvents = new Subject();
    router = {
      events: routerEvents,
      url: '/game/world/shenic',
    };
    const storage = {
      get: () => null,
      set: jasmine.createSpy('set'),
    } as unknown as LocalStorageService;
    const characterState = {
      currentCharacter: () => null,
    } as unknown as CharacterStateService;
    const actionWatcher = {
      watch: () => () => undefined,
    } as unknown as FirstPartyTourActionWatcherService;

    service = new FirstPartyTourService(
      storage,
      characterState,
      router as unknown as Router,
      actionWatcher,
    );
  });

  afterEach(() => service.stop(false));

  it('does not restore a pending step after the tour is stopped', async () => {
    spyOn(window, 'fetch').and.resolveTo({
      ok: true,
      json: async () => [
        {
          element: '[data-tour=late-target]',
          description: 'Wait for a target.',
          targetTimeoutMs: 500,
        },
      ],
    } as Response);

    const startPromise = service.start('tutorial-race');
    await new Promise((resolve) => window.setTimeout(resolve, 0));

    service.stop(true);

    const target = document.createElement('button');
    target.dataset['tour'] = 'late-target';
    document.body.appendChild(target);

    await startPromise;

    expect(service.state()).toBeNull();
    target.remove();
  });

  it('stops an active tour after navigating to another page', async () => {
    spyOn(window, 'fetch').and.resolveTo({
      ok: true,
      json: async () => [
        {
          element: '[data-tour=route-target]',
          description: 'Stay on this page.',
          targetTimeoutMs: 0,
        },
      ],
    } as Response);

    await service.start('tutorial-route');
    expect(service.state()?.pageId).toBe('tutorial-route');

    router.url = '/login';
    routerEvents.next(new NavigationEnd(1, '/login', '/login'));

    expect(service.state()).toBeNull();
  });

  it('does not activate a tour whose load completes after navigation', async () => {
    let resolveFetch!: (response: Response) => void;
    spyOn(window, 'fetch').and.returnValue(
      new Promise<Response>((resolve) => {
        resolveFetch = resolve;
      }),
    );

    const startPromise = service.start('tutorial-loading');

    router.url = '/login';
    routerEvents.next(new NavigationEnd(1, '/login', '/login'));
    resolveFetch({
      ok: true,
      json: async () => [
        {
          element: '[data-tour=late-route-target]',
          description: 'This should never appear.',
        },
      ],
    } as Response);

    await startPromise;

    expect(service.state()).toBeNull();
  });
});
