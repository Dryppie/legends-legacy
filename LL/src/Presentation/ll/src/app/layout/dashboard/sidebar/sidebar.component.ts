import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  effect,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  untracked,
} from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { filter, Subject, takeUntil } from 'rxjs';
import { SidebarItemComponent } from './sidebar-item/sidebar-item.component';
import { SidebarService } from '../../../core/services/client-side/sidebar/sidebar.service';
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CurrentActionComponent } from '../../../shared/components/current-action/current-action.component';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../shared/models/enums/characterActionType';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { SidebarSection } from '../../../shared/models/sidebar-item';
import {
  NOTIFICATION_SURFACE,
  NotificationService,
} from '../../../core/services/client-side/notifications/notification.service';
import { SidebarNotificationRefreshService } from '../../../core/services/client-side/notifications/sidebar-notification-refresh.service';
import { EssenceStateService } from '../../../core/services/api/essences/essence-state.service';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { SidebarLayoutPreferenceService } from '../../../core/services/client-side/sidebar-layout/sidebar-layout-preference.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    SidebarItemComponent,
    RouterLink,
    CurrentActionComponent,
  ],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Output() itemTapped = new EventEmitter<void>();

  private readonly destroy$ = new Subject<void>();

  sections: SidebarSection[] = [];
  activeUrl = '';
  displayCurrentAction = false;
  readonly sidebarLayout;

  constructor(
    private readonly sidebarService: SidebarService,
    private readonly gameService: GameService,
    private readonly state: CharacterActionsStateService,
    private readonly characterState: CharacterStateService,
    private readonly notificationService: NotificationService,
    private readonly sidebarNotificationRefreshService: SidebarNotificationRefreshService,
    public readonly essenceState: EssenceStateService,
    public readonly guildState: GuildStateService,
    private readonly sidebarLayoutPreference: SidebarLayoutPreferenceService,
    private readonly router: Router,
  ) {
    this.sidebarLayout = this.sidebarLayoutPreference.layout;

    effect(() => {
      this.displayCurrentAction = this.state.displayCurrentAction();
    });

    effect(() => {
      const characterId = this.characterState.currentCharacter()?.id;
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
        this.sections = sections;
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

  onNavigate(): void {
    this.gameService.hideCombat();
    this.itemTapped.emit();
  }

  isItemActive(route: string[], exact = false): boolean {
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
    const now = Date.now();
    const updatedAt = new Date(action.updatedAt ?? 0).getTime();

    if (updatedAt > now) {
      this.gameService.showCombat();
      return;
    }

    if (actionType === CharacterActionType.Crafting) {
      this.router.navigate(['game', 'professions', 'crafting']);
    } else {
      return;
    }
  }
}
