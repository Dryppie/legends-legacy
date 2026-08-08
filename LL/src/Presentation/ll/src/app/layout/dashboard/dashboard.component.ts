import { Component, effect, HostListener, OnInit, Signal } from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { RouterOutlet } from '@angular/router';
import { NgClass, NgIf } from '@angular/common';
import { ChatComponent } from './chat/chat.component';
import { LootTrackerComponent } from './loot-tracker/loot-tracker.component';
import { CharacterActionsStateService } from '../../core/services/api/character-actions/character-actions.state.service';
import { GameBootstrapStateService } from '../../core/services/api/game-bootstrap/game-bootstrap-state.service';
import { ChatLayoutPreferenceService } from '../../core/services/client-side/chat-layout/chat-layout-preference.service';
import { GameHeaderComponent } from './game-header/game-header.component';

@Component({
  selector: 'app-dashboard',
  imports: [
    RouterOutlet,
    SidebarComponent,
    NgIf,
    NgClass,
    ChatComponent,
    LootTrackerComponent,
    GameHeaderComponent,
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
  isMobileChatExpanded = false;
  isResolvingAction = false;
  readonly bootstrapLoaded: Signal<boolean>;
  readonly bootstrapLoading: Signal<boolean>;
  readonly bootstrapError: Signal<string | null>;
  readonly idleCombatError: Signal<string | null>;
  readonly chatLayout;

  constructor(
    private readonly state: CharacterActionsStateService,
    private readonly bootstrapState: GameBootstrapStateService,
    private readonly chatLayoutPreference: ChatLayoutPreferenceService,
  ) {
    this.bootstrapLoaded = this.bootstrapState.loaded;
    this.bootstrapLoading = this.bootstrapState.loading;
    this.bootstrapError = this.bootstrapState.error;
    this.idleCombatError = this.state.idleCombatError;
    this.chatLayout = this.chatLayoutPreference.layout;

    effect(() => {
      this.isResolvingAction = this.state.loadingActionRefresh();
    });
  }

  ngOnInit() {
    this.checkScreenSize();
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

    if (!nextIsScreenSmall) {
      this.isMobileChatExpanded = false;
    }
  }

  toggleNav() {
    if (!this.isScreenSmall) return;

    this.isSidebarOpen = !this.isSidebarOpen;
    if (this.isSidebarOpen) {
      this.isMobileChatExpanded = false;
    }
  }

  toggleMobileChat(): void {
    this.closeSidebar();
    this.isMobileChatExpanded = !this.isMobileChatExpanded;
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

  retryBootstrap(): void {
    this.bootstrapState.reload().subscribe({
      error: () => undefined,
    });
  }

  retryOfflineProgress(): void {
    this.state.retryIdleCombatResolution();
  }
}
