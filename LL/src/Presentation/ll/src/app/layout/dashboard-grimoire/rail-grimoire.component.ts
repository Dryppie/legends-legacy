import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SidebarComponent } from '../dashboard/sidebar/sidebar.component';
import { SidebarSection, Tab } from '../../shared/models/sidebar-item';
import { playerJourneySidebarLockReason } from '../../core/services/client-side/player-journey/player-journey';
import { environment } from '../../../environments/environment';
import { LgActivityComponent, LgIconName, LgNavRailComponent, LgNavSection, LgSlotDirective } from '@grimoire';
import { actionLabel, injectActionProgress } from './action-progress';

/** Grimoire icon names for the sidebar's destinations: the same drawings as the sidebar's own SVGs. */
export const RAIL_ICONS: Record<string, LgIconName> = {
  'character-overview': 'overview',
  inventory: 'inventory',
  essences: 'essences',
  'combat-styles': 'combat-styles',
  achievements: 'achievements',
  'soulstone-archive': 'soulstones',
  world: 'world-map',
  'legacy-ascension': 'legacy-ascension',
  quests: 'quest-journal',
  prophecies: 'prophecies',
  guild: 'guild',
  colosseum: 'colosseum',
  'market-place': 'cinder-bazaar',
  tavern: 'leaderboard',
  settings: 'settings',
};

export interface RailItemState {
  /** Why the journey keeps it back (D-106), or null. */
  lockReason: string | null;
  count: number;
  /** The journey's next destination. */
  next: boolean;
}

/**
 * Where a screen that isn't a destination itself belongs, so the rail always says where you are (D-115): a dungeon, raid,
 * region boss or any other screen under the world belongs to the World Map.
 */
export function railParentId(url: string): string | undefined {
  const path = url.split(/[?#]/)[0];
  return /^\/(?:game\/)?world(?:\/|$)/.test(path) ? 'world' : undefined;
}

/** The sidebar's sections in the NavRail's shape: every destination, each with its description (D-104). */
export function toRailSections(sections: readonly SidebarSection[], state: (item: Tab) => RailItemState): LgNavSection[] {
  return sections.map((section) => ({
    label: section.label,
    items: section.items.map((item) => {
      const s = state(item);
      return {
        id: item.id,
        title: item.title,
        description: item.description,
        icon: RAIL_ICONS[item.id],
        route: '/game/' + item.route.join('/'),
        locked: !!s.lockReason || undefined,
        reason: s.lockReason ?? undefined,
        badge: s.count > 0 ? s.count : undefined,
        badgeLabel:
          s.count > 0
            ? item.id === 'quests'
              ? `${s.count} ${s.count === 1 ? 'quest' : 'quests'} ready to turn in`
              : `${s.count} pending`
            : s.next
              ? 'Next on your journey'
              : undefined,
        ready: s.next || undefined,
      };
    }),
  }));
}

/**
 * The Grimoire rail (D-104 to D-106, D-109): the sidebar's own data and behaviour — journey, notifications, the current
 * action — drawn as a NavRail with the current action as an Activity at its head.
 */
@Component({
  selector: 'app-rail-grimoire',
  imports: [LgNavRailComponent, LgActivityComponent, LgSlotDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <lg-nav-rail [sections]="railSections()" [activeId]="activeId()" [compact]="compact()" (navigate)="onRailNavigate($event)">
      @if (hasAction()) {
        <lg-activity
          lgSlot="header"
          [label]="activityLabel()"
          [remaining]="progress.remaining()"
          [progress]="progress.progress()"
          [compact]="compact()"
          [short]="currentActionLabel()"
          [openLabel]="compact() ? '' : 'Go to action'"
          interactive
          (activate)="navigateToAction(); toggleSidebar()"
        />
      }
    </lg-nav-rail>
  `,
})
export class RailGrimoireComponent extends SidebarComponent {
  protected readonly progress = injectActionProgress();
  private readonly url = signal(this.router.url);
  protected readonly compact = computed(() => this.sidebarLayout() === 'compact');
  protected readonly hasAction = computed(() => this.state.displayCurrentAction());
  protected readonly activityLabel = computed(() => actionLabel(this.state.currentAction()));

  protected readonly railSections = computed(() => {
    const journal = this.questState.journal();
    const level = this.characterState.currentCharacter()?.level ?? 1;
    this.url();
    return toRailSections(this.sections(), (item) => ({
      lockReason: playerJourneySidebarLockReason(item, journal, level, environment.features.focusedBetaJourney),
      count: this.getSidebarItemNotificationCount(item.id),
      next: this.isQuestDestination(item),
    }));
  });

  protected readonly activeId = computed(() => {
    const url = this.url();
    for (const section of this.sections()) {
      for (const item of section.items) if (this.isItemActive(item.route)) return item.id;
    }
    return railParentId(url);
  });

  // The parent's own constructor takes the sidebar's services; this only follows the URL as a signal.
  private readonly followUrl = this.router.events
    .pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      takeUntilDestroyed(),
    )
    .subscribe((e) => this.url.set(e.urlAfterRedirects));

  protected onRailNavigate(id: string): void {
    const item = this.sections()
      .flatMap((s) => s.items)
      .find((i) => i.id === id);
    if (item) this.onNavigate(item);
  }
}
