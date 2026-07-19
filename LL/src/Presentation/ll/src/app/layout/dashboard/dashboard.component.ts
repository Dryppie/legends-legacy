import { Component, effect, HostListener, OnInit, Signal } from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { Router, RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar/navbar.component';
import { AsyncPipe, NgClass, NgIf } from '@angular/common';
import { Observable } from 'rxjs';
import { GameService } from '../../core/services/client-side/game/game.service';
import { CombatComponent } from '../../shared/components/combat/combat.component';
import { ChatComponent } from './chat/chat.component';
import { LootTrackerComponent } from './loot-tracker/loot-tracker.component';
import { CurrentActionComponent } from '../../shared/components/current-action/current-action.component';
import { CharacterActionsStateService } from '../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../shared/models/enums/characterActionType';
import { TutorialQuestComponent } from './tutorial-quest/tutorial-quest.component';
import { GameBootstrapStateService } from '../../core/services/api/game-bootstrap/game-bootstrap-state.service';
import { ChatLayoutPreferenceService } from '../../core/services/client-side/chat-layout/chat-layout-preference.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterOutlet,
    SidebarComponent,
    NavbarComponent,
    NgIf,
    NgClass,
    AsyncPipe,
    CombatComponent,
    ChatComponent,
    LootTrackerComponent,
    CurrentActionComponent,
    TutorialQuestComponent,
  ],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  isSidebarOpen = false;
  isScreenSmall = false;
  isScreenLarge = false;
  isChatOpenDesktop = true; // open by default on ≥ lg
  isFloatingDrawerOpen = false;
  isFloatingDrawerTall = false;
  isFloatingChatOpen = false;
  displayCurrentAction = false;
  isResolvingAction = false;
  combatVisible$!: Observable<boolean>;
  readonly bootstrapLoaded: Signal<boolean>;
  readonly bootstrapLoading: Signal<boolean>;
  readonly bootstrapError: Signal<string | null>;
  readonly chatLayout;

  constructor(
    private readonly gameService: GameService,
    private readonly state: CharacterActionsStateService,
    private readonly router: Router,
    private readonly bootstrapState: GameBootstrapStateService,
    private readonly chatLayoutPreference: ChatLayoutPreferenceService,
  ) {
    this.bootstrapLoaded = this.bootstrapState.loaded;
    this.bootstrapLoading = this.bootstrapState.loading;
    this.bootstrapError = this.bootstrapState.error;
    this.chatLayout = this.chatLayoutPreference.layout;

    effect(() => {
      this.displayCurrentAction = this.state.displayCurrentAction();
    });

    effect(() => {
      this.isResolvingAction = this.state.loadingActionRefresh();
    });
  }

  ngOnInit() {
    this.checkScreenSize();
    this.combatVisible$ = this.gameService.combatVisible$;
    this.bootstrapState.load().subscribe({
      error: () => undefined,
    });
  }

  @HostListener('window:resize')
  onResize() {
    this.checkScreenSize();
  }

  checkScreenSize() {
    const nextIsScreenSmall = window.innerWidth < 640;
    const nextIsScreenLarge = window.innerWidth >= 1280;

    if (nextIsScreenSmall && !this.isScreenSmall) {
      this.isSidebarOpen = false;
    }

    if (!nextIsScreenSmall) {
      this.isSidebarOpen = true;
    }

    this.isScreenSmall = nextIsScreenSmall;
    this.isScreenLarge = nextIsScreenLarge;

    if (nextIsScreenLarge) {
      this.isFloatingChatOpen = false;
    }
  }

  toggleNav() {
    if (!this.isScreenSmall) return;

    this.isSidebarOpen = !this.isSidebarOpen;
  }

  toggleChat(): void {
    if (this.chatLayout() === 'floating') {
      this.isFloatingDrawerOpen = !this.isFloatingDrawerOpen;
      return;
    }

    if (this.isScreenLarge) {
      this.isChatOpenDesktop = !this.isChatOpenDesktop;
      return;
    }

    this.closeSidebar();
    this.isFloatingChatOpen = !this.isFloatingChatOpen;
  }

  openSidebar() {
    this.isFloatingChatOpen = false;
    this.isFloatingDrawerOpen = false;
    this.isSidebarOpen = true;
  }

  closeSidebar() {
    if (this.isScreenSmall) {
      this.isSidebarOpen = false;
    }
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

  retryBootstrap(): void {
    this.bootstrapState.reload().subscribe({
      error: () => undefined,
    });
  }
}
