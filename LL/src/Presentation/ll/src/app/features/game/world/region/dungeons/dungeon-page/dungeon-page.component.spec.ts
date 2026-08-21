import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import {
  ClaimDungeonRewardsResponse,
  DungeonRun,
  DungeonRunStatus,
  RoomInstanceStatus,
  RoomType,
} from '../../../../../../core/services/api/dungeon/dungeon.service';
import { DungeonStateService } from '../../../../../../core/services/api/dungeon/dungeon-state.service';
import { CombatStateService } from '../../../../../../core/state/combat-state/combat-state.service';
import { InventoryItem } from '../../../../../../shared/models/inventoryItem';
import { DungeonPageComponent } from './dungeon-page.component';

describe('DungeonPageComponent', () => {
  let dungeonState: jasmine.SpyObj<DungeonStateService>;
  let router: jasmine.SpyObj<Router>;
  const activeDungeon = signal<DungeonRun | null>(null);

  beforeEach(() => {
    activeDungeon.set(createRestSiteRun());
    dungeonState = jasmine.createSpyObj<DungeonStateService>(
      'DungeonStateService',
      ['chooseRoute', 'fight', 'restAtSite', 'retreat', 'claimDungeonRewards'],
      {
        activeDungeon: activeDungeon.asReadonly(),
        loading: signal(false).asReadonly(),
        error: signal<string | null>(null).asReadonly(),
        message: signal<string | null>(null).asReadonly(),
      },
    );
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    router.navigate.and.resolveTo(true);

    TestBed.configureTestingModule({
      providers: [
        { provide: DungeonStateService, useValue: dungeonState },
        { provide: CombatStateService, useValue: {} },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('rests when the current Rest Site node is clicked', () => {
    const component = TestBed.runInInjectionContext(
      () => new DungeonPageComponent(),
    );
    const node = component.graphNodes()[0];

    expect(component.isMapNodeActionable(node)).toBeTrue();
    expect(component.mapNodeAriaLabel(node)).toBe(
      'Rest at Upper Rest Site and recover 15 Vigor',
    );
    expect(component.mapNodeTitle(node)).toBe(
      'Upper Rest Site · Rest · +15 Vigor',
    );

    component.chooseMapNode(node);

    expect(dungeonState.restAtSite).toHaveBeenCalledOnceWith();
    expect(dungeonState.chooseRoute).not.toHaveBeenCalled();
  });

  for (const roomType of [RoomType.Combat, RoomType.MiniBoss, RoomType.Boss]) {
    it(`begins combat when the current ${roomType} node is clicked`, () => {
      activeDungeon.set(createRun(roomType));
      const component = TestBed.runInInjectionContext(
        () => new DungeonPageComponent(),
      );
      const node = component.graphNodes()[0];

      expect(component.isMapNodeActionable(node)).toBeTrue();
      expect(component.currentRoomUsesDirectNodeAction()).toBeTrue();
      expect(component.mapNodeAriaLabel(node)).toBe(
        `Begin combat at ${node.displayName}`,
      );
      expect(component.mapNodeTitle(node)).toBe('Begin Combat');

      component.chooseMapNode(node);

      expect(dungeonState.fight).toHaveBeenCalledOnceWith();
      expect(dungeonState.chooseRoute).not.toHaveBeenCalled();
      expect(dungeonState.restAtSite).not.toHaveBeenCalled();
    });
  }

  for (const roomType of [RoomType.Combat, RoomType.MiniBoss]) {
    it(`shows a Vigor forecast for every ${roomType} node`, () => {
      const run = createRun(roomType);
      run.state.mapNodes[0].vigorCostMin = 12;
      run.state.mapNodes[0].vigorCostMax = 22;
      activeDungeon.set(run);
      const component = TestBed.runInInjectionContext(
        () => new DungeonPageComponent(),
      );

      expect(component.mapNodeVigorForecast(component.graphNodes()[0])).toEqual(
        {
          minimum: 10,
          maximum: 19,
        },
      );
    });
  }

  it('does not show a Vigor forecast for Boss nodes', () => {
    const run = createRun(RoomType.Boss);
    run.state.mapNodes[0].vigorCostMin = 20;
    run.state.mapNodes[0].vigorCostMax = 30;
    activeDungeon.set(run);
    const component = TestBed.runInInjectionContext(
      () => new DungeonPageComponent(),
    );

    expect(
      component.mapNodeVigorForecast(component.graphNodes()[0]),
    ).toBeNull();
  });

  it('widens non-route Vigor forecasts when the expedition is fatigued', () => {
    const run = createRun(RoomType.Combat);
    run.state.vigorState = 'Strained';
    run.state.mapNodes[0].vigorCostMin = 12;
    run.state.mapNodes[0].vigorCostMax = 22;
    activeDungeon.set(run);
    const component = TestBed.runInInjectionContext(
      () => new DungeonPageComponent(),
    );

    expect(component.mapNodeVigorForecast(component.graphNodes()[0])).toEqual({
      minimum: 8,
      maximum: 21,
    });
  });

  it('does not make a completed Rest Site actionable', () => {
    const run = createRestSiteRun();
    run.rooms[0].status = RoomInstanceStatus.Completed;
    activeDungeon.set(run);
    const component = TestBed.runInInjectionContext(
      () => new DungeonPageComponent(),
    );
    const node = component.graphNodes()[0];

    expect(component.isMapNodeActionable(node)).toBeFalse();
    expect(component.currentRoomUsesDirectNodeAction()).toBeFalse();

    component.chooseMapNode(node);

    expect(dungeonState.restAtSite).not.toHaveBeenCalled();
    expect(dungeonState.fight).not.toHaveBeenCalled();
  });

  it('masks the depleted portion of the fixed Vigor gradient', () => {
    const run = createRestSiteRun();
    run.state.vigor = 51;
    activeDungeon.set(run);
    const component = TestBed.runInInjectionContext(
      () => new DungeonPageComponent(),
    );

    expect(component.vigorPercent()).toBe(51);
    expect(component.vigorDepletedPercent()).toBe(49);
    expect(component.vigorGradientClipPath()).toBe('inset(0 49% 0 0)');
  });

  it('retreats and secures loot from the sidebar action', () => {
    const component = TestBed.runInInjectionContext(
      () => new DungeonPageComponent(),
    );

    expect(component.canRetreat()).toBeTrue();

    component.retreatAndSecureLoot();

    expect(dungeonState.retreat).toHaveBeenCalledOnceWith();
  });

  it('shows the claimed rewards before returning to the world', () => {
    const run = createRun(RoomType.Boss);
    run.status = DungeonRunStatus.Completed;
    run.rooms[0].status = RoomInstanceStatus.Completed;
    run.pendingCinders = 125;
    run.pendingExperience = 2400;
    run.pendingSoulstones = 3;
    activeDungeon.set(run);

    const claimedItem = {
      id: 'inventory-item',
      quantity: 2,
      itemInstance: {
        id: 'item-instance',
        displayName: 'Runed Goblin Blade',
        itemBase: {
          name: 'Goblin Blade',
          itemType: 'Equipment',
        },
      },
    } as InventoryItem;
    const response = {
      activeRun: null,
      hub: {
        sigilFragments: 0,
        sigilAssemblyEnabled: false,
        sigilAssemblyCost: 0,
        dungeons: [],
      },
      inventoryItems: [claimedItem],
      claimedLoot: [claimedItem],
      character: {},
    } as unknown as ClaimDungeonRewardsResponse;

    dungeonState.claimDungeonRewards.and.callFake((onSuccess) => {
      activeDungeon.set(null);
      onSuccess?.(response);
    });

    const component = TestBed.runInInjectionContext(
      () => new DungeonPageComponent(),
    );

    component.claimDungeonRewards();

    expect(component.claimedRewardResult()).toEqual({
      run,
      claimedLoot: [claimedItem],
    });
    expect(component.claimedCurrencyRewards()).toEqual([
      { label: 'Cinders', value: 125 },
      { label: 'Experience', value: 2400 },
      { label: 'Soulstones', value: 3 },
    ]);
    expect(component.rewardResultTitle()).toBe('The dungeon spoils are yours');
    expect(router.navigate).not.toHaveBeenCalled();

    component.returnToWorldAfterClaim();

    expect(component.claimedRewardResult()).toBeNull();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/game/world/shenic']);
  });
});

function createRestSiteRun(): DungeonRun {
  return createRun(RoomType.RestSite);
}

function createRun(roomType: RoomType): DungeonRun {
  const isRestSite = roomType === RoomType.RestSite;

  return {
    id: 'run',
    characterId: 'character',
    dungeonDefinitionId: 'goblin_mines_Normal',
    dungeonDefinitionName: 'Goblin Mines I',
    seed: 1,
    status: DungeonRunStatus.Active,
    currentRoomIndex: 3,
    rooms: [
      {
        id: 'current-room',
        index: 3,
        type: roomType,
        status: RoomInstanceStatus.Pending,
        encounterIds: [],
      },
    ],
    pendingExperience: 0,
    pendingCinders: 0,
    pendingSoulstones: 0,
    pendingRewards: [],
    state: {
      securedLoot: {
        experience: 0,
        cinders: 0,
        soulstones: 0,
        items: {},
      },
      pendingLoot: {
        experience: 0,
        cinders: 0,
        soulstones: 0,
        items: {},
      },
      mapNodes: [
        {
          id: isRestSite ? 'rest-site' : 'combat-room',
          displayName: isRestSite ? 'Upper Rest Site' : 'Throne Tunnel',
          roomIndex: 3,
          depth: 3,
          lane: 0,
          section: 1,
          forecast: isRestSite
            ? 'Recover 15 Vigor.'
            : 'Defeat the enemies here.',
          vigorCostMin: 0,
          vigorCostMax: 0,
          nextRoomIndexes: [4],
        },
      ],
      traversedRoomIndexes: [3],
      currentRouteOptions: [],
      masteryAwardReasons: [],
      vigor: 68,
      vigorState: 'Steady',
      vigorThresholds: [],
      currentSection: 1,
      totalSections: 3,
      restSitesVisited: 0,
      lastConsequence: '',
      expiresAt: new Date().toISOString(),
      vigorHistory: [],
    },
    createdAt: new Date().toISOString(),
  };
}
