import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { FirstPartyTourService } from '../../client-side/first-party-tour/first-party-tour.service';
import { TutorialStateService } from './tutorial-state.service';

@Injectable({ providedIn: 'root' })
export class TutorialPresenterService {
  private readonly tutorialState = inject(TutorialStateService);
  private readonly tour = inject(FirstPartyTourService);
  private readonly router = inject(Router);

  private readonly currentUrl = signal(this.router.url);
  private lastStartedKey = '';

  private readonly activePresentation = computed(() => {
    const tutorial = this.tutorialState.state();
    if (!tutorial) return null;

    return {
      stepKey: tutorial.currentStep,
      route: tutorial.presentation?.destinationRoute ?? tutorial.destinationRoute,
      tourPageId: tutorial.presentation?.tourPageId ?? tutorial.tourPageId,
    };
  });

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.currentUrl.set(event.urlAfterRedirects));

    effect(() => {
      const presentation = this.activePresentation();
      const url = this.currentUrl();
      const tourPageId = presentation?.tourPageId;
      if (!tourPageId || !this.isCurrentRoute(presentation.route, url)) {
        return;
      }

      const key = `${presentation.stepKey}:${tourPageId}:${this.normalizeRoute(url)}`;
      if (this.lastStartedKey === key) {
        return;
      }

      this.lastStartedKey = key;
      setTimeout(() => void this.tour.start(tourPageId), 0);
    });
  }

  initialize(): void {
    // Injecting the service is enough to activate the effects.
  }

  private isCurrentRoute(expectedRoute: string, actualRoute: string): boolean {
    const expected = this.normalizeRoute(expectedRoute);
    const actual = this.normalizeRoute(actualRoute);

    if (expected.includes('?')) {
      return actual === expected;
    }

    return actual === expected || actual.startsWith(`${expected}?`);
  }

  private normalizeRoute(route: string): string {
    return route.startsWith('/') ? route : `/${route}`;
  }
}
