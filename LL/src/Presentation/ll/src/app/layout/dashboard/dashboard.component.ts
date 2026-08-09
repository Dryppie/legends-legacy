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
  private static readonly sidebarSwipeDistance = 64;
  private static readonly sidebarSwipeFlickDistance = 32;
  private static readonly sidebarSwipeFlickVelocity = 0.45;
  private static readonly sidebarSwipeDirectionRatio = 1.25;
  private static readonly sidebarSwipeIntentDistance = 10;
  private static readonly sidebarSwipeStartInset = 24;
  private static readonly sidebarSwipeEndInset = 112;

  isSidebarOpen = false;
  isScreenSmall = false;
  isScreenLarge = false;
  isChatOpenDesktop = true; // open by default on ≥ lg
  isFloatingDrawerOpen = false;
  isFloatingDrawerTall = false;
  isFloatingChatOpen = false;
  isMobileChatExpanded = false;
  isSidebarSwiping = false;
  sidebarSwipeOffset = 0;
  isResolvingAction = false;
  readonly bootstrapLoaded: Signal<boolean>;
  readonly bootstrapLoading: Signal<boolean>;
  readonly bootstrapError: Signal<string | null>;
  readonly idleCombatError: Signal<string | null>;
  readonly chatLayout;
  private sidebarSwipeTouchIdentifier: number | null = null;
  private sidebarSwipeStartX = 0;
  private sidebarSwipeStartY = 0;
  private sidebarSwipeStartTime = 0;

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
      this.cancelActiveSidebarSwipe();
    }
  }

  toggleNav() {
    if (!this.isScreenSmall) return;

    this.isSidebarOpen = !this.isSidebarOpen;
    if (this.isSidebarOpen) {
      this.isMobileChatExpanded = false;
    }
  }

  startSidebarSwipe(event: TouchEvent): void {
    const eventTarget = event.target;
    const startedOnInteractiveElement =
      eventTarget instanceof Element &&
      !!eventTarget.closest(
        'a, button, input, select, textarea, [contenteditable="true"], [role="button"]',
      );

    const touch = event.changedTouches.item(0);
    if (
      !this.isScreenSmall ||
      this.isSidebarOpen ||
      event.touches.length !== 1 ||
      !touch ||
      touch.clientX < DashboardComponent.sidebarSwipeStartInset ||
      touch.clientX > DashboardComponent.sidebarSwipeEndInset ||
      startedOnInteractiveElement
    ) {
      return;
    }

    this.sidebarSwipeTouchIdentifier = touch.identifier;
    this.sidebarSwipeStartX = touch.clientX;
    this.sidebarSwipeStartY = touch.clientY;
    this.sidebarSwipeStartTime = event.timeStamp;
    this.sidebarSwipeOffset = 0;
  }

  updateSidebarSwipe(event: TouchEvent): void {
    const touch = this.findTrackedTouch(event.touches);
    if (!touch) return;

    if (this.updateSidebarSwipePosition(touch.clientX, touch.clientY)) {
      event.preventDefault();
    }
  }

  finishSidebarSwipe(event: TouchEvent): void {
    const touch = this.findTrackedTouch(event.changedTouches);
    if (!touch) return;

    this.updateSidebarSwipePosition(touch.clientX, touch.clientY);
    if (!this.isSidebarSwiping) {
      this.resetSidebarSwipePointer();
      return;
    }

    event.preventDefault();
    const horizontalDistance = touch.clientX - this.sidebarSwipeStartX;
    const verticalDistance = Math.abs(touch.clientY - this.sidebarSwipeStartY);
    const elapsedTime = Math.max(
      event.timeStamp - this.sidebarSwipeStartTime,
      1,
    );
    const horizontalVelocity = horizontalDistance / elapsedTime;
    const hasOpeningDirection =
      horizontalDistance >=
      verticalDistance * DashboardComponent.sidebarSwipeDirectionRatio;
    const isOpeningSwipe =
      hasOpeningDirection &&
      (horizontalDistance >= DashboardComponent.sidebarSwipeDistance ||
        (horizontalDistance >= DashboardComponent.sidebarSwipeFlickDistance &&
          horizontalVelocity >= DashboardComponent.sidebarSwipeFlickVelocity));

    if (isOpeningSwipe) {
      this.isFloatingChatOpen = false;
      this.isFloatingDrawerOpen = false;
      this.isMobileChatExpanded = false;
    }

    this.isSidebarOpen = isOpeningSwipe;
    this.isSidebarSwiping = false;
    this.sidebarSwipeOffset = 0;
    this.resetSidebarSwipePointer();
  }

  cancelSidebarSwipe(): void {
    this.cancelActiveSidebarSwipe();
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
    this.isMobileChatExpanded = false;
    this.cancelActiveSidebarSwipe();
    this.isSidebarOpen = true;
  }

  closeSidebar() {
    if (this.isScreenSmall) {
      this.cancelActiveSidebarSwipe();
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

  private cancelActiveSidebarSwipe(): void {
    this.isSidebarSwiping = false;
    this.sidebarSwipeOffset = 0;
    this.resetSidebarSwipePointer();
  }

  private updateSidebarSwipePosition(
    clientX: number,
    clientY: number,
  ): boolean {
    const horizontalDistance = clientX - this.sidebarSwipeStartX;
    const verticalDistance = Math.abs(clientY - this.sidebarSwipeStartY);

    if (!this.isSidebarSwiping) {
      const movedDistance = Math.max(
        Math.abs(horizontalDistance),
        verticalDistance,
      );
      if (movedDistance < DashboardComponent.sidebarSwipeIntentDistance) {
        return false;
      }

      const hasHorizontalIntent =
        horizontalDistance > 0 &&
        horizontalDistance >=
          verticalDistance * DashboardComponent.sidebarSwipeDirectionRatio;
      if (!hasHorizontalIntent) {
        this.cancelActiveSidebarSwipe();
        return false;
      }

      this.isSidebarSwiping = true;
    }

    const targetSidebarWidth = window.innerWidth * 0.64;
    this.sidebarSwipeOffset = Math.min(
      Math.max(horizontalDistance, 0),
      targetSidebarWidth,
    );
    return true;
  }

  private findTrackedTouch(touches: TouchList): Touch | null {
    if (this.sidebarSwipeTouchIdentifier === null) return null;

    for (let index = 0; index < touches.length; index += 1) {
      const touch = touches.item(index);
      if (touch?.identifier === this.sidebarSwipeTouchIdentifier) {
        return touch;
      }
    }

    return null;
  }

  private resetSidebarSwipePointer(): void {
    this.sidebarSwipeTouchIdentifier = null;
    this.sidebarSwipeStartX = 0;
    this.sidebarSwipeStartY = 0;
    this.sidebarSwipeStartTime = 0;
  }
}
