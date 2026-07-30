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
  private requestedPresentationKey = '';
  private suppressedPresentationKey = '';
  private lastObservedPresentation: {
    key: string;
    route: string;
  } | null = null;

  private readonly activePresentation = computed(() => {
    const tutorial = this.tutorialState.state();
    if (!tutorial || !this.tutorialState.presentationReady()) return null;

    return {
      stepKey: tutorial.currentStep,
      route:
        tutorial.presentation?.destinationRoute ?? tutorial.destinationRoute,
      tourPageId: tutorial.presentation?.tourPageId ?? tutorial.tourPageId,
    };
  });

  constructor() {
    this.router.events
      .pipe(
        filter(
          (event): event is NavigationEnd => event instanceof NavigationEnd,
        ),
      )
      .subscribe((event) => {
        this.suppressedPresentationKey = '';
        this.currentUrl.set(event.urlAfterRedirects);
      });

    effect(() => {
      const presentation = this.activePresentation();
      const url = this.currentUrl();
      const tourPageId = presentation?.tourPageId;
      if (!presentation || !tourPageId) {
        this.lastObservedPresentation = null;
        return;
      }

      const key = this.presentationKey(presentation);
      const previousPresentation = this.lastObservedPresentation;
      if (
        previousPresentation &&
        previousPresentation.key !== key &&
        this.isCurrentRoute(previousPresentation.route, url) &&
        this.isCurrentRoute(presentation.route, url)
      ) {
        this.suppressedPresentationKey = key;
      }
      this.lastObservedPresentation = {
        key,
        route: presentation.route,
      };

      if (!this.isCurrentRoute(presentation.route, url)) {
        if (this.tour.state()?.pageId === tourPageId) {
          this.tour.stop(false);
        }
        this.lastStartedKey = '';
        return;
      }

      if (
        this.suppressedPresentationKey === key &&
        this.lastStartedKey !== key &&
        this.requestedPresentationKey !== key
      ) {
        this.tour.stop(false);
        this.lastStartedKey = '';
        return;
      }

      if (this.lastStartedKey === key) {
        return;
      }

      this.startPresentation(key, tourPageId);
    });
  }

  initialize(): void {
    // Injecting the service is enough to activate the effects.
  }

  presentCurrentStep(): void {
    const presentation = this.activePresentation();
    const tourPageId = presentation?.tourPageId;
    if (!presentation || !tourPageId) return;

    const key = this.presentationKey(presentation);
    this.requestedPresentationKey = key;
    this.suppressedPresentationKey = '';

    const url = this.currentUrl();
    if (!this.isCurrentRoute(presentation.route, url)) return;
    if (this.lastStartedKey === key) return;

    this.startPresentation(key, tourPageId);
  }

  private startPresentation(key: string, tourPageId: string): void {
    this.lastStartedKey = key;
    this.requestedPresentationKey = '';
    this.suppressedPresentationKey = '';
    setTimeout(() => void this.tour.start(tourPageId), 0);
  }

  private presentationKey(presentation: {
    stepKey: string;
    route: string;
    tourPageId: string | null | undefined;
  }): string {
    return `${presentation.stepKey}:${presentation.tourPageId}:${this.normalizeRoute(presentation.route)}`;
  }

  private isCurrentRoute(expectedRoute: string, actualRoute: string): boolean {
    const expectedPath = this.normalizeRoute(expectedRoute).split('?')[0];
    const actualPath = this.normalizeRoute(actualRoute).split('?')[0];
    return actualPath === expectedPath;
  }

  private normalizeRoute(route: string): string {
    return route.startsWith('/') ? route : `/${route}`;
  }
}
