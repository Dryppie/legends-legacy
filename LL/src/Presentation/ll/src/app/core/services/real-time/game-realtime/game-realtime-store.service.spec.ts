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

    store.addLoot(
      [reward],
      '2026-08-11T12:00:00Z',
      'guild-shop',
      null,
      'grant-id',
    );
    store.addLoot(
      [reward],
      '2026-08-11T12:00:01Z',
      'guild-shop',
      null,
      'grant-id',
    );

    expect(store.recentLoot().length).toBe(1);
  });

  it('does not append a live copy after the authoritative history entry arrives first', () => {
    TestBed.configureTestingModule({
      providers: [
        GameRealtimeStore,
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });
    const store = TestBed.inject(GameRealtimeStore);
    const reward = item('reward');
    store.setLootHistory([
      {
        id: 'persisted-entry',
        item: reward,
        receivedAt: '2026-08-26T00:12:00Z',
        source: 'container-reward',
        location: 'Catalyst Selection Cache',
      },
    ]);

    store.addLoot(
      [reward],
      '2026-08-26T00:12:01Z',
      'container-reward',
      'Catalyst Selection Cache',
      'grant-id',
    );

    expect(store.recentLoot().map((entry) => entry.id)).toEqual([
      'persisted-entry',
    ]);
  });

  it('keeps a distinct live reward that follows an authoritative entry', () => {
    TestBed.configureTestingModule({
      providers: [
        GameRealtimeStore,
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });
    const store = TestBed.inject(GameRealtimeStore);
    store.setLootHistory([
      {
        id: 'persisted-entry',
        item: item('first-reward'),
        receivedAt: '2026-08-26T00:12:00Z',
        source: 'container-reward',
        location: 'Catalyst Selection Cache',
      },
    ]);

    store.addLoot(
      [item('second-reward')],
      '2026-08-26T00:12:01Z',
      'container-reward',
      'Catalyst Selection Cache',
      'grant-id',
    );

    expect(store.recentLoot().length).toBe(2);
    expect(store.recentLoot()[0].item.itemInstance.id).toBe(
      'second-reward-instance',
    );
  });

  it('does not treat an older matching history entry as the current live reward', () => {
    TestBed.configureTestingModule({
      providers: [
        GameRealtimeStore,
        { provide: EventBusService, useValue: { logout: signal(false) } },
      ],
    });
    const store = TestBed.inject(GameRealtimeStore);
    const reward = item('reward');
    store.setLootHistory([
      {
        id: 'older-entry',
        item: reward,
        receivedAt: '2026-08-26T00:10:00Z',
        source: 'container-reward',
        location: 'Catalyst Selection Cache',
      },
    ]);

    store.addLoot(
      [reward],
      '2026-08-26T00:12:00Z',
      'container-reward',
      'Catalyst Selection Cache',
      'new-grant-id',
    );

    expect(store.recentLoot().length).toBe(2);
    expect(store.recentLoot()[0].id).toContain('live:');
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
