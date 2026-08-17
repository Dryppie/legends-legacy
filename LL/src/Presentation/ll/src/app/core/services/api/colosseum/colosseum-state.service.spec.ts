import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import {
  ChampionMarket,
  ChampionMarketItem,
  ChampionMarketPurchaseResponse,
} from '../../../../shared/models/Dtos/colosseum/championMarket';
import { StartArenaBattleResponse } from '../../../../shared/models/Dtos/colosseum/startArenaBattleResponse';
import { CombatService } from '../../client-side/combat/combat.service';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { NotificationService } from '../../client-side/notifications/notification.service';
import { GameEventService } from '../../real-time/game-event.service';
import { CharacterStateService } from '../character/character-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { ColosseumStateService } from './colosseum-state.service';
import { ColosseumService } from './colosseum.service';

describe('ColosseumStateService', () => {
  it('derives market affordability immediately after earning Glory', () => {
    const market = createMarket(129);
    const response = createBattleResponse(20, 1000);
    const { service, colosseumApi } = setupStateService({ market, response });
    service.loadChampionMarket();
    expect(service.championMarket()?.items[0].cannotPurchaseReason).toBe(
      'Not enough Glory',
    );
    spyOn(service, 'loadStatus');
    spyOn(service, 'loadArenaOpponents');
    spyOn(service, 'loadColosseumRankings');
    spyOn(service, 'loadColosseumMatchResults');

    service.startArenaBattle('opponent-id');

    const updatedMarket = service.championMarket();
    expect(updatedMarket?.glory).toBe(149);
    expect(updatedMarket?.items[0].canPurchase).toBeTrue();
    expect(updatedMarket?.items[0].cannotPurchaseReason).toBeNull();
    expect(colosseumApi.getChampionMarket).toHaveBeenCalledTimes(1);
  });

  it('reacts to battle rating changes while preserving rank requirements', () => {
    const market = createMarket(149, {
      requiredRankTier: 'gold',
      requiredRankMinRating: 1250,
    });
    const response = createBattleResponse(0, 1250);
    const { service, colosseumApi } = setupStateService({
      market,
      response,
      arenaRating: 1249,
    });
    service.loadChampionMarket();
    expect(service.championMarket()?.items[0].cannotPurchaseReason).toBe(
      'Requires Gold',
    );
    spyOn(service, 'loadStatus');
    spyOn(service, 'loadArenaOpponents');
    spyOn(service, 'loadColosseumRankings');
    spyOn(service, 'loadColosseumMatchResults');

    service.startArenaBattle('opponent-id');

    expect(service.championMarket()?.items[0].canPurchase).toBeTrue();
    expect(service.championMarket()?.items[0].cannotPurchaseReason).toBeNull();
    expect(colosseumApi.getChampionMarket).toHaveBeenCalledTimes(1);
  });

  it('keeps another weekly purchase available after updating the Glory balance', () => {
    const market = createMarket(281, { weeklyPurchaseLimit: 2 });
    const purchaseResponse: ChampionMarketPurchaseResponse = {
      itemId: 'sigil-fragments',
      quantity: 1,
      glorySpent: 140,
      gloryRemaining: 141,
      cindersGranted: 0,
      soulstonesGranted: 0,
      sigilFragmentsGranted: 20,
      rewardItemId: null,
      rewardItemName: null,
      rewardItemQuantity: 0,
      inventoryGrantId: null,
      inventoryItemsGranted: [],
    };
    const { service, colosseumApi } = setupStateService({
      market,
      purchaseResponse,
    });
    service.loadChampionMarket();

    service.purchaseChampionMarketItem('sigil-fragments');

    const updatedMarket = service.championMarket();
    const updatedItem = updatedMarket?.items[0];
    expect(updatedMarket?.glory).toBe(141);
    expect(updatedItem?.remainingWeeklyPurchases).toBe(1);
    expect(updatedItem?.canPurchase).toBeTrue();
    expect(updatedItem?.cannotPurchaseReason).toBeNull();
    expect(colosseumApi.getChampionMarket).toHaveBeenCalledTimes(1);
  });
});

function createMarket(
  glory: number,
  overrides: Partial<ChampionMarketItem> = {},
): ChampionMarket {
  return {
    glory,
    weeklyResetAt: new Date('2026-08-24T00:00:00Z'),
    items: [
      {
        id: 'sigil-fragments',
        name: 'Sigil Fragments',
        description: 'Fragments.',
        category: 'Weekly Cache',
        gloryCost: 140,
        weeklyPurchaseLimit: 2,
        lifetimePurchaseLimit: null,
        remainingWeeklyPurchases: 2,
        remainingLifetimePurchases: 2_147_483_647,
        requiredRating: null,
        requiredRankTier: null,
        requiredRankMinRating: null,
        sortOrder: 1,
        cindersGranted: 0,
        soulstonesGranted: 0,
        sigilFragmentsGranted: 20,
        rewardItemId: null,
        rewardItemName: null,
        rewardItemQuantity: 0,
        ...overrides,
      },
    ],
  };
}

function createBattleResponse(
  gloryEarned: number,
  ratingAfter: number,
): StartArenaBattleResponse {
  return {
    arenaTicketStatus: {},
    attackerRating: { ratingAfter },
    rewards: { gloryEarned },
    battle: {},
  } as StartArenaBattleResponse;
}

function setupStateService(options: {
  market: ChampionMarket;
  response?: StartArenaBattleResponse;
  purchaseResponse?: ChampionMarketPurchaseResponse;
  arenaRating?: number;
}): {
  service: ColosseumStateService;
  colosseumApi: jasmine.SpyObj<ColosseumService>;
} {
  const colosseumApi = jasmine.createSpyObj<ColosseumService>(
    'ColosseumService',
    ['getChampionMarket', 'startArenaBattle', 'purchaseChampionMarketItem'],
  );
  colosseumApi.getChampionMarket.and.returnValue(of(options.market));
  if (options.response) {
    colosseumApi.startArenaBattle.and.returnValue(of(options.response));
  }
  if (options.purchaseResponse) {
    colosseumApi.purchaseChampionMarketItem.and.returnValue(
      of(options.purchaseResponse),
    );
  }
  const combatService = jasmine.createSpyObj<CombatService>('CombatService', [
    'startColosseumMatchSimulation',
  ]);
  const currentCharacter = signal(
    options.arenaRating == null
      ? null
      : ({ arenaRating: options.arenaRating } as ReturnType<
          CharacterStateService['currentCharacter']
        >),
  );
  const updateCharacter = jasmine
    .createSpy('updateCharacter')
    .and.callFake((character) => currentCharacter.set(character));

  TestBed.configureTestingModule({
    providers: [
      ColosseumStateService,
      { provide: ColosseumService, useValue: colosseumApi },
      { provide: CombatService, useValue: combatService },
      {
        provide: GameEventService,
        useValue: {
          reconnectCount: signal(0),
          eventEnvelope: { ArenaBattleCompletedMsg: signal(null) },
        },
      },
      {
        provide: CharacterStateService,
        useValue: {
          currentCharacterId: signal(null),
          currentCharacter,
          updateCharacter,
        },
      },
      {
        provide: NotificationService,
        useValue: {
          count: jasmine.createSpy('count').and.returnValue(0),
        },
      },
      {
        provide: ToastService,
        useValue: { showToast: jasmine.createSpy('showToast') },
      },
      {
        provide: InventoryStateService,
        useValue: {
          applyInventoryGrant: jasmine.createSpy('applyInventoryGrant'),
        },
      },
    ],
  });

  return {
    service: TestBed.inject(ColosseumStateService),
    colosseumApi,
  };
}
