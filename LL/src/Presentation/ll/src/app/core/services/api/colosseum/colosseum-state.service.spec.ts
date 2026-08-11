import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ChampionMarket } from '../../../../shared/models/Dtos/colosseum/championMarket';
import { CombatService } from '../../client-side/combat/combat.service';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { NotificationService } from '../../client-side/notifications/notification.service';
import { GameEventService } from '../../real-time/game-event.service';
import { CharacterStateService } from '../character/character-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { ColosseumStateService } from './colosseum-state.service';
import { ColosseumService } from './colosseum.service';

describe('ColosseumStateService', () => {
  it('keeps another weekly purchase available after updating the Glory balance', () => {
    const market: ChampionMarket = {
      glory: 281,
      weeklyResetAt: new Date('2026-08-17T00:00:00Z'),
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
          canPurchase: true,
          cannotPurchaseReason: null,
          sortOrder: 1,
          cindersGranted: 0,
          soulstonesGranted: 0,
          sigilFragmentsGranted: 20,
          rewardItemId: null,
          rewardItemName: null,
          rewardItemQuantity: 0,
        },
      ],
    };
    const colosseumApi = jasmine.createSpyObj<ColosseumService>(
      'ColosseumService',
      ['getChampionMarket', 'purchaseChampionMarketItem'],
    );
    colosseumApi.getChampionMarket.and.returnValue(of(market));
    colosseumApi.purchaseChampionMarketItem.and.returnValue(
      of({
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
      }),
    );

    TestBed.configureTestingModule({
      providers: [
        ColosseumStateService,
        { provide: ColosseumService, useValue: colosseumApi },
        { provide: CombatService, useValue: {} },
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
            currentCharacter: signal(null),
            updateCharacter: jasmine.createSpy('updateCharacter'),
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

    const service = TestBed.inject(ColosseumStateService);
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
