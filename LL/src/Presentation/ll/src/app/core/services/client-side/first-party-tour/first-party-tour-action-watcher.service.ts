import { Injectable } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import {
  FirstPartyTourStatePredicate,
  FirstPartyTourStep,
} from './first-party-tour.models';

export interface FirstPartyTourActionWatchContext {
  advance: () => void;
  statePredicates: ReadonlyMap<string, FirstPartyTourStatePredicate>;
}

@Injectable({ providedIn: 'root' })
export class FirstPartyTourActionWatcherService {
  constructor(private readonly router: Router) {}

  watch(
    step: FirstPartyTourStep,
    context: FirstPartyTourActionWatchContext,
  ): () => void {
    const cleanups: Array<() => void> = [];

    if (step.kind === 'click') {
      cleanups.push(this.watchPointerAction(step, context.advance));
    }

    if (step.kind === 'navigate') {
      const expectedRoute = step.advanceOn?.route ?? step.route;
      const actionSelector = step.advanceOn?.selector ?? step.actionSelector;

      if (expectedRoute) {
        cleanups.push(this.watchRoute(expectedRoute, context.advance));
        if (this.routeMatches(expectedRoute)) {
          queueMicrotask(context.advance);
        }
      } else if (actionSelector) {
        cleanups.push(this.watchPointerAction(step, context.advance));
      }
    }

    if (step.kind === 'waitForState') {
      const stateKey = step.advanceOn?.stateKey ?? step.stateKey;
      if (stateKey) {
        cleanups.push(
          this.watchStatePredicate(
            stateKey,
            context.statePredicates,
            context.advance,
          ),
        );
      }
    }

    return () => cleanups.forEach((cleanup) => cleanup());
  }

  private watchPointerAction(
    step: FirstPartyTourStep,
    advance: () => void,
  ): () => void {
    const selector = step.advanceOn?.selector ?? step.actionSelector ?? step.element;
    const eventName = step.advanceOn?.event ?? 'click';
    let hasAdvanced = false;

    const listener = (event: MouseEvent | PointerEvent) => {
      if (hasAdvanced || !(event.target instanceof Element)) {
        return;
      }

      if (!event.target.closest(selector)) {
        return;
      }

      hasAdvanced = true;
      setTimeout(advance, 0);
    };

    document.addEventListener(eventName, listener, true);
    return () => document.removeEventListener(eventName, listener, true);
  }

  private watchRoute(expectedRoute: string, advance: () => void): () => void {
    let hasAdvanced = false;
    let subscription: Subscription | null = this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => {
        if (hasAdvanced || !this.routeMatches(expectedRoute)) {
          return;
        }

        hasAdvanced = true;
        setTimeout(advance, 0);
      });

    return () => {
      subscription?.unsubscribe();
      subscription = null;
    };
  }

  private watchStatePredicate(
    stateKey: string,
    predicates: ReadonlyMap<string, FirstPartyTourStatePredicate>,
    advance: () => void,
  ): () => void {
    let hasAdvanced = false;

    const intervalId = window.setInterval(() => {
      const predicate = predicates.get(stateKey);
      if (!predicate || hasAdvanced || !predicate()) {
        return;
      }

      hasAdvanced = true;
      window.clearInterval(intervalId);
      setTimeout(advance, 0);
    }, 150);

    return () => window.clearInterval(intervalId);
  }

  private routeMatches(expectedRoute: string): boolean {
    const current = this.normalizeRoute(this.router.url);
    const expected = this.normalizeRoute(expectedRoute);

    return current === expected || current.startsWith(`${expected}?`);
  }

  private normalizeRoute(route: string): string {
    return route.startsWith('/') ? route : `/${route}`;
  }
}
