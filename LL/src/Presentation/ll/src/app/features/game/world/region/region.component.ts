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
import { Region, Area } from '../../../../shared/models/Dtos/regionDto';
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
import { TutorialStateService } from '../../../../core/services/api/tutorial/tutorial-state.service';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import {
  TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE,
  TUTORIAL_TRAINING_GROUNDS_AREA_ID,
} from '../../../../shared/models/tutorial';
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
    private readonly tutorialState: TutorialStateService,
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
      const tutorial = this.tutorialState.state();
      this.applyRegionView();
      if (
        tutorial?.currentStep === TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE &&
        !tutorial.isCompleted
      ) {
        this.applyRegionView();
      }
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

  private withTargetAreaFirst(region: Region): Region {
    if (!this.targetAreaId) {
      return region;
    }

    const areas = [...region.areas];
    const targetIndex = areas.findIndex(
      (area: Area) => area.id === this.targetAreaId,
    );
    if (targetIndex <= 0) {
      return { ...region, areas };
    }

    const [targetArea] = areas.splice(targetIndex, 1);
    return { ...region, areas: [targetArea, ...areas] };
  }

  private applyRegionView(): void {
    if (!this.sourceRegion) {
      return;
    }

    this.region = this.withTargetAreaFirst(
      this.withTutorialAreaAvailability(this.sourceRegion),
    );
  }

  private withTutorialAreaAvailability(region: Region): Region {
    const tutorial = this.tutorialState.state();
    const showTrainingArea =
      tutorial?.currentStep === TUTORIAL_STEP_DEFEAT_TRAINING_CREATURE &&
      !tutorial.isCompleted;

    return {
      ...region,
      areas: region.areas.filter(
        (area) =>
          area.id !== TUTORIAL_TRAINING_GROUNDS_AREA_ID || showTrainingArea,
      ),
    };
  }

  closeTrainingSummary(): void {
    this.combatService.closeCurrentTrainingBattle();
  }

  private dismissTrainingSummaryOutsideTrainingArea(): void {
    if (
      this.targetAreaId !== TUTORIAL_TRAINING_GROUNDS_AREA_ID &&
      this.combatStateService.getIsCombatActive(BattleType.Training)()
    ) {
      this.combatService.stop(BattleType.Training);
    }
  }
}
