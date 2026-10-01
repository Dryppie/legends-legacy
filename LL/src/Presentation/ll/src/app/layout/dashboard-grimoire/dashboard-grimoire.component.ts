import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { DashboardComponent } from '../dashboard/dashboard.component';
import { CharacterStateService } from '../../core/services/api/character/character-state.service';
import { LocalStorageService } from '../../core/services/client-side/local-storage/local-storage.service';
import { LocalDatePipe } from '../../shared/pipes/local-date/local-date.pipe';
import {
  LgButtonComponent,
  LgChroniclePosition,
  LgCurrencyPillComponent,
  LgGameShellComponent,
  LgNoticeComponent,
  LgSlotDirective,
  LgTopBarComponent,
} from '../../shared/components/grimoire';
import { RailGrimoireComponent } from './rail-grimoire.component';
import { TopCenterGrimoireComponent } from './top-center-grimoire.component';
import { ChatGrimoireComponent } from './chat-grimoire.component';

/** The frame's backdrop: the game's own textured picture (D-107). */
export const GAME_BACKDROP = 'assets/backgrounds/optimized/background.webp';

/** True when the deepest active route says it is a Grimoire Page in the new look (`data.grimoireView`). */
export function isGrimoireView(route: ActivatedRoute): boolean {
  let r: ActivatedRoute | null = route;
  let view = false;
  while (r) {
    if (r.snapshot.data['grimoireView']) view = true;
    r = r.firstChild;
  }
  return view;
}

/**
 * The game's frame in the new look (GAME_SHELL_GRIMOIRE_PLAN.md): GameShell with the game's backdrop, the NavRail with
 * the current action, the TopBar with currencies and one "now" thing, the Chronicle, and shell notices. A screen that
 * has moved to Grimoire is a Page in the stage; one that hasn't sits in an lg-legacy region with the old surface and
 * gutters (D-108). The bootstrap, offline-progress and access state are the old frame's, inherited.
 */
@Component({
  selector: 'app-dashboard-grimoire',
  imports: [
    RouterOutlet,
    LocalDatePipe,
    LgGameShellComponent,
    LgTopBarComponent,
    LgCurrencyPillComponent,
    LgNoticeComponent,
    LgButtonComponent,
    LgSlotDirective,
    RailGrimoireComponent,
    TopCenterGrimoireComponent,
    ChatGrimoireComponent,
  ],
  changeDetection: ChangeDetectionStrategy.Default,
  templateUrl: './dashboard-grimoire.component.html',
  styleUrl: './dashboard-grimoire.component.scss',
  host: {
    class: 'lg-root dg-host',
    '(touchstart)': 'onTouchStart($event)',
    '(touchend)': 'onTouchEnd($event)',
  },
})
export class DashboardGrimoireComponent extends DashboardComponent {
  protected readonly backdrop = GAME_BACKDROP;
  protected readonly character = inject(CharacterStateService).currentCharacter;
  private readonly storage = inject(LocalStorageService);
  private readonly routerRef = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly shell = viewChild(LgGameShellComponent);

  protected readonly grimoireView = signal(isGrimoireView(this.activatedRoute));
  protected readonly chatPosition = signal<LgChroniclePosition | null>(null);
  protected readonly shortCurrency = signal(this.storage.get<boolean>('useShortFormat') ?? false);
  protected readonly level = computed(() => this.character()?.level ?? 1);

  private readonly followRoute = this.routerRef.events
    .pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      takeUntilDestroyed(),
    )
    .subscribe(() => this.grimoireView.set(isGrimoireView(this.activatedRoute)));

  protected toggleCurrencyFormat(): void {
    this.shortCurrency.update((v) => !v);
    this.storage.set('useShortFormat', this.shortCurrency());
  }

  // Phones: a swipe in from the left edge opens the rail drawer, as the old sidebar's did.
  private swipeStart: { x: number; y: number } | null = null;
  protected onTouchStart(event: TouchEvent): void {
    const t = event.touches.length === 1 ? event.touches.item(0) : null;
    const onControl = event.target instanceof Element && !!event.target.closest('a, button, input, [contenteditable="true"]');
    this.swipeStart = t && t.clientX >= 24 && t.clientX <= 112 && !onControl && window.innerWidth < 960 ? { x: t.clientX, y: t.clientY } : null;
  }
  protected onTouchEnd(event: TouchEvent): void {
    const start = this.swipeStart;
    const t = event.changedTouches.item(0);
    this.swipeStart = null;
    if (!start || !t) return;
    const dx = t.clientX - start.x;
    if (dx > 64 && Math.abs(dx) > Math.abs(t.clientY - start.y) * 1.25) this.shell()?.openRail();
  }
}
