import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  ViewChild,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import {
  ClaimDungeonRewardsResponse,
  DungeonMapNode,
  DungeonRun,
  DungeonRunStatus,
  DungeonRouteOption,
  RoomInstance,
} from '../../../../../../core/services/api/dungeon/dungeon.service';
import { DungeonStateService } from '../../../../../../core/services/api/dungeon/dungeon-state.service';
import { CombatStateService } from '../../../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../../../core/state/combat-state/combatState';
import { CombatComponent } from '../../../../../../shared/components/combat/combat.component';
import { DungeonRoomIconComponent } from '../../../../../../shared/components/dungeons/dungeon-room-icon/dungeon-room-icon.component';
import { InventoryItemComponent } from '../../../../../../shared/components/inventory-item/inventory-item.component';
import { InventoryItem } from '../../../../../../shared/models/inventoryItem';
import { HelpLauncherComponent } from '../../../../../../shared/help/help-launcher.component';

interface DungeonGraphNode extends DungeonMapNode {
  x: number;
  y: number;
  room: RoomInstance | null;
  route: DungeonRouteOption | null;
}

interface DungeonGraphEdge {
  key: string;
  fromRoomIndex: number;
  toRoomIndex: number;
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  isTraversed: boolean;
  isAvailable: boolean;
}

interface DungeonRewardResult {
  run: DungeonRun;
  claimedLoot: InventoryItem[];
}

interface DungeonVigorForecast {
  minimum: number;
  maximum: number;
}

@Component({
  selector: 'app-dungeon-page',
  imports: [
    NgIf,
    NgFor,
    NgClass,
    DecimalPipe,
    CombatComponent,
    DungeonRoomIconComponent,
    InventoryItemComponent,
    HelpLauncherComponent,
  ],
  templateUrl: './dungeon-page.component.html',
  styleUrl: './dungeon-page.component.scss',
})
export class DungeonPageComponent {
  readonly dungeonState = inject(DungeonStateService);
  readonly combatStateService = inject(CombatStateService);
  private readonly router = inject(Router);

  private dungeonMapScrollElement?: ElementRef<HTMLDivElement>;
  private dungeonLayoutScrollElement?: ElementRef<HTMLElement>;

  @ViewChild('dungeonLayout')
  private set dungeonLayoutScroll(
    element: ElementRef<HTMLElement> | undefined,
  ) {
    this.dungeonLayoutScrollElement = element;

    const roomIndex = this.activeDungeon()?.currentRoomIndex;
    if (element && roomIndex !== undefined && this.isVerticalMap()) {
      requestAnimationFrame(() => this.scrollMapToRoom(roomIndex));
    }
  }

  @ViewChild('dungeonMapScroll')
  private set dungeonMapScroll(
    element: ElementRef<HTMLDivElement> | undefined,
  ) {
    this.dungeonMapScrollElement = element;

    const roomIndex = this.activeDungeon()?.currentRoomIndex;
    if (element && roomIndex !== undefined) {
      requestAnimationFrame(() => this.scrollMapToRoom(roomIndex));
    }
  }

  private lastFollowedRoomIndex: number | null = null;
  private pendingRoomIndex: number | null = null;

  readonly battleType = BattleType.Dungeon;
  readonly activeDungeon = this.dungeonState.activeDungeon;
  readonly loading = this.dungeonState.loading;
  readonly error = this.dungeonState.error;
  readonly message = this.dungeonState.message;
  readonly claimedRewardResult = signal<DungeonRewardResult | null>(null);
  readonly viewportWidth = signal(
    typeof window === 'undefined' ? 1024 : window.innerWidth,
  );
  readonly isVerticalMap = computed(() => this.viewportWidth() < 640);

  readonly currentRoom = computed(() => {
    const run = this.activeDungeon();
    if (!run) return null;
    return (
      run.rooms.find((room) => room.index === run.currentRoomIndex) ?? null
    );
  });

  readonly mapNodes = computed<DungeonMapNode[]>(() => {
    const run = this.activeDungeon();
    if (!run) return [];

    if (run.state?.mapNodes?.length) {
      return [...run.state.mapNodes].sort(
        (left, right) =>
          left.depth - right.depth ||
          left.lane - right.lane ||
          left.roomIndex - right.roomIndex,
      );
    }

    return [];
  });

  readonly hasPlayableMap = computed(
    () => this.mapNodes().length > 0 && this.graphNodes().length > 0,
  );

  readonly totalDepths = computed(() => {
    const nodes = this.mapNodes();
    return nodes.length ? Math.max(...nodes.map((node) => node.depth)) + 1 : 0;
  });

  readonly currentDepth = computed(() => {
    const run = this.activeDungeon();
    if (!run) return 0;
    return (
      this.mapNodes().find((node) => node.roomIndex === run.currentRoomIndex)
        ?.depth ?? 0
    );
  });

  readonly currentDepthNumber = computed(() => {
    if (!this.totalDepths()) return 0;
    return Math.min(this.totalDepths(), this.currentDepth() + 1);
  });

  readonly routeOptions = computed(
    () => this.activeDungeon()?.state?.currentRouteOptions ?? [],
  );
  readonly vigorThresholdsExpanded = signal(false);
  readonly currentVigorThreshold = computed(() => {
    const state = this.activeDungeon()?.state;
    if (!state) return null;

    return (
      state.vigorThresholds.find((threshold) => threshold.isCurrent) ??
      state.vigorThresholds.find(
        (threshold) => threshold.state === state.vigorState,
      ) ??
      state.vigorThresholds[0] ??
      null
    );
  });
  readonly otherVigorThresholds = computed(() => {
    const current = this.currentVigorThreshold();
    return (
      this.activeDungeon()?.state?.vigorThresholds.filter(
        (threshold) => threshold !== current,
      ) ?? []
    );
  });
  readonly graphWidth = computed(() => {
    if (this.isVerticalMap()) {
      return Math.max(280, Math.min(360, this.viewportWidth() - 16));
    }

    return Math.max(760, (this.totalDepths() - 1) * 154 + 140);
  });
  readonly graphHeight = computed(() =>
    this.isVerticalMap()
      ? Math.max(560, (this.totalDepths() - 1) * 140 + 180)
      : 470,
  );

  readonly graphNodes = computed<DungeonGraphNode[]>(() => {
    const run = this.activeDungeon();
    if (!run) return [];
    const routes = this.routeOptions();

    const vertical = this.isVerticalMap();
    const width = this.graphWidth();
    const laneSpacing = Math.min(92, (width - 150) / 2);

    return this.mapNodes().map((node) => ({
      ...node,
      x: vertical ? width / 2 + node.lane * laneSpacing : 70 + node.depth * 154,
      y: vertical
        ? this.graphHeight() - 75 - node.depth * 140
        : this.graphHeight() / 2 + node.lane * 104,
      room: run.rooms.find((room) => room.index === node.roomIndex) ?? null,
      route: routes.find((route) => route.roomIndex === node.roomIndex) ?? null,
    }));
  });

  @HostListener('window:resize')
  onViewportResize(): void {
    const nextWidth = window.innerWidth;
    if (nextWidth === this.viewportWidth()) return;

    const orientationChanged = nextWidth < 640 !== this.isVerticalMap();
    this.viewportWidth.set(nextWidth);

    if (orientationChanged) {
      this.lastFollowedRoomIndex = null;
    }
  }

  constructor() {
    effect(() => {
      const roomIndex = this.activeDungeon()?.currentRoomIndex ?? null;
      this.graphNodes();

      if (
        roomIndex === null ||
        roomIndex === this.lastFollowedRoomIndex ||
        roomIndex === this.pendingRoomIndex
      ) {
        return;
      }

      this.pendingRoomIndex = roomIndex;
      requestAnimationFrame(() => this.scrollMapToRoom(roomIndex));
    });
  }

  private scrollMapToRoom(roomIndex: number): void {
    const viewport = this.isVerticalMap()
      ? this.dungeonLayoutScrollElement?.nativeElement
      : this.dungeonMapScrollElement?.nativeElement;
    const node = this.graphNodes().find(
      (candidate) => candidate.roomIndex === roomIndex,
    );

    if (!viewport || !node) {
      this.pendingRoomIndex = null;
      return;
    }

    const behavior = this.lastFollowedRoomIndex === null ? 'auto' : 'smooth';

    if (this.isVerticalMap()) {
      const maxScrollTop = Math.max(
        0,
        viewport.scrollHeight - viewport.clientHeight,
      );
      const targetScrollTop = Math.min(
        maxScrollTop,
        Math.max(0, node.y - viewport.clientHeight * 0.7),
      );

      viewport.scrollTo({
        top: targetScrollTop,
        behavior,
      });
    } else {
      const maxScrollLeft = Math.max(
        0,
        viewport.scrollWidth - viewport.clientWidth,
      );
      const targetScrollLeft = Math.min(
        maxScrollLeft,
        Math.max(0, node.x - viewport.clientWidth * 0.35),
      );

      viewport.scrollTo({
        left: targetScrollLeft,
        behavior,
      });
    }

    this.lastFollowedRoomIndex = roomIndex;
    this.pendingRoomIndex = null;
  }

  readonly graphEdges = computed<DungeonGraphEdge[]>(() => {
    const nodes = this.graphNodes();
    const nodeByRoom = new Map(nodes.map((node) => [node.roomIndex, node]));
    const traversed = this.activeDungeon()?.state?.traversedRoomIndexes ?? [];
    const routes = this.routeOptions();
    const edges: DungeonGraphEdge[] = [];

    for (const source of nodes) {
      const mapNode = this.mapNodes().find(
        (node) => node.roomIndex === source.roomIndex,
      );
      for (const targetRoomIndex of mapNode?.nextRoomIndexes ?? []) {
        const target = nodeByRoom.get(targetRoomIndex);
        if (!target) continue;

        const targetPathIndex = traversed.indexOf(targetRoomIndex);
        edges.push({
          key: `${source.roomIndex}:${targetRoomIndex}`,
          fromRoomIndex: source.roomIndex,
          toRoomIndex: targetRoomIndex,
          x1: source.x,
          y1: source.y,
          x2: target.x,
          y2: target.y,
          isTraversed:
            targetPathIndex > 0 &&
            traversed[targetPathIndex - 1] === source.roomIndex,
          isAvailable:
            source.roomIndex === this.activeDungeon()?.currentRoomIndex &&
            routes.some((route) => route.roomIndex === targetRoomIndex),
        });
      }
    }

    return edges;
  });

  readonly dungeonTitle = computed(
    () => this.activeDungeon()?.dungeonDefinitionName ?? 'Dungeon',
  );

  readonly phaseLabel = computed(() => {
    const run = this.activeDungeon();
    if (!run) return 'No active run';
    if (run.status === 'Completed') return 'Dungeon cleared';
    if (run.status === 'Retreated') return 'Loot secured';
    if (run.status === 'Failed') return 'Run failed';
    if (run.state.currentSection) return `Section ${run.state.currentSection}`;
    if (this.routeOptions().length) return 'Room cleared';
    return 'Exploring';
  });

  readonly decisionEyebrow = computed(() => {
    if (this.routeOptions().length) return 'Choose your path';
    if (this.currentRoom()?.type === 'RestSite') return 'Rest Site';
    return this.getRoomTypeLabel(this.currentRoom()?.type);
  });

  readonly decisionTitle = computed(() => {
    const run = this.activeDungeon();
    if (!run) return 'No active dungeon';
    if (run.status === 'Completed') return 'Dungeon complete';
    if (run.status === 'Retreated') return 'Pending Loot secured';
    if (run.status === 'Failed') return 'The expedition ended';
    if (this.routeOptions().length) return 'The dungeon branches ahead';
    if (this.currentRoom()?.type === 'RestSite') return 'Catch your breath';
    return this.currentRoomTitle();
  });

  readonly decisionDescription = computed(() => {
    const run = this.activeDungeon();
    const room = this.currentRoom();
    if (!run) return 'Choose a dungeon from the world map to begin.';
    if (this.loading()) return 'Resolving your last dungeon action...';
    if (run.status === 'Completed')
      return 'The boss is defeated. Claim the rewards and return to the world.';
    if (run.status === 'Retreated')
      return 'Everything earned during this run is ready to claim.';
    if (run.status === 'Failed')
      return 'Pending Loot was lost. Leave this run to begin another expedition.';
    if (this.routeOptions().length)
      return 'Choose the next combat route and compare its expected Vigor toll.';
    if (!room) return 'Preparing the next room.';

    switch (room.type) {
      case 'Combat':
        return 'Defeat the enemies here to reveal the next paths.';
      case 'MiniBoss':
        return 'An elite guardian blocks this route. Victory offers stronger rewards.';
      case 'Boss':
        return 'Defeat the dungeon boss to complete the expedition.';
      case 'RestSite':
        return `Rest here to recover ${this.restSiteRecovery()} Vigor before moving deeper.`;
      default:
        return 'Resolve this room to continue.';
    }
  });

  readonly primaryActionLabel = computed(() => {
    const run = this.activeDungeon();
    if (!run || this.loading()) return null;
    if (run.status === 'Completed' || run.status === 'Retreated')
      return 'Claim Rewards';
    if (run.status === 'Failed') return 'Leave Dungeon';
    return null;
  });

  readonly canRetreat = computed(
    () => this.activeDungeon()?.status === 'Active' && !this.loading(),
  );

  readonly pendingCurrencyRewards = computed(() => {
    const run = this.activeDungeon();
    return [
      { label: 'Cinders', value: run?.pendingCinders ?? 0 },
      { label: 'Experience', value: run?.pendingExperience ?? 0 },
      { label: 'Soulstones', value: run?.pendingSoulstones ?? 0 },
    ];
  });

  readonly pendingRewards = computed(
    () => this.activeDungeon()?.pendingRewards ?? [],
  );

  readonly clearedRoomCount = computed(
    () =>
      this.activeDungeon()?.rooms.filter((room) => room.status === 'Completed')
        .length ?? 0,
  );

  readonly failureTitle = computed(
    () =>
      this.activeDungeon()?.state?.failureAnalysis?.primaryCause ||
      'The expedition was defeated',
  );

  readonly failureExplanation = computed(
    () =>
      this.activeDungeon()?.state?.failureAnalysis?.explanation ||
      'The party could not overcome the final encounter. Pending Loot was lost.',
  );

  readonly failureLocation = computed(() => {
    const run = this.activeDungeon();
    if (!run) return 'Unknown';

    const analysis = run.state?.failureAnalysis;
    if (analysis?.location) {
      return analysis.section > 0
        ? `${analysis.location} · Section ${analysis.section}`
        : analysis.location;
    }

    return this.currentRoomTitle();
  });

  readonly failureSuggestions = computed(
    () =>
      this.activeDungeon()?.state?.failureAnalysis?.suggestions ?? [
        'Use Rest Sites before committing to the next Section.',
        'Choose a route whose Vigor forecast fits the party’s condition.',
      ],
  );

  readonly failedCurrencyRewards = computed(() => {
    const lost = this.activeDungeon()?.state?.failureAnalysis?.lostPendingLoot;
    return [
      { label: 'Cinders', value: lost?.cinders ?? 0 },
      { label: 'Experience', value: lost?.experience ?? 0 },
      { label: 'Soulstones', value: lost?.soulstones ?? 0 },
    ];
  });

  readonly failedItemRewards = computed(() => {
    const items =
      this.activeDungeon()?.state?.failureAnalysis?.lostPendingLoot?.items ??
      {};
    return Object.entries(items)
      .filter(([, quantity]) => quantity > 0)
      .map(([id, quantity]) => ({ id, quantity }));
  });

  readonly claimedCurrencyRewards = computed(() => {
    const result = this.claimedRewardResult();
    if (!result) return [];

    const run = result.run;
    const rewards =
      run.status === DungeonRunStatus.Retreated
        ? run.state.securedLoot
        : {
            cinders: run.pendingCinders,
            experience: run.pendingExperience,
            soulstones: run.pendingSoulstones,
          };

    return [
      { label: 'Cinders', value: rewards.cinders },
      { label: 'Experience', value: rewards.experience },
      { label: 'Soulstones', value: rewards.soulstones },
    ];
  });

  readonly claimedRoomCount = computed(
    () =>
      this.claimedRewardResult()?.run.rooms.filter(
        (room) => room.status === 'Completed',
      ).length ?? 0,
  );

  readonly rewardResultTitle = computed(() =>
    this.claimedRewardResult()?.run.status === DungeonRunStatus.Retreated
      ? 'Your expedition loot is secured'
      : 'The dungeon spoils are yours',
  );

  readonly rewardResultExplanation = computed(() =>
    this.claimedRewardResult()?.run.status === DungeonRunStatus.Retreated
      ? 'You withdrew safely. Everything secured during the expedition has been added to your character.'
      : 'The dungeon is cleared. Every reward from the expedition has been added to your character.',
  );

  readonly vigorPercent = computed(() =>
    Math.min(100, Math.max(0, this.activeDungeon()?.state?.vigor ?? 100)),
  );

  readonly vigorDepletedPercent = computed(() => 100 - this.vigorPercent());
  readonly vigorGradientClipPath = computed(
    () => `inset(0 ${this.vigorDepletedPercent()}% 0 0)`,
  );

  readonly statusNote = computed(() => {
    const run = this.activeDungeon();
    if (!run) return '';
    if (run.status === 'Completed') return 'Boss defeated';
    if (run.status === 'Retreated') return 'Rewards secured';
    if (run.status === 'Failed') return 'Pending Loot lost';
    if (this.routeOptions().length) return 'choose your path';
    return this.currentRoomTitle();
  });

  executePrimaryAction(): void {
    const run = this.activeDungeon();
    if (!run || this.loading()) return;

    if (run.status === 'Completed' || run.status === 'Retreated') {
      this.claimDungeonRewards();
      return;
    }
    if (run.status === 'Failed') {
      this.dismissFailedDungeonRun();
    }
  }

  chooseMapNode(node: DungeonGraphNode): void {
    if (this.loading()) return;

    if (node.route) {
      this.dungeonState.chooseRoute(node.route.id);
      return;
    }

    if (!this.isCurrentRoomActionNode(node)) return;

    switch (node.room?.type) {
      case 'Combat':
      case 'MiniBoss':
      case 'Boss':
        this.dungeonState.fight();
        break;
      case 'RestSite':
        this.dungeonState.restAtSite();
        break;
    }
  }

  retreatAndSecureLoot(): void {
    if (!this.canRetreat()) return;
    this.dungeonState.retreat();
  }

  claimDungeonRewards(): void {
    const run = this.activeDungeon();
    if (
      !run ||
      (run.status !== DungeonRunStatus.Completed &&
        run.status !== DungeonRunStatus.Retreated)
    ) {
      return;
    }

    this.dungeonState.claimDungeonRewards(
      (response: ClaimDungeonRewardsResponse) => {
        this.claimedRewardResult.set({
          run,
          claimedLoot: response.claimedLoot,
        });
      },
    );
  }

  returnToWorldAfterClaim(): void {
    const returnRoute = this.worldRouteForRun(
      this.claimedRewardResult()?.run ?? null,
    );
    this.claimedRewardResult.set(null);
    void this.router.navigate([returnRoute]);
  }

  dismissFailedDungeonRun(): void {
    const returnRoute = this.worldRouteForRun(this.activeDungeon());
    this.dungeonState.dismissFailedDungeonRun(() => {
      void this.router.navigate([returnRoute]);
    });
  }

  worldRouteForRun(run: DungeonRun | null): string {
    if (!run) return '/game/world/shenic';

    const definitionId = run.dungeonDefinitionId.toLowerCase();
    const region = this.dungeonState
      .dungeons()
      .find((dungeon) => dungeon.id.toLowerCase() === definitionId)?.region;

    return region === 2 ? '/game/world/meran' : '/game/world/shenic';
  }

  refresh(): void {
    this.dungeonState.refresh();
  }

  toggleVigorThresholds(event: MouseEvent): void {
    const expanded = !this.vigorThresholdsExpanded();
    this.vigorThresholdsExpanded.set(expanded);

    if (!expanded && event.currentTarget instanceof HTMLElement) {
      event.currentTarget.blur();
    }
  }

  skipBattle(): void {
    this.dungeonState.skipDungeonMatch();
  }

  isCurrentNode(node: DungeonGraphNode): boolean {
    return node.roomIndex === this.activeDungeon()?.currentRoomIndex;
  }

  isTraversedNode(node: DungeonGraphNode): boolean {
    return (
      this.activeDungeon()?.state?.traversedRoomIndexes?.includes(
        node.roomIndex,
      ) ?? false
    );
  }

  isMapNodeActionable(node: DungeonGraphNode): boolean {
    return !!node.route || this.isCurrentRoomActionNode(node);
  }

  isCurrentRoomActionNode(node: DungeonGraphNode): boolean {
    return (
      this.activeDungeon()?.status === 'Active' &&
      this.isCurrentNode(node) &&
      this.isDirectNodeActionRoomType(node.room?.type) &&
      node.room?.status !== 'Completed'
    );
  }

  currentRoomUsesDirectNodeAction(): boolean {
    const room = this.currentRoom();
    return (
      this.activeDungeon()?.status === 'Active' &&
      this.isDirectNodeActionRoomType(room?.type) &&
      room?.status !== 'Completed'
    );
  }

  mapNodeAriaLabel(node: DungeonGraphNode): string {
    if (node.room?.type === 'RestSite') {
      return `Rest at ${node.displayName} and recover ${this.restSiteRecovery()} Vigor`;
    }

    if (node.route) {
      return `Choose ${node.route.displayName}, ${this.getRoomTypeLabel(node.room?.type)}`;
    }

    if (!this.isCurrentRoomActionNode(node)) {
      return this.getRoomTypeLabel(node.room?.type);
    }

    return `Begin combat at ${node.displayName}`;
  }

  mapNodeTitle(node: DungeonGraphNode): string | null {
    if (node.room?.type === 'RestSite') {
      return `${node.displayName} · Rest · +${this.restSiteRecovery()} Vigor`;
    }

    if (node.route) {
      return `${node.route.displayName} · ${node.route.forecast} · Vigor ${node.route.vigorCostMin}–${node.route.vigorCostMax}`;
    }

    if (!this.isCurrentRoomActionNode(node)) return null;

    return 'Begin Combat';
  }

  mapNodeVigorForecast(node: DungeonGraphNode): DungeonVigorForecast | null {
    if (node.room?.type !== 'Combat' && node.room?.type !== 'MiniBoss') {
      return null;
    }

    if (node.route) {
      return {
        minimum: node.route.vigorCostMin,
        maximum: node.route.vigorCostMax,
      };
    }

    const authoredMinimum = node.vigorCostMin > 0 ? node.vigorCostMin : 12;
    const authoredMaximum =
      node.vigorCostMax >= authoredMinimum
        ? node.vigorCostMax
        : Math.max(authoredMinimum, 22);
    const widenForecast =
      this.activeDungeon()?.state?.vigorState === 'Strained' ||
      this.activeDungeon()?.state?.vigorState === 'Exhausted';
    const scaledMinimum = this.scaleVigorForecast(authoredMinimum);
    const scaledMaximum = this.scaleVigorForecast(authoredMaximum);

    return {
      minimum: widenForecast ? Math.max(0, scaledMinimum - 2) : scaledMinimum,
      maximum: widenForecast ? Math.min(35, scaledMaximum + 2) : scaledMaximum,
    };
  }

  private scaleVigorForecast(value: number): number {
    const reduction =
      this.activeDungeon()?.state?.masteryBenefits?.combatVigorCostReduction ??
      0;
    return Math.max(0, Math.round(Math.max(0, value) * 0.85) - reduction);
  }

  restSiteRecovery(): number {
    const bonus =
      this.activeDungeon()?.state?.masteryBenefits?.restSiteVigorBonus ?? 0;
    return 15 + bonus;
  }

  private isDirectNodeActionRoomType(type: string | null | undefined): boolean {
    return (
      type === 'Combat' ||
      type === 'MiniBoss' ||
      type === 'Boss' ||
      type === 'RestSite'
    );
  }

  nodeClass(node: DungeonGraphNode): string {
    if (node.route) return 'dungeon-node--available';
    if (this.isCurrentNode(node)) return 'dungeon-node--current';
    if (node.room?.status === 'Completed' || this.isTraversedNode(node))
      return 'dungeon-node--cleared';
    if (node.room?.type === 'Boss') return 'dungeon-node--boss';
    return 'dungeon-node--pending';
  }

  getRoomTypeLabel(type: string | null | undefined): string {
    switch (type) {
      case 'MiniBoss':
        return 'Miniboss';
      case 'RestSite':
        return 'Rest Site';
      case 'Unknown':
        return 'Unknown';
      default:
        return type || 'Unknown';
    }
  }

  currentRoomTitle(): string {
    const room = this.currentRoom();
    if (!room) return 'Preparing';
    const node = this.mapNodes().find(
      (candidate) => candidate.roomIndex === room.index,
    );
    if (node?.displayName) return node.displayName;
    return this.getRoomTypeLabel(room.type);
  }

  formatDelta(value: number): string {
    return value > 0 ? `+${value}` : `${value}`;
  }

  formatRewardSource(value: string | null | undefined): string {
    if (!value) return 'Dungeon';
    return value
      .replace(/[:_-]/g, ' ')
      .replace(/\b\w/g, (character) => character.toUpperCase());
  }

  trackByIndex(index: number): number {
    return index;
  }

  trackNode(_: number, node: DungeonGraphNode): number {
    return node.roomIndex;
  }

  trackEdge(_: number, edge: DungeonGraphEdge): string {
    return edge.key;
  }
}
