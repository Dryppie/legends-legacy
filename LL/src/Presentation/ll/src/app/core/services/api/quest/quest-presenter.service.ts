import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { FirstPartyTourService } from '../../client-side/first-party-tour/first-party-tour.service';
import { QuestStateService } from './quest-state.service';

@Injectable({ providedIn: 'root' })
export class QuestPresenterService {
  private readonly quests = inject(QuestStateService);
  private readonly tour = inject(FirstPartyTourService);
  private readonly router = inject(Router);
  private readonly currentUrl = signal(this.router.url);
  private lastStartedKey = '';

  private readonly activePresentation = computed(() => {
    const quest = this.quests.pinnedQuest();
    const objective = this.quests.pinnedObjective();
    if (
      !quest ||
      !objective ||
      (quest.choice && !quest.choice.selectedOptionKey)
    ) {
      return null;
    }
    return {
      key: `${quest.questId}:${quest.version}:${objective.key}`,
      route: objective.presentation.destinationRoute,
      tourPageId: objective.presentation.tourPageId,
    };
  });

  constructor() {
    this.router.events
      .pipe(
        filter(
          (event): event is NavigationEnd => event instanceof NavigationEnd,
        ),
      )
      .subscribe((event) => this.currentUrl.set(event.urlAfterRedirects));

    effect(() => {
      const presentation = this.activePresentation();
      const url = this.currentUrl();
      if (
        !presentation?.tourPageId ||
        !this.isCurrentRoute(presentation.route, url)
      ) {
        this.lastStartedKey = '';
        return;
      }

      this.start(presentation.key, presentation.tourPageId);
    });
  }

  initialize(): void {}

  presentCurrentObjective(): void {
    const presentation = this.activePresentation();
    if (
      !presentation?.tourPageId ||
      !this.isCurrentRoute(presentation.route, this.currentUrl())
    ) {
      return;
    }

    this.lastStartedKey = '';
    this.start(presentation.key, presentation.tourPageId);
  }

  private start(key: string, tourPageId: string): void {
    if (this.lastStartedKey === key) return;
    this.lastStartedKey = key;
    setTimeout(() => void this.tour.start(tourPageId), 0);
  }

  private isCurrentRoute(expected: string, actual: string): boolean {
    return (
      this.normalize(expected).split('?')[0] ===
      this.normalize(actual).split('?')[0]
    );
  }

  private normalize(route: string): string {
    return route.startsWith('/') ? route : `/${route}`;
  }
}
