import { SessionSummaryService } from '../../../core/services/client-side/session-summary/session-summary.service';
import {
  BattleOutcome,
  CombatResultDto,
  CombatSessionDto,
} from '../../models/Dtos/combatResultDto';
import { ItemType } from '../../models/enums/itemType';
import { Rarity } from '../../models/enums/rarity';
import { InventoryItem } from '../../models/inventoryItem';
import { SessionSummaryPopupComponent } from './session-summary-popup.component';

describe('SessionSummaryPopupComponent', () => {
  it('presents combat drops in their current reward buckets', () => {
    const component = new SessionSummaryPopupComponent(
      new SessionSummaryService(),
    );

    const sections = component.rewardSections(combatSession());

    expect(sections.map((section) => section.key)).toEqual([
      'power',
      'miscellaneous',
      'essence',
      'dungeon-access',
      'currencies',
    ]);
    expect(
      sections
        .find((section) => section.key === 'miscellaneous')
        ?.items.map((entry) => entry.itemInstance.itemBase.name),
    ).toEqual(['Catalyst']);
  });
});

function combatSession(): CombatSessionDto {
  return {
    from: new Date('2026-08-22T08:00:00Z'),
    to: new Date('2026-08-22T09:00:00Z'),
    combatResult: {
      outcome: BattleOutcome.Victory,
    } as unknown as CombatResultDto,
    combatSummary: {
      totalBattles: 2,
      wins: 2,
      losses: 0,
      draws: 0,
      totalExperience: 40,
      totalGold: 0,
      totalCinders: 3,
      totalSoulstones: 1,
      rewardBreakdown: {
        powerItems: [item('weapon', 'Weapon')],
        miscellaneousItems: [item('catalyst', 'Catalyst')],
        essenceItems: [item('essence', 'Essence')],
        dungeonAccessItems: [item('sigil', 'Sigil')],
      },
    },
  };
}

function item(id: string, name: string): InventoryItem {
  return {
    id: `inventory-${id}`,
    quantity: 1,
    itemInstance: {
      id: `instance-${id}`,
      itemBase: {
        id,
        name,
        rarity: Rarity.Common,
        itemType: ItemType.Resource,
        description: '',
        stackable: true,
      },
    },
  };
}
