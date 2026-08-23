import { SessionSummaryService } from '../../../core/services/client-side/session-summary/session-summary.service';
import {
  BattleOutcome,
  CombatResultDto,
  CombatSessionDto,
} from '../../models/Dtos/combatResultDto';
import { GatheringType } from '../../models/enums/gatheringType';
import { ItemType } from '../../models/enums/itemType';
import { Rarity } from '../../models/enums/rarity';
import { InventoryItem } from '../../models/inventoryItem';
import { SessionSummaryPopupComponent } from './session-summary-popup.component';

describe('SessionSummaryPopupComponent', () => {
  it('presents Gathering XP, ordinary materials, and rare Catalysts separately', () => {
    const component = new SessionSummaryPopupComponent(
      new SessionSummaryService(),
    );
    const sections = component.rewardSections(combatSession());
    const gathering = sections.find((section) => section.key === 'gathering');

    expect(gathering).toBeDefined();
    expect(gathering?.metrics).toEqual([{ label: 'Mining XP', value: 125 }]);
    expect(
      gathering?.items.map((item) => item.itemInstance.itemBase.name),
    ).toEqual(['Fury Heart', 'Ore']);
    expect(
      gathering?.items.find((item) => item.key === 'fury_heart')?.isRare,
    ).toBeTrue();
    expect(
      sections.find((section) => section.key === 'crafting'),
    ).toBeUndefined();
  });
});

function combatSession(): CombatSessionDto {
  return {
    from: new Date('2026-08-22T08:00:00Z'),
    to: new Date('2026-08-22T09:00:00Z'),
    combatResult: {
      outcome: BattleOutcome.Victory,
      gatheringRewards: [
        {
          toolType: GatheringType.Mining,
          nodeId: 'ore',
          nodeName: 'Ore',
          toolName: 'Pickaxe',
          toolRarity: Rarity.Rare,
          success: true,
          experienceGained: 125,
          itemsGained: [
            item('ore', 'Ore', Rarity.Common, 20),
            item('fury_heart', 'Fury Heart', Rarity.Rare, 1),
          ],
          appliedBonusEffects: ['+25% gathering XP'],
        },
      ],
    } as unknown as CombatResultDto,
    combatSummary: {
      totalBattles: 2,
      wins: 2,
      losses: 0,
      draws: 0,
      totalExperience: 40,
      totalGold: 0,
      totalCinders: 0,
      totalSoulstones: 0,
      rewardBreakdown: {
        powerItems: [],
        craftingItems: [],
        essenceItems: [],
        dungeonAccessItems: [],
      },
    },
  };
}

function item(
  id: string,
  name: string,
  rarity: Rarity,
  quantity: number,
): InventoryItem {
  return {
    id: `inventory-${id}`,
    quantity,
    itemInstance: {
      id: `instance-${id}`,
      itemBase: {
        id,
        name,
        rarity,
        itemType: ItemType.Resource,
        description: '',
        stackable: true,
      },
    },
  };
}
