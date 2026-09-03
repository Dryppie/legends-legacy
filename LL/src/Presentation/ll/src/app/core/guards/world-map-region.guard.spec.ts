import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';
import { firstValueFrom, Observable, of, Subject, throwError } from 'rxjs';
import { CharacterActionsStateService } from '../services/api/character-actions/character-actions.state.service';
import { GameBootstrapStateService } from '../services/api/game-bootstrap/game-bootstrap-state.service';
import { RegionService } from '../services/client-side/region/region.service';
import { CharacterActionDto } from '../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../shared/models/enums/characterActionType';
import {
  getWorldMapRegionId,
  worldMapRegionRedirect,
} from './world-map-region.guard';

describe('worldMapRegionRedirect', () => {
  let currentAction: ReturnType<typeof signal<CharacterActionDto | null>>;
  let bootstrap: jasmine.SpyObj<GameBootstrapStateService>;
  let regions: jasmine.SpyObj<RegionService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    currentAction = signal<CharacterActionDto | null>(null);
    bootstrap = jasmine.createSpyObj<GameBootstrapStateService>(
      'GameBootstrapStateService',
      ['load'],
    );
    bootstrap.load.and.returnValue(of(null));
    regions = jasmine.createSpyObj<RegionService>('RegionService', [
      'getFirstRegionId',
      'getRegionIdByAreaId',
    ]);
    regions.getFirstRegionId.and.returnValue('shenic');
    regions.getRegionIdByAreaId.and.callFake((areaId) =>
      areaId.startsWith('region_02_') ? 'meran' : 'shenic',
    );
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);

    TestBed.configureTestingModule({
      providers: [
        { provide: CharacterActionsStateService, useValue: { currentAction } },
        { provide: GameBootstrapStateService, useValue: bootstrap },
        { provide: RegionService, useValue: regions },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('prefers the area of an active combat action', () => {
    const action = actionSnapshot({
      characterActionType: CharacterActionType.Combat,
      combatActionDetails: {
        characterTeam: [],
        area: {
          id: 'region_02_area_02',
          name: 'Rotgrave Fields',
          levelRequirement: 55,
          creatures: [],
        },
      },
    });

    expect(getWorldMapRegionId(action, regions)).toBe('meran');
  });

  it('defaults to the first region without a known active combat area', () => {
    regions.getRegionIdByAreaId.and.returnValue(null);

    expect(getWorldMapRegionId(null, regions)).toBe('shenic');
    expect(getWorldMapRegionId(actionSnapshot({}), regions)).toBe('shenic');
  });

  it('loads bootstrap state before redirecting to the selected region', async () => {
    const tree = {} as UrlTree;
    const loaded = new Subject<null>();
    bootstrap.load.and.returnValue(loaded);
    router.createUrlTree.and.returnValue(tree);

    const result = TestBed.runInInjectionContext(() =>
      worldMapRegionRedirect({} as ActivatedRouteSnapshot),
    ) as Observable<boolean | UrlTree>;

    const redirect = firstValueFrom(result);
    expect(router.createUrlTree).not.toHaveBeenCalled();
    currentAction.set(
      actionSnapshot({
        characterActionType: CharacterActionType.Combat,
        combatActionDetails: {
          characterTeam: [],
          area: {
            id: 'region_02_area_02',
            name: 'Rotgrave Fields',
            levelRequirement: 55,
            creatures: [],
          },
        },
      }),
    );
    loaded.next(null);
    expect(await redirect).toBe(tree);
    expect(bootstrap.load).toHaveBeenCalledTimes(1);
    expect(router.createUrlTree).toHaveBeenCalledOnceWith([
      '/game/world',
      'meran',
    ]);
  });

  it('ignores a deleted combat action when choosing the region', () => {
    expect(
      getWorldMapRegionId(
        actionSnapshot({
          characterActionType: CharacterActionType.Combat,
          isDeleted: true,
          combatActionDetails: {
            characterTeam: [],
            area: {
              id: 'region_02_area_02',
              name: 'Rotgrave Fields',
              levelRequirement: 55,
              creatures: [],
            },
          },
        }),
        regions,
      ),
    ).toBe('shenic');
  });

  it('falls back to the first region if bootstrap fails without an active combat area', async () => {
    const tree = {} as UrlTree;
    bootstrap.load.and.returnValue(throwError(() => new Error('Unavailable')));
    router.createUrlTree.and.returnValue(tree);
    const result = TestBed.runInInjectionContext(() =>
      worldMapRegionRedirect({} as ActivatedRouteSnapshot),
    ) as Observable<boolean | UrlTree>;

    expect(await firstValueFrom(result)).toBe(tree);
    expect(router.createUrlTree).toHaveBeenCalledOnceWith([
      '/game/world',
      'shenic',
    ]);
  });
});

function actionSnapshot(
  overrides: Partial<CharacterActionDto> = {},
): CharacterActionDto {
  return {
    characterActionType: CharacterActionType.Idle,
    lootTableId: '',
    updatedAt: new Date(),
    revision: 'test',
    isDeleted: false,
    ...overrides,
  };
}
