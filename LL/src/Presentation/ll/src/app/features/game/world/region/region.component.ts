import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  computed,
  effect,
  HostListener,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  Area,
  AreaDrop,
  Region,
} from '../../../../shared/models/Dtos/regionDto';
import { RegionService } from '../../../../core/services/client-side/region/region.service';
import { CombatAreaCardComponent } from '../../../../shared/components/combat/combat-area-card/combat-area-card.component';
import { TabsComponent } from '../../../../shared/components/custom-components/tabs/tabs.component';
import { TabComponent } from '../../../../shared/components/custom-components/tabs/tab/tab.component';
import { RaidsComponent } from './raids/raids.component';
import { DungeonsComponent } from './dungeons/dungeons.component';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../core/state/combat-state/combatState';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { TRAINING_GROUNDS_AREA_ID } from '../../../../shared/models/quest';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';

@Component({
  selector: 'app-region',
  imports: [
    NgIf,
    NgFor,
    CombatAreaCardComponent,
    TabsComponent,
    TabComponent,
    RaidsComponent,
    DungeonsComponent,
    CombatComponent,
    RouterLink,
    DefaultHeaderComponent,
  ],
  templateUrl: './region.component.html',
})
export class RegionComponent implements OnInit, OnDestroy {
  regionId!: string;
  region!: Region; // You can define a more specific type based on your item data structure
  private sourceRegion: Region | null = null;
  targetAreaId: string | null = null;
  readonly trainingBattleType = BattleType.Training;
  readonly activeBattle;

  constructor(
    private route: ActivatedRoute,
    private regionService: RegionService,
    public readonly combatStateService: CombatStateService,
    private readonly combatService: CombatService,
    private readonly questState: QuestStateService,
    private readonly dungeonState: DungeonStateService,
    characterActions: CharacterActionsStateService,
  ) {
    this.activeBattle = computed(() => {
      const action = characterActions.currentAction();
      if (action?.characterActionType !== CharacterActionType.Combat) {
        return null;
      }

      return {
        areaName: action.combatActionDetails?.area?.name ?? 'Current encounter',
      };
    });

    effect(() => {
      this.questState.areaAccess();
      this.dungeonState.dungeons();
      this.applyRegionView();
    });
  }

  ngOnInit(): void {
    this.setColumnCount();

    this.route.paramMap.subscribe((params) => {
      this.regionId = params.get('id') ?? '';
      this.getRegionDetails(this.regionId);
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
    this.closeTrainingSummary();
  }

  getRegionDetails(id: string) {
    this.regionService.getRegionById(id).subscribe((data: any) => {
      this.sourceRegion = data as Region;
      this.applyRegionView();
    });
  }

  columnCount = 1;
  @HostListener('window:resize')
  onResize() {
    this.setColumnCount();
  }

  private setColumnCount() {
    const width = window.innerWidth;

    if (width >= 1280)
      this.columnCount = 4; // xl
    else if (width >= 1024)
      this.columnCount = 3; // lg
    else if (width >= 768)
      this.columnCount = 2; // md
    else this.columnCount = 0; // base
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
          (area) => this.questState.accessFor(area.id)?.isVisible !== false,
        )
        .map((area) => ({
          ...area,
          possibleDrops: this.possibleDropsFor(area),
        })),
    };
  }

  private possibleDropsFor(area: Area): AreaDrop[] {
    const region = this.areaRegion(area.id);
    if (region === null) {
      return [];
    }

    const drops = new Map<string, AreaDrop>();
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
}
