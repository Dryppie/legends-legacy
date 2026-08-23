import { SessionSummaryService } from './session-summary.service';
import {
  BattleOutcome,
  CombatResultDto,
  CombatSessionDto,
} from '../../../../shared/models/Dtos/combatResultDto';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { GatheringType } from '../../../../shared/models/enums/gatheringType';
import { Rarity } from '../../../../shared/models/enums/rarity';

describe('SessionSummaryService', () => {
  it('holds chunk summaries until catch-up completes and then shows their total', () => {
    const service = new SessionSummaryService();

    service.loadCombatSince(
      session('2026-08-11T00:00:00Z', '2026-08-11T00:16:40Z', 100, 400, 2),
      true,
    );
    expect(service.combatSession()).toBeNull();

    const completedSession = service.loadCombatSince(
      session('2026-08-11T00:16:40Z', '2026-08-11T00:26:40Z', 60, 240, 3),
      false,
    );

    const summary = service.combatSession();
    expect(summary).toBe(completedSession);
    expect(summary?.combatSummary.totalBattles).toBe(160);
    expect(summary?.combatSummary.totalExperience).toBe(640);
    expect(summary?.combatSummary.rewardBreakdown?.powerItems[0].quantity).toBe(
      5,
    );
    expect(summary?.combatResult.gatheringRewards[0].experienceGained).toBe(
      8_000,
    );
    expect(
      summary?.combatResult.gatheringRewards[0].itemsGained[0].quantity,
    ).toBe(5);
    expect(new Date(summary!.from).toISOString()).toBe(
      '2026-08-11T00:00:00.000Z',
    );
    expect(new Date(summary!.to).toISOString()).toBe(
      '2026-08-11T00:26:40.000Z',
    );
  });

  it('keeps the last completed battle when the final response contains no encounter', () => {
    const service = new SessionSummaryService();
    const completedBattle = session(
      '2026-08-11T00:00:00Z',
      '2026-08-11T00:16:40Z',
      100,
      400,
      2,
    );

    service.loadCombatSince(completedBattle, true);
    const completedSession = service.loadCombatSince(
      session('2026-08-11T00:16:40Z', '2026-08-11T00:16:40Z', 0, 0, 0),
      false,
    );

    expect(completedSession?.combatResult.startedAt).toBe(
      completedBattle.combatResult.startedAt,
    );
    expect(completedSession?.combatSummary.totalBattles).toBe(100);
  });
});

function session(
  from: string,
  to: string,
  battles: number,
  experience: number,
  itemQuantity: number,
): CombatSessionDto {
  return {
    from: new Date(from),
    to: new Date(to),
    combatResult: {
      startedAt: new Date(to),
      outcome: BattleOutcome.Victory,
      gatheringRewards:
        battles > 0
          ? [
              {
                toolType: GatheringType.Mining,
                nodeId: 'ore',
                nodeName: 'Ore',
                toolName: 'Pickaxe',
                toolRarity: Rarity.Common,
                success: itemQuantity > 0,
                experienceGained: battles * 50,
                itemsGained:
                  itemQuantity > 0 ? [gatheringItem(itemQuantity)] : [],
                appliedBonusEffects: [],
              },
            ]
          : [],
    } as unknown as CombatResultDto,
    combatSummary: {
      totalBattles: battles,
      wins: battles,
      losses: 0,
      draws: 0,
      totalExperience: experience,
      totalGold: 0,
      totalCinders: 0,
      totalSoulstones: 0,
      rewardBreakdown: {
        powerItems: [item(itemQuantity)],
        craftingItems: [],
        essenceItems: [],
        dungeonAccessItems: [],
      },
    },
  };
}

function gatheringItem(quantity: number): InventoryItem {
  const result = item(quantity);
  result.id = 'gathering-item';
  result.itemInstance = {
    ...result.itemInstance,
    id: 'gathering-item-instance',
    itemBase: {
      ...result.itemInstance.itemBase,
      id: 'ore',
      name: 'Ore',
    },
  };
  return result;
}

function item(quantity: number): InventoryItem {
  return {
    id: 'inventory-item',
    quantity,
    itemInstance: {
      id: 'item-instance',
      itemBase: {
        id: 'power-item',
        name: 'Power Item',
      },
    },
  } as InventoryItem;
}
