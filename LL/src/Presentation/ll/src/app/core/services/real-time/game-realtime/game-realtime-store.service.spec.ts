import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { GameRealtimeStore } from './game-realtime-store.service';

describe('GameRealtimeStore', () => {
  it('records a retried inventory grant only once in loot history', () => {
    TestBed.configureTestingModule({
      providers: [
        GameRealtimeStore,
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });
    const store = TestBed.inject(GameRealtimeStore);
    const reward = item('reward');

    store.addLoot([reward], '2026-08-11T12:00:00Z', 'guild-shop', null, 'grant-id');
    store.addLoot([reward], '2026-08-11T12:00:01Z', 'guild-shop', null, 'grant-id');

    expect(store.recentLoot().length).toBe(1);
  });
});

function item(id: string): InventoryItem {
  return {
    id,
    quantity: 1,
    itemInstance: {
      id: `${id}-instance`,
      itemBase: {
        id: `${id}-base`,
        name: id,
        description: '',
        rarity: 'Common' as never,
        itemType: ItemType.Resource,
        stackable: true,
      },
    },
  };
}
