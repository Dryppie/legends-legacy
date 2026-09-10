import { NgFor, NgIf } from '@angular/common';
import { NO_ERRORS_SCHEMA, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  provideRouter,
  RouterLink,
} from '@angular/router';
import { BehaviorSubject, of, Subject } from 'rxjs';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import { EssencesService } from '../../../../core/services/api/essences/essences.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { RaidService } from '../../../../core/services/api/raid/raid.service';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import { RegionService } from '../../../../core/services/client-side/region/region.service';
import { StateSyncCoordinator } from '../../../../core/services/real-time/game-realtime/state-sync-coordinator.service';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { DungeonPreviewData } from '../../../../shared/models/Dtos/dungeons/dungeonPreviewData';
import { RegionComponent } from './region.component';

describe('RegionComponent dungeon availability', () => {
  let fixture: ComponentFixture<RegionComponent>;
  let loadAvailableDungeons: jasmine.Spy;
  let refreshResponse: Subject<DungeonPreviewData[]>;
  let routeParams: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  const dungeon = (ownedAmount: number): DungeonPreviewData => ({
    id: 'goblin_mines',
    familyId: 'goblin_mines',
    region: 1,
    number: 1,
    title: 'Goblin Mines',
    lore: '',
    rewards: [],
    unlockedDifficulties: [],
    sigilItemId: 'sigil_goblin_mines',
    canEnter: ownedAmount > 0,
    entryRequirements: [
      {
        itemId: 'sigil_goblin_mines',
        name: 'Goblin Sigil',
        requiredAmount: 1,
        ownedAmount,
      },
    ],
  });

  beforeEach(async () => {
    const dungeons = signal([dungeon(0)]);
    refreshResponse = new Subject<DungeonPreviewData[]>();
    routeParams = new BehaviorSubject(convertToParamMap({ id: 'shenic' }));
    loadAvailableDungeons = jasmine
      .createSpy('loadAvailableDungeons')
      .and.callFake(() =>
        refreshResponse.subscribe((previews) => dungeons.set(previews)),
      );

    await TestBed.configureTestingModule({
      imports: [RegionComponent],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: routeParams,
            queryParamMap: of(convertToParamMap({})),
          },
        },
        {
          provide: DungeonStateService,
          useValue: {
            dungeons: dungeons.asReadonly(),
            activeDungeon: signal(null),
            loadAvailableDungeons,
          },
        },
        {
          provide: RegionService,
          useValue: { getRegionById: () => of({ name: 'Shenic', areas: [] }) },
        },
        {
          provide: CombatStateService,
          useValue: { getIsCombatActive: () => signal(false) },
        },
        {
          provide: CombatService,
          useValue: { closeCurrentTrainingBattle: () => undefined },
        },
        {
          provide: QuestStateService,
          useValue: {
            areaAccess: signal([]),
            accessFor: () => undefined,
            loadAreaAccess: () => undefined,
          },
        },
        { provide: RaidService, useValue: { getRaidBosses: () => of([]) } },
        {
          provide: StateSyncCoordinator,
          useValue: {
            register: () => () => undefined,
            activate: () => undefined,
          },
        },
        { provide: EssencesService, useValue: { getArchive: () => of(null) } },
        {
          provide: CharacterStateService,
          useValue: { currentCharacter: signal({ level: 1 }) },
        },
        {
          provide: CharacterActionsStateService,
          useValue: { currentAction: signal(null) },
        },
      ],
    })
      .overrideComponent(RegionComponent, {
        set: {
          imports: [NgFor, NgIf, RouterLink],
          schemas: [NO_ERRORS_SCHEMA],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(RegionComponent);
  });

  afterEach(() => {
    refreshResponse.complete();
    routeParams.complete();
  });

  it('refreshes the map on entry and displays the looted Sigil without selecting a dungeon', () => {
    fixture.detectChanges();
    expect(loadAvailableDungeons).toHaveBeenCalledOnceWith();
    const row = fixture.nativeElement.querySelector(
      '[data-tour="dungeons-introduction"] .activity-row',
    ) as HTMLButtonElement;
    expect(row.textContent).toContain('0 sigils');
    expect(row.classList.contains('locked')).toBeTrue();

    refreshResponse.next([dungeon(1)]);
    fixture.detectChanges();

    expect(row.textContent).toContain('1 sigil');
    expect(row.classList.contains('locked')).toBeFalse();
    expect(fixture.componentInstance.selectedDungeonId).toBeNull();
    expect(fixture.nativeElement.querySelector('app-dungeons')).toBeNull();
  });

  it('refreshes availability when navigating between regions in the same map component', () => {
    fixture.detectChanges();
    loadAvailableDungeons.calls.reset();

    routeParams.next(convertToParamMap({ id: 'meran' }));
    fixture.detectChanges();

    expect(loadAvailableDungeons).toHaveBeenCalledOnceWith();
  });
});
