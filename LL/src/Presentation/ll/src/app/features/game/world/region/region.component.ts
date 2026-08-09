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
import { Region } from '../../../../shared/models/Dtos/regionDto';
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
      this.dismissTrainingSummaryOutsideTrainingArea();
      this.applyRegionView();
    });
  }

  ngOnDestroy(): void {
    this.combatService.stop(BattleType.Training);
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
      areas: region.areas.filter(
        (area) => this.questState.accessFor(area.id)?.isVisible !== false,
      ),
    };
  }

  closeTrainingSummary(): void {
    this.combatService.closeCurrentTrainingBattle();
  }

  private dismissTrainingSummaryOutsideTrainingArea(): void {
    if (
      this.targetAreaId !== TRAINING_GROUNDS_AREA_ID &&
      this.combatStateService.getIsCombatActive(BattleType.Training)()
    ) {
      this.combatService.stop(BattleType.Training);
    }
  }
}
