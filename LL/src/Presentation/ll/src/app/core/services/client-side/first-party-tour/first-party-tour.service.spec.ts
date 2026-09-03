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
    const actionWatcher = new FirstPartyTourActionWatcherService(
      router as unknown as Router,
    );

    service = new FirstPartyTourService(
      storage,
      characterState,
      router as unknown as Router,
      actionWatcher,
    );
  });

  afterEach(() => service.stop(false));

  it('runs the authored Lumo combat tour through its card and Battle action', async () => {
    const response = await fetch('/assets/help/tours/tutorial-lumo-ruins.json');
    expect(response.ok).toBeTrue();
    const steps = await response.json();
    expect(steps.map((step: { element: string }) => step.element)).toEqual([
      '[data-tour=lumo-ruins-card]',
      '[data-tour=lumo-ruins-battle]',
    ]);
    spyOn(window, 'fetch').and.resolveTo({
      ok: true,
      json: async () => steps,
    } as Response);
    const card = document.createElement('div');
    card.dataset['tour'] = 'lumo-ruins-card';
    card.textContent = 'Lumo Ruins';
    const battle = document.createElement('button');
    battle.dataset['tour'] = 'lumo-ruins-battle';
    battle.textContent = 'Battle';
    card.appendChild(battle);
    document.body.appendChild(card);

    try {
      await service.start('tutorial-lumo-ruins');
      expect(service.state()?.stepIndex).toBe(0);
      expect(service.state()?.targetRect).not.toBeNull();
      service.next();
      const deadline = performance.now() + 1500;
      while (service.state()?.stepIndex !== 1 && performance.now() < deadline) {
        await new Promise((resolve) => requestAnimationFrame(resolve));
      }
      expect(service.state()?.stepIndex).toBe(1);
      expect(service.state()?.targetRect).not.toBeNull();
      battle.click();
      await new Promise((resolve) => setTimeout(resolve, 0));
      expect(service.state()).toBeNull();
    } finally {
      card.remove();
    }
  });

  it('advances the authored essence tour when any archived essence is selected', async () => {
    const response = await fetch(
      '/assets/help/tours/tutorial-essence-loadout.json',
    );
    expect(response.ok).toBeTrue();
    const steps = await response.json();
    spyOn(window, 'fetch').and.resolveTo({
      ok: true,
      json: async () => steps,
    } as Response);
    router.url = '/game/character/essences';
    const archive = document.createElement('div');
    archive.dataset['tour'] = 'essence-archive';
    const wrongEssence = document.createElement('button');
    wrongEssence.dataset['tour'] = 'essence-archive-list';
    wrongEssence.textContent = 'Wood Nymph';
    const equip = document.createElement('button');
    equip.dataset['tour'] = 'tutorial-equip-essence';
    equip.textContent = 'Equip Essence';
    wrongEssence.addEventListener('click', () => archive.appendChild(equip));
    archive.append(wrongEssence);
    document.body.appendChild(archive);

    try {
      await service.start('tutorial-essence-loadout');
      wrongEssence.click();
      const deadline = performance.now() + 1500;
      while (service.state()?.stepIndex !== 1 && performance.now() < deadline) {
        await new Promise((resolve) => requestAnimationFrame(resolve));
      }
      expect(service.state()?.stepIndex).toBe(1);
      expect(service.state()?.targetRect).not.toBeNull();
      expect(service.state()?.blocksInteraction).toBeFalse();
      expect(service.state()?.step.showBack).toBeTrue();

      equip.click();
      await new Promise((resolve) => setTimeout(resolve, 0));
      expect(service.state()).toBeNull();
    } finally {
      archive.remove();
    }
  });

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

  it('keeps an active tour while navigating to a child detail route', async () => {
    spyOn(window, 'fetch').and.resolveTo({
      ok: true,
      json: async () => [
        {
          kind: 'click',
          element: '[data-tour=route-target]',
          description: 'Continue into the detail view.',
          targetTimeoutMs: 0,
        },
      ],
    } as Response);

    await service.start('tutorial-child-route');
    router.url = '/game/world/shenic/essence-id';
    routerEvents.next(
      new NavigationEnd(
        1,
        '/game/world/shenic/essence-id',
        '/game/world/shenic/essence-id',
      ),
    );

    expect(service.state()?.pageId).toBe('tutorial-child-route');
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

  it('targets the visible responsive element when duplicate selectors exist', async () => {
    spyOn(window, 'fetch').and.resolveTo({
      ok: true,
      json: async () => [
        {
          element: '[data-tour=responsive-target]',
          description: 'Use the visible responsive target.',
          targetTimeoutMs: 0,
        },
      ],
    } as Response);

    const hiddenTarget = document.createElement('div');
    hiddenTarget.dataset['tour'] = 'responsive-target';
    hiddenTarget.style.display = 'none';
    document.body.appendChild(hiddenTarget);

    const visibleTarget = document.createElement('button');
    visibleTarget.dataset['tour'] = 'responsive-target';
    visibleTarget.scrollIntoView = jasmine.createSpy('scrollIntoView');
    spyOn(visibleTarget, 'getBoundingClientRect').and.returnValue({
      top: 80,
      right: 320,
      bottom: 140,
      left: 120,
      width: 200,
      height: 60,
      x: 120,
      y: 80,
      toJSON: () => undefined,
    });
    document.body.appendChild(visibleTarget);

    await service.start('tutorial-responsive-target');

    expect(service.state()?.targetRect?.left).toBe(120);
    expect(service.state()?.targetRect?.width).toBe(200);
    expect(visibleTarget.scrollIntoView).toHaveBeenCalled();

    hiddenTarget.remove();
    visibleTarget.remove();
  });
});
