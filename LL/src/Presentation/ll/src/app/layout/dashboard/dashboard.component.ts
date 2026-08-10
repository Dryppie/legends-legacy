import {
  Component,
  effect,
  ElementRef,
  HostListener,
  OnInit,
  Signal,
  ViewChild,
} from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { RouterOutlet } from '@angular/router';
import { NgClass, NgIf } from '@angular/common';
import { ChatComponent } from './chat/chat.component';
import { LootTrackerComponent } from './loot-tracker/loot-tracker.component';
import { CharacterActionsStateService } from '../../core/services/api/character-actions/character-actions.state.service';
import { GameBootstrapStateService } from '../../core/services/api/game-bootstrap/game-bootstrap-state.service';
import { ChatLayoutPreferenceService } from '../../core/services/client-side/chat-layout/chat-layout-preference.service';
import { GameHeaderComponent } from './game-header/game-header.component';

export interface FloatingDrawerPosition {
  left: number;
  verticalOffset: number;
  verticalAnchor: 'top' | 'bottom';
}

export function getFloatingDrawerVerticalAnchor(
  drawerTop: number,
  drawerBottom: number,
  viewportHeight: number,
): 'top' | 'bottom' {
  return drawerTop <= viewportHeight - drawerBottom ? 'top' : 'bottom';
}

export function clampFloatingDrawerPosition(
  position: FloatingDrawerPosition,
  drawerWidth: number,
  drawerHeight: number,
  viewportWidth: number,
  viewportHeight: number,
  margin = 8,
): FloatingDrawerPosition {
  const maxLeft = Math.max(margin, viewportWidth - drawerWidth - margin);
  const maxVerticalOffset = Math.max(
    margin,
    viewportHeight - drawerHeight - margin,
  );
  return {
    left: Math.min(Math.max(position.left, margin), maxLeft),
    verticalOffset: Math.min(
      Math.max(position.verticalOffset, margin),
      maxVerticalOffset,
    ),
    verticalAnchor: position.verticalAnchor,
  };
}

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
  private static readonly floatingDrawerMargin = 8;

  @ViewChild('floatingChatDrawer')
  private floatingChatDrawer?: ElementRef<HTMLElement>;

  isSidebarOpen = false;
  isScreenSmall = false;
  isScreenLarge = false;
  isChatOpenDesktop = true; // open by default on ≥ lg
  isFloatingDrawerOpen = false;
  isFloatingDrawerTall = false;
  floatingDrawerPosition: FloatingDrawerPosition | null = null;
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
  private sidebarSwipeStartedOpen = false;
  private floatingDrawerDragPointerId: number | null = null;
  private floatingDrawerDragStartX = 0;
  private floatingDrawerDragStartY = 0;
  private floatingDrawerDragStartPosition: FloatingDrawerPosition | null = null;

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
    this.constrainFloatingDrawer();
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
    const targetSidebarWidth = window.innerWidth * 0.64;
    const canStartClosingSwipe =
      this.isSidebarOpen && !!touch && touch.clientX <= targetSidebarWidth;
    const canStartOpeningSwipe =
      !this.isSidebarOpen &&
      !!touch &&
      touch.clientX >= DashboardComponent.sidebarSwipeStartInset &&
      touch.clientX <= DashboardComponent.sidebarSwipeEndInset &&
      !startedOnInteractiveElement;

    if (
      !this.isScreenSmall ||
      event.touches.length !== 1 ||
      !touch ||
      (!canStartClosingSwipe && !canStartOpeningSwipe)
    ) {
      return;
    }

    this.sidebarSwipeTouchIdentifier = touch.identifier;
    this.sidebarSwipeStartX = touch.clientX;
    this.sidebarSwipeStartY = touch.clientY;
    this.sidebarSwipeStartTime = event.timeStamp;
    this.sidebarSwipeStartedOpen = this.isSidebarOpen;
    this.sidebarSwipeOffset = this.sidebarSwipeStartedOpen
      ? targetSidebarWidth
      : 0;
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
    const directionalDistance = this.sidebarSwipeStartedOpen
      ? -horizontalDistance
      : horizontalDistance;
    const directionalVelocity = this.sidebarSwipeStartedOpen
      ? -horizontalVelocity
      : horizontalVelocity;
    const hasExpectedDirection =
      directionalDistance >=
      verticalDistance * DashboardComponent.sidebarSwipeDirectionRatio;
    const completedSwipe =
      hasExpectedDirection &&
      (directionalDistance >= DashboardComponent.sidebarSwipeDistance ||
        (directionalDistance >= DashboardComponent.sidebarSwipeFlickDistance &&
          directionalVelocity >= DashboardComponent.sidebarSwipeFlickVelocity));

    if (completedSwipe && !this.sidebarSwipeStartedOpen) {
      this.isFloatingChatOpen = false;
      this.isFloatingDrawerOpen = false;
      this.isMobileChatExpanded = false;
    }

    this.isSidebarOpen = this.sidebarSwipeStartedOpen
      ? !completedSwipe
      : completedSwipe;
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
      this.constrainFloatingDrawerAfterResize();
      return;
    }

    if (this.isScreenLarge) {
      this.isChatOpenDesktop = !this.isChatOpenDesktop;
      return;
    }

    this.closeSidebar();
    this.isFloatingChatOpen = !this.isFloatingChatOpen;
  }

  setFloatingDrawerCollapsed(collapsed: boolean): void {
    this.isFloatingDrawerOpen = !collapsed;
    this.constrainFloatingDrawerAfterResize();
  }

  setFloatingDrawerTall(tall: boolean): void {
    this.isFloatingDrawerTall = tall;
    this.constrainFloatingDrawerAfterResize();
  }

  startFloatingDrawerDrag(event: PointerEvent): void {
    const drawer = this.floatingChatDrawer?.nativeElement;
    if (!drawer || (event.pointerType === 'mouse' && event.button !== 0)) {
      return;
    }

    const rect = drawer.getBoundingClientRect();
    const verticalAnchor =
      this.floatingDrawerPosition?.verticalAnchor ?? 'bottom';
    this.floatingDrawerDragPointerId = event.pointerId;
    this.floatingDrawerDragStartX = event.clientX;
    this.floatingDrawerDragStartY = event.clientY;
    this.floatingDrawerDragStartPosition = {
      left: rect.left,
      verticalOffset:
        verticalAnchor === 'top' ? rect.top : window.innerHeight - rect.bottom,
      verticalAnchor,
    };
    this.floatingDrawerPosition = this.floatingDrawerDragStartPosition;
  }

  moveFloatingDrawer(event: PointerEvent): void {
    if (
      this.floatingDrawerDragPointerId !== event.pointerId ||
      !this.floatingDrawerDragStartPosition
    ) {
      return;
    }

    const drawer = this.floatingChatDrawer?.nativeElement;
    if (!drawer) return;

    const rect = drawer.getBoundingClientRect();
    this.floatingDrawerPosition = clampFloatingDrawerPosition(
      {
        left:
          this.floatingDrawerDragStartPosition.left +
          event.clientX -
          this.floatingDrawerDragStartX,
        verticalOffset:
          this.floatingDrawerDragStartPosition.verticalOffset +
          (this.floatingDrawerDragStartPosition.verticalAnchor === 'top'
            ? event.clientY - this.floatingDrawerDragStartY
            : this.floatingDrawerDragStartY - event.clientY),
        verticalAnchor: this.floatingDrawerDragStartPosition.verticalAnchor,
      },
      rect.width,
      rect.height,
      window.innerWidth,
      window.innerHeight,
      DashboardComponent.floatingDrawerMargin,
    );
  }

  endFloatingDrawerDrag(event: PointerEvent): void {
    if (this.floatingDrawerDragPointerId !== event.pointerId) return;

    const drawer = this.floatingChatDrawer?.nativeElement;
    if (drawer) {
      const rect = drawer.getBoundingClientRect();
      const verticalAnchor = getFloatingDrawerVerticalAnchor(
        rect.top,
        rect.bottom,
        window.innerHeight,
      );
      this.floatingDrawerPosition = clampFloatingDrawerPosition(
        {
          left: rect.left,
          verticalOffset:
            verticalAnchor === 'top'
              ? rect.top
              : window.innerHeight - rect.bottom,
          verticalAnchor,
        },
        rect.width,
        rect.height,
        window.innerWidth,
        window.innerHeight,
        DashboardComponent.floatingDrawerMargin,
      );
    }

    this.floatingDrawerDragPointerId = null;
    this.floatingDrawerDragStartPosition = null;
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

  private constrainFloatingDrawer(): void {
    const drawer = this.floatingChatDrawer?.nativeElement;
    if (!drawer || !this.floatingDrawerPosition) return;

    const rect = drawer.getBoundingClientRect();
    this.floatingDrawerPosition = clampFloatingDrawerPosition(
      this.floatingDrawerPosition,
      rect.width,
      rect.height,
      window.innerWidth,
      window.innerHeight,
      DashboardComponent.floatingDrawerMargin,
    );
  }

  private constrainFloatingDrawerAfterResize(): void {
    setTimeout(() => this.constrainFloatingDrawer(), 220);
  }

  private updateSidebarSwipePosition(
    clientX: number,
    clientY: number,
  ): boolean {
    const horizontalDistance = clientX - this.sidebarSwipeStartX;
    const verticalDistance = Math.abs(clientY - this.sidebarSwipeStartY);
    const directionalDistance = this.sidebarSwipeStartedOpen
      ? -horizontalDistance
      : horizontalDistance;

    if (!this.isSidebarSwiping) {
      const movedDistance = Math.max(
        Math.abs(horizontalDistance),
        verticalDistance,
      );
      if (movedDistance < DashboardComponent.sidebarSwipeIntentDistance) {
        return false;
      }

      const hasHorizontalIntent =
        directionalDistance > 0 &&
        directionalDistance >=
          verticalDistance * DashboardComponent.sidebarSwipeDirectionRatio;
      if (!hasHorizontalIntent) {
        this.cancelActiveSidebarSwipe();
        return false;
      }

      this.isSidebarSwiping = true;
    }

    const targetSidebarWidth = window.innerWidth * 0.64;
    this.sidebarSwipeOffset = Math.min(
      Math.max(
        this.sidebarSwipeStartedOpen
          ? targetSidebarWidth + horizontalDistance
          : horizontalDistance,
        0,
      ),
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
    this.sidebarSwipeStartedOpen = false;
  }
}
