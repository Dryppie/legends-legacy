import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  effect,
  ElementRef,
  NgZone,
  OnDestroy,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Area,
  AreaDrop,
  Region,
} from '../../../../shared/models/Dtos/regionDto';
import { RegionService } from '../../../../core/services/client-side/region/region.service';
import { CombatAreaCardComponent } from '../../../../shared/components/combat/combat-area-card/combat-area-card.component';
import { DungeonsComponent } from './dungeons/dungeons.component';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { TRAINING_GROUNDS_AREA_ID } from '../../../../shared/models/quest';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import { DungeonPreviewData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { HelpLauncherComponent } from '../../../../shared/help/help-launcher.component';
import { RaidsComponent } from './raids/raids.component';
import {
  RaidBossSummary,
  RaidService,
} from '../../../../core/services/api/raid/raid.service';
import { StateSyncCoordinator } from '../../../../core/services/real-time/game-realtime/state-sync-coordinator.service';
import { environment } from '../../../../../environments/environment';
import { RegionBossComponent } from '../region-boss/region-boss.component';
import { EssencesService } from '../../../../core/services/api/essences/essences.service';
import { SoulArchiveDto } from '../../../../shared/models/essence-system';
import { calculateAreaEssenceProgress } from './area-essence-progress';

interface WorldMapDungeonEntry {
  id: string;
  title: string;
  requiredTowerFloor: number | null;
  ownedSigilCount: number;
  canEnter: boolean;
}

const PRE_IMPLEMENTATION_SIGIL_DROPS_BY_AREA: Readonly<
  Record<string, readonly AreaDrop[]>
> = {
  region_02_area_01: [
    { itemId: 'sigil_tangled_cave', name: 'Silkbound Sigil' },
    { itemId: 'sigil_great_tree', name: 'Heartwood Sigil' },
  ],
  region_02_area_02: [
    { itemId: 'sigil_tangled_cave', name: 'Silkbound Sigil' },
    { itemId: 'sigil_great_tree', name: 'Heartwood Sigil' },
  ],
  region_02_area_03: [
    { itemId: 'sigil_tangled_cave', name: 'Silkbound Sigil' },
    { itemId: 'sigil_great_tree', name: 'Heartwood Sigil' },
  ],
  region_02_area_04: [
    { itemId: 'sigil_tangled_cave', name: 'Silkbound Sigil' },
    { itemId: 'sigil_great_tree', name: 'Heartwood Sigil' },
  ],
};

@Component({
  selector: 'app-region',
  imports: [
    NgIf,
    NgFor,
    CombatAreaCardComponent,
    DungeonsComponent,
    CombatComponent,
    RouterLink,
    HelpLauncherComponent,
    RaidsComponent,
    RegionBossComponent,
  ],
  templateUrl: './region.component.html',
  styleUrl: './region.component.scss',
})
export class RegionComponent implements OnInit, OnDestroy {
  readonly focusedBetaJourney = environment.features.focusedBetaJourney;
  readonly raidsEnabled =
    environment.features.raids && !environment.features.focusedBetaJourney;
  readonly regionBossEnabled = !environment.features.focusedBetaJourney;
  regionId = '';
  region!: Region; // You can define a more specific type based on your item data structure
  private sourceRegion: Region | null = null;
  private soulArchive: SoulArchiveDto | null = null;
  targetAreaId: string | null = null;
  readonly trainingBattleType = BattleType.Training;
  readonly activeBattle;
  selectedDungeonId: string | null = null;
  readonly raidBosses = signal<RaidBossSummary[]>([]);
  selectedRaidBossId: string | null = null;
  selectedRegionBoss = false;
  regionBossPlaybackActive = false;
  columnCount = 1;
  private contentResizeObserver: ResizeObserver | null = null;
  private readonly raidSyncCleanup: () => void;

  constructor(
    private route: ActivatedRoute,
    private regionService: RegionService,
    public readonly combatStateService: CombatStateService,
    private readonly combatService: CombatService,
    private readonly questState: QuestStateService,
    private readonly dungeonState: DungeonStateService,
    private readonly raids: RaidService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly ngZone: NgZone,
    private readonly essences: EssencesService,
    characterActions: CharacterActionsStateService,
  ) {
    this.raidSyncCleanup = this.stateSync.register(
      'raid-directory',
      'world-map-raids',
      async () => this.loadRaidBosses(),
      () => this.raidsEnabled && this.regionNumber() !== null,
    );

    this.activeBattle = computed(() => {
      const action = characterActions.currentAction();
      if (action?.characterActionType !== CharacterActionType.Combat) {
        return null;
      }

      return {
        areaId: action.combatActionDetails?.area?.id ?? null,
        areaName: action.combatActionDetails?.area?.name ?? 'Current encounter',
      };
    });

    effect(() => {
      this.questState.areaAccess();
      this.dungeonState.dungeons();
      this.regionNumber();
      this.stateSync.activate('raid-directory', 'world-map-raids');
      this.applyRegionView();
    });
  }

  ngOnInit(): void {
    this.loadEssenceProgress();

    this.route.paramMap.subscribe((params) => {
      this.regionId = params.get('id') ?? '';
      this.selectedDungeonId = null;
      this.selectedRaidBossId = null;
      this.selectedRegionBoss = false;
      this.regionBossPlaybackActive = false;
      this.questState.loadAreaAccess();
      this.getRegionDetails(this.regionId);
      this.loadRaidBosses();
    });

    this.route.queryParamMap.subscribe((params) => {
      this.targetAreaId = params.get('area');
      if (this.targetAreaId !== TRAINING_GROUNDS_AREA_ID) {
        this.closeTrainingSummary();
      }
      this.applyRegionView();
    });
  }

  ngOnDestroy(): void {
    this.raidSyncCleanup();
    this.contentResizeObserver?.disconnect();
    this.closeTrainingSummary();
  }

  @ViewChild('worldMapContent')
  set worldMapContent(content: ElementRef<HTMLElement> | undefined) {
    this.contentResizeObserver?.disconnect();
    this.contentResizeObserver = null;
    if (!content) return;

    const element = content.nativeElement;
    this.contentResizeObserver = new ResizeObserver(([entry]) => {
      this.ngZone.run(() => {
        this.setColumnCount(element, entry.contentRect.width);
      });
    });
    this.contentResizeObserver.observe(element);
  }

  getRegionDetails(id: string) {
    this.regionService.getRegionById(id).subscribe((data: any) => {
      this.sourceRegion = data as Region;
      this.applyRegionView();
    });
  }

  private setColumnCount(content: HTMLElement, contentWidth: number): void {
    if (window.matchMedia('(max-width: 42rem)').matches) {
      this.columnCount = 1;
      return;
    }

    const areaGrid = content.querySelector<HTMLElement>('.area-grid');
    const firstCard = areaGrid?.querySelector<HTMLElement>(
      'app-combat-area-card',
    );
    if (!areaGrid || !firstCard) return;

    const rootFontSize = Number.parseFloat(
      getComputedStyle(document.documentElement).fontSize,
    );
    const layoutGap = Number.parseFloat(getComputedStyle(content).columnGap);
    const cardGap = Number.parseFloat(getComputedStyle(areaGrid).columnGap);
    const cardWidth = firstCard.getBoundingClientRect().width;
    const isStacked =
      getComputedStyle(content).gridTemplateColumns.trim().split(/\s+/)
        .length === 1;
    const reservedRailWidth = isStacked ? 0 : 17 * rootFontSize + layoutGap;
    const availableAreaWidth = Math.max(
      cardWidth,
      contentWidth - reservedRailWidth,
    );
    const nextColumnCount = Math.floor(
      (availableAreaWidth + cardGap) / (cardWidth + cardGap),
    );

    this.columnCount = Math.max(1, Math.min(4, nextColumnCount));
  }

  isLastInRow(index: number): boolean {
    return (index + 1) % this.columnCount === 0;
  }

  private applyRegionView(): void {
    if (!this.sourceRegion) {
      return;
    }

    this.region = this.withQuestAreaAvailability(this.sourceRegion);
  }

  private withQuestAreaAvailability(region: Region): Region {
    return {
      ...region,
      areas: region.areas
        .filter(
          (area) =>
            this.questState.accessFor(area.id)?.isVisible !== false &&
            (!this.focusedBetaJourney || area.levelRequirement <= 30),
        )
        .map((area) => ({
          ...area,
          essenceProgress: calculateAreaEssenceProgress(area, this.soulArchive),
          possibleDrops: this.possibleDropsFor(area),
        })),
    };
  }

  private loadEssenceProgress(): void {
    this.essences.getArchive().subscribe({
      next: (archive) => {
        this.soulArchive = archive;
        this.applyRegionView();
      },
    });
  }

  private possibleDropsFor(area: Area): AreaDrop[] {
    const region = this.areaRegion(area.id);
    if (region === null) {
      return [];
    }

    const drops = new Map<string, AreaDrop>(
      (PRE_IMPLEMENTATION_SIGIL_DROPS_BY_AREA[area.id] ?? []).map((drop) => [
        drop.itemId,
        drop,
      ]),
    );
    for (const dungeon of this.dungeonState.dungeons()) {
      if (
        dungeon.region !== region ||
        !dungeon.sigilItemId ||
        !dungeon.sigilName
      ) {
        continue;
      }

      drops.set(dungeon.sigilItemId, {
        itemId: dungeon.sigilItemId,
        name: dungeon.sigilName,
      });
    }

    return Array.from(drops.values()).sort((left, right) =>
      left.name.localeCompare(right.name),
    );
  }

  private areaRegion(areaId: string): number | null {
    const match = /^region_(\d+)_area_/i.exec(areaId);
    return match ? Number.parseInt(match[1], 10) : null;
  }

  closeTrainingSummary(): void {
    this.combatService.closeCurrentTrainingBattle();
  }

  isMeranUnlocked(): boolean {
    return (
      this.questState.accessFor('region_02_area_01')
        ?.isRequiredTowerFloorCleared === true
    );
  }

  isRegionTowerLocked(): boolean {
    return !!this.region?.requiredTowerFloor && !this.isMeranUnlocked();
  }

  regionLevelRange(): string {
    if (!this.region?.areas.length) return '';

    const levels = this.region.areas.map((area) => area.levelRequirement);
    return `Lv. ${Math.min(...levels)}–${Math.max(...levels)}`;
  }

  unlockedAreaCount(): number {
    return (
      this.region?.areas.filter(
        (area) => this.questState.accessFor(area.id)?.canAccess === true,
      ).length ?? 0
    );
  }

  regionNumber(): number | null {
    switch (this.regionId.toLowerCase()) {
      case 'shenic':
        return 1;
      case 'meran':
        return 2;
      default:
        return null;
    }
  }

  regionDungeons(): WorldMapDungeonEntry[] {
    const regionNumber = this.regionNumber();
    if (regionNumber === null) return [];

    const families = new Map<string, DungeonPreviewData[]>();
    for (const dungeon of this.dungeonState.dungeons()) {
      if (dungeon.region !== regionNumber) continue;

      const familyId = dungeon.familyId ?? dungeon.id;
      families.set(familyId, [...(families.get(familyId) ?? []), dungeon]);
    }

    return Array.from(families.entries()).map(([id, variants]) => {
      const ownedSigilCount = variants.reduce((highestCount, variant) => {
        const sigilRequirement = variant.entryRequirements?.find(
          (requirement) => requirement.itemId === variant.sigilItemId,
        );

        return Math.max(highestCount, sigilRequirement?.ownedAmount ?? 0);
      }, 0);

      return {
        id,
        title: variants[0]?.familyTitle ?? variants[0]?.title ?? id,
        requiredTowerFloor:
          variants.find((variant) => variant.requiredTowerFloor != null)
            ?.requiredTowerFloor ?? null,
        ownedSigilCount,
        canEnter: variants.some((variant) => variant.canEnter ?? true),
      };
    });
  }

  trackDungeon(_: number, dungeon: WorldMapDungeonEntry): string {
    return dungeon.id;
  }

  selectDungeon(dungeonId: string): void {
    this.selectedDungeonId = dungeonId;
    this.selectedRaidBossId = null;
    this.selectedRegionBoss = false;
    this.regionBossPlaybackActive = false;
  }

  regionRaidBosses(): RaidBossSummary[] {
    if (!this.raidsEnabled) return [];

    const region = this.regionNumber();
    return region === null
      ? []
      : this.raidBosses().filter((boss) => boss.region === region);
  }

  trackRaidBoss(_: number, boss: RaidBossSummary): string {
    return boss.id;
  }

  selectRaidBoss(raidBossId: string): void {
    if (!this.raidsEnabled) return;

    this.selectedRaidBossId = raidBossId;
    this.selectedDungeonId = null;
    this.selectedRegionBoss = false;
    this.regionBossPlaybackActive = false;
  }

  selectRegionBoss(): void {
    this.selectedRegionBoss = true;
    this.selectedDungeonId = null;
    this.selectedRaidBossId = null;
    this.regionBossPlaybackActive = false;
  }

  selectedRaidBoss(): RaidBossSummary | null {
    if (!this.raidsEnabled) return null;

    return (
      this.regionRaidBosses().find(
        (boss) => boss.id === this.selectedRaidBossId,
      ) ?? null
    );
  }

  private loadRaidBosses(): void {
    if (!this.raidsEnabled) {
      this.raidBosses.set([]);
      return;
    }

    const region = this.regionNumber();
    if (region === null) {
      this.raidBosses.set([]);
      return;
    }
    this.raids.getRaidBosses(region).subscribe({
      next: (bosses) => this.raidBosses.set(bosses),
      error: () => this.raidBosses.set([]),
    });
  }

  refreshRaidBosses(): void {
    this.loadRaidBosses();
  }

  showAreas(): void {
    this.selectedDungeonId = null;
    this.selectedRaidBossId = null;
    this.selectedRegionBoss = false;
    this.regionBossPlaybackActive = false;
  }

  selectedDungeonTitle(): string {
    return (
      this.regionDungeons().find(
        (dungeon) => dungeon.id === this.selectedDungeonId,
      )?.title ?? 'Dungeon'
    );
  }

  selectedDungeonRequiredTowerFloor(): number | null {
    return (
      this.regionDungeons().find(
        (dungeon) => dungeon.id === this.selectedDungeonId,
      )?.requiredTowerFloor ?? null
    );
  }
}
