import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  effect,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  signal,
  untracked,
} from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, Subject, takeUntil } from 'rxjs';
import { SidebarItemComponent } from './sidebar-item/sidebar-item.component';
import { SidebarService } from '../../../core/services/client-side/sidebar/sidebar.service';
import { CurrentActionComponent } from '../../../shared/components/current-action/current-action.component';
import { CurrentDungeonComponent } from '../../../shared/components/current-dungeon/current-dungeon.component';
import { CurrentRaidComponent } from '../../../shared/components/current-raid/current-raid.component';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { DungeonStateService } from '../../../core/services/api/dungeon/dungeon-state.service';
import { RaidService } from '../../../core/services/api/raid/raid.service';
import { CharacterActionType } from '../../../shared/models/enums/characterActionType';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { SidebarSection, Tab } from '../../../shared/models/sidebar-item';
import {
  NOTIFICATION_SURFACE,
  NotificationService,
} from '../../../core/services/client-side/notifications/notification.service';
import { SidebarNotificationRefreshService } from '../../../core/services/client-side/notifications/sidebar-notification-refresh.service';
import { EssenceStateService } from '../../../core/services/api/essences/essence-state.service';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { SidebarLayoutPreferenceService } from '../../../core/services/client-side/sidebar-layout/sidebar-layout-preference.service';
import { QuestStateService } from '../../../core/services/api/quest/quest-state.service';
import { QuestPresenterService } from '../../../core/services/api/quest/quest-presenter.service';
import { ProgressBarComponent } from '../../../shared/components/progress-bar/progress-bar.component';
import { RegionService } from '../../../core/services/client-side/region/region.service';
import {
  filterSidebarForPlayerJourney,
  getPlayerJourneyDestinationRoute,
} from '../../../core/services/client-side/player-journey/player-journey';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-sidebar',
  imports: [
    NgFor,
    NgIf,
    SidebarItemComponent,
    CurrentActionComponent,
    CurrentDungeonComponent,
    CurrentRaidComponent,
    ProgressBarComponent,
  ],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Output() itemTapped = new EventEmitter<void>();

  private readonly destroy$ = new Subject<void>();
  private readonly sections = signal<SidebarSection[]>([]);
  readonly visibleSections = computed(() =>
    filterSidebarForPlayerJourney(
      this.sections(),
      this.questState.journal(),
      this.characterState.currentCharacter()?.level ?? 1,
      environment.features.focusedBetaJourney,
    ),
  );
  activeUrl = '';
  displayCurrentAction = false;
  readonly sidebarLayout;
  readonly currentActionLabel = computed(() => {
    if (this.state.isActionCooldown()) return 'Stopping';

    switch (this.state.currentAction()?.characterActionType) {
      case CharacterActionType.Combat:
        return 'Battling';
      default:
        return 'Action';
    }
  });
  readonly hasActiveDungeon: DungeonStateService['hasActiveDungeon'];
  readonly hasActiveRaid: RaidService['hasActiveRaid'];
  constructor(
    private readonly sidebarService: SidebarService,
    private readonly state: CharacterActionsStateService,
    private readonly characterState: CharacterStateService,
    private readonly notificationService: NotificationService,
    private readonly sidebarNotificationRefreshService: SidebarNotificationRefreshService,
    public readonly essenceState: EssenceStateService,
    public readonly guildState: GuildStateService,
    private readonly sidebarLayoutPreference: SidebarLayoutPreferenceService,
    private readonly router: Router,
    private readonly questState: QuestStateService,
    private readonly questPresenter: QuestPresenterService,
    private readonly regionService: RegionService,
    dungeonState: DungeonStateService,
    raidService: RaidService,
  ) {
    this.hasActiveDungeon = dungeonState.hasActiveDungeon;
    this.hasActiveRaid = raidService.hasActiveRaid;
    this.sidebarLayout = this.sidebarLayoutPreference.layout;

    effect(() => {
      this.displayCurrentAction = this.state.displayCurrentAction();
    });

    effect(() => {
      const characterId = this.characterState.currentCharacterId();
      untracked(() =>
        this.sidebarNotificationRefreshService.refreshForCharacter(characterId),
      );
      untracked(() => {
        if (characterId) {
          this.essenceState.refreshCreatureArchive();
        }
      });
    });
  }

  ngOnInit(): void {
    this.activeUrl = this.router.url;

    this.sidebarService
      .getSidebar()
      .pipe(takeUntil(this.destroy$))
      .subscribe((sections) => {
        this.sections.set(sections);
      });

    this.router.events
      .pipe(
        filter(
          (event): event is NavigationEnd => event instanceof NavigationEnd,
        ),
        takeUntil(this.destroy$),
      )
      .subscribe((event) => {
        this.activeUrl = event.urlAfterRedirects;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleSidebar(): void {
    this.itemTapped.emit();
  }

  onNavigate(item: Tab): void {
    if (
      item.id === 'essences' &&
      this.questState.pinnedOnboardingObjective()?.type === 'EssenceEquipped'
    ) {
      this.essenceState.setActiveView('archive');
      this.questPresenter.presentCurrentObjective();
    }

    this.itemTapped.emit();
  }

  isItemActive(route: string[], exact = false): boolean {
    if (route.length === 1 && route[0] === 'world') {
      const match = /^\/(?:game\/)?world\/([^/]+)\/?$/.exec(
        this.routePath(this.activeUrl),
      );
      return !!match && this.regionService.isRegionId(match[1]);
    }

    return this.sidebarService.isRouteActive(route, exact);
  }

  trackSection(index: number, section: SidebarSection): string {
    return section.id;
  }

  trackItem(index: number, item: { id: string }): string {
    return item.id;
  }

  getNotificationCount(itemId: string): number {
    return this.notificationService.count(NOTIFICATION_SURFACE.Sidebar, itemId);
  }

  getSidebarItemNotificationCount(itemId: string): number {
    return (
      this.getNotificationCount(itemId) +
      (itemId === 'essences' && this.essenceState.essenceFocusReady() ? 1 : 0) +
      (itemId === 'guild' ? this.guildState.claimableDailyOrderCount() : 0)
    );
  }

  navigateToAction(): void {
    const action = this.state.currentAction();
    if (!action) return;

    const actionType = action.characterActionType;

    if (actionType === CharacterActionType.Combat) {
      this.router.navigate(['/game/combat']);
      return;
    }

  }

  isQuestDestination(item: Tab): boolean {
    if (!this.questState.pinnedOnboardingObjective()) return false;

    const destinationRoute = getPlayerJourneyDestinationRoute(
      this.questState.journal(),
    );
    if (!destinationRoute) return false;
    const destinationPath = this.routePath(destinationRoute);
    const itemRoute = `/${item.route.join('/')}`;
    const itemPath = itemRoute.startsWith('/game/')
      ? itemRoute
      : `/game${itemRoute}`;

    return (
      destinationPath === itemPath || destinationPath.startsWith(`${itemPath}/`)
    );
  }

  private routePath(route: string): string {
    const normalized = route.startsWith('/') ? route : `/${route}`;
    return normalized.split(/[?#]/, 1)[0];
  }
}
