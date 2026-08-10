import { TestBed } from '@angular/core/testing';
import { setAttributeDefinitions } from '../../models/attribute-definition';
import { ModifierType } from '../../models/Dtos/attributesDto';
import { AttributeType } from '../../models/enums/attributeType';
import { EquipmentType } from '../../models/enums/equipmentType';
import { ItemQuality } from '../../models/enums/itemQuality';
import { ItemType } from '../../models/enums/itemType';
import { Rarity } from '../../models/enums/rarity';
import { Equipment, EquipmentInstance, ToolBonusType } from '../../models/item';
import {
  buildAttributeComparisons,
  buildToolBonusComparisons,
  EquipmentDisplay,
  mapInstanceToDisplay,
} from './equipment-display';
import { EquipmentDisplayComponent } from './equipment-display/equipment-display.component';

describe('buildAttributeComparisons', () => {
  it('shows gains, losses, and attributes unique to either item', () => {
    const hovered = display([
      [AttributeType.Armor, 15],
      [AttributeType.MaxHealth, 20],
    ]);
    const equipped = display([
      [AttributeType.Armor, 10],
      [AttributeType.BlockChance, 7],
    ]);

    const comparisons = buildAttributeComparisons(hovered, equipped);

    expect(
      comparisons.map((comparison) => ({
        attributeType: comparison.attributeType,
        equippedAmount: comparison.equippedAmount,
        hoveredAmount: comparison.hoveredAmount,
      })),
    ).toEqual([
      {
        attributeType: AttributeType.MaxHealth,
        equippedAmount: 0,
        hoveredAmount: 20,
      },
      {
        attributeType: AttributeType.Armor,
        equippedAmount: 10,
        hoveredAmount: 15,
      },
      {
        attributeType: AttributeType.BlockChance,
        equippedAmount: 7,
        hoveredAmount: 0,
      },
    ]);
  });

  it('compares one total when modifier types differ', () => {
    const hovered = display([
      [AttributeType.CrowdControlResistance, 9, ModifierType.Additive],
    ]);
    const equipped = display([
      [AttributeType.CrowdControlResistance, 1, ModifierType.Flat],
    ]);

    const [comparison] = buildAttributeComparisons(hovered, equipped);

    expect(comparison).toEqual({
      attributeType: AttributeType.CrowdControlResistance,
      equippedAmount: 1,
      hoveredAmount: 9,
    });
    expect(comparison.hoveredAmount - comparison.equippedAmount).toBe(8);
  });
});

describe('buildToolBonusComparisons', () => {
  it('compares effective bonuses and aggregates matching affixes', () => {
    const hovered = display([]);
    const equipped = display([]);
    hovered.toolBonuses = [
      toolBonus('hovered-base', 5),
      toolBonus('hovered-affix', 7),
    ];
    equipped.toolBonuses = [toolBonus('equipped-affix', 8)];

    expect(buildToolBonusComparisons(hovered, equipped)).toEqual([
      {
        bonusType: ToolBonusType.GatheringYieldPercent,
        scopeId: undefined,
        equippedAmount: 8,
        hoveredAmount: 12,
      },
    ]);
  });
});

describe('mapInstanceToDisplay', () => {
  it('uses combined attributes when a marketplace DTO omits split modifier lists', () => {
    const item = equipmentInstance(
      'marketplace-item',
      AttributeType.MaxHealth,
      70,
    );
    item.baseModifiers = [];
    item.instanceModifiers = [];

    expect(mapInstanceToDisplay(item).attributes).toEqual([
      {
        attributeType: AttributeType.MaxHealth,
        amount: 70,
        modifierType: ModifierType.Flat,
      },
    ]);
  });
});

describe('EquipmentDisplayComponent', () => {
  afterEach(() => setAttributeDefinitions([]));

  it('renders the crowd control resistance difference from the visible values', async () => {
    setAttributeDefinitions([
      {
        attributeType: AttributeType.CrowdControlResistance,
        displayName: 'Crowd Control Resistance',
        description: 'Reduces crowd-control duration.',
        unit: 'PercentagePoints',
        stackingRule: 'Additive',
        minimumValue: 0,
        maximumValue: 80,
        capKind: 'Fixed',
        isEquipmentEligible: true,
        isContentFacing: true,
        displayPrecision: 2,
        displaySuffix: '%',
        relevantBenchmarkScenarios: ['CrowdControlResilience'],
      },
    ]);
    await TestBed.configureTestingModule({
      imports: [EquipmentDisplayComponent],
    }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentDisplayComponent);

    fixture.componentRef.setInput(
      'item',
      equipmentInstance('hovered', AttributeType.CrowdControlResistance, 9),
    );
    fixture.componentRef.setInput(
      'comparisonItem',
      equipmentInstance('equipped', AttributeType.CrowdControlResistance, 1),
    );
    fixture.detectChanges();

    const difference: HTMLElement | null = fixture.nativeElement.querySelector(
      '[data-testid="comparison-difference"]' +
        '[data-attribute="CrowdControlResistance"]',
    );

    expect(difference?.textContent?.trim()).toBe('+8%');
  });

  it('renders tool affixes using the standard equipment attribute layout', async () => {
    await TestBed.configureTestingModule({
      imports: [EquipmentDisplayComponent],
    }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentDisplayComponent);

    fixture.componentRef.setInput('item', toolInstance());
    fixture.componentRef.setInput(
      'comparisonItem',
      toolInstance(8, 'equipped-tool'),
    );
    fixture.detectChanges();

    const section: HTMLElement | null = fixture.nativeElement.querySelector(
      '[data-testid="tool-attributes"]',
    );
    const row: HTMLElement | null =
      section?.querySelector('.ll-item-stat-row') ?? null;

    expect(section?.classList).toContain('ll-item-detail-section');
    expect(section?.textContent).toContain('Attributes');
    expect(row?.textContent).toContain('Gathering Yield');
    expect(row?.textContent).toContain('+12%');

    const difference: HTMLElement | null = fixture.nativeElement.querySelector(
      '[data-testid="tool-comparison-difference"]',
    );
    expect(difference?.textContent?.trim()).toBe('+4%');
  });
});

function display(
  attributes: readonly [AttributeType, number, ModifierType?][],
): EquipmentDisplay {
  return {
    name: 'Gear',
    rarity: Rarity.Common,
    equipmentType: EquipmentType.Chest,
    description: '',
    attributes: attributes.map(([attributeType, amount, modifierType]) => ({
      attributeType,
      amount,
      modifierType: modifierType ?? ModifierType.Flat,
    })),
    itemBudget: 0,
    itemBudgetTier: 1,
    toolBonuses: [],
    toolAffixes: [],
    baseToolBonuses: [],
  };
}

function equipmentInstance(
  id: string,
  attributeType: AttributeType,
  amount: number,
): EquipmentInstance {
  const equipmentBase: Equipment = {
    id: `${id}-base`,
    name: 'Heavy Helm',
    rarity: Rarity.Common,
    itemType: ItemType.Equipment,
    description: '',
    stackable: false,
    equipmentType: EquipmentType.Head,
    attributeModifiers: [],
    itemBudget: amount,
    itemBudgetTier: 1,
  };

  const modifiers = [
    {
      attributeType,
      amount,
      modifierType: ModifierType.Flat,
    },
  ];

  return {
    id,
    itemBase: equipmentBase,
    displayName: equipmentBase.name,
    rarity: Rarity.Common,
    quality: ItemQuality.Standard,
    tier: 1,
    equipmentBase,
    potential: 210,
    temperingProgress: 0,
    itemXp: 0,
    baseModifiers: modifiers,
    instanceModifiers: [],
    attributeModifiers: modifiers,
    toolAffixes: [],
    effectiveToolBonuses: [],
    affinityTags: [],
    itemBudget: amount,
    itemBudgetTier: 1,
  };
}

function toolInstance(amount = 12, id = 'test-tool'): EquipmentInstance {
  const equipmentBase: Equipment = {
    id: 'test-pickaxe',
    name: 'Pickaxe',
    rarity: Rarity.Common,
    itemType: ItemType.Equipment,
    description: '',
    stackable: false,
    equipmentType: EquipmentType.Tool,
    attributeModifiers: [],
    toolBonuses: [],
    itemBudget: 0,
    itemBudgetTier: 1,
  };
  const affix = toolBonus('test-affix', amount, id);

  return {
    id,
    itemBase: equipmentBase,
    displayName: 'Plain Pickaxe',
    rarity: Rarity.Common,
    quality: ItemQuality.Standard,
    tier: 1,
    equipmentBase,
    potential: undefined,
    temperingProgress: 0,
    itemXp: 0,
    baseModifiers: [],
    instanceModifiers: [],
    attributeModifiers: [],
    toolAffixes: [affix],
    effectiveToolBonuses: [affix],
    affinityTags: [],
    itemBudget: 0,
    itemBudgetTier: 1,
  };
}

function toolBonus(id: string, amount: number, equipmentInstanceId?: string) {
  return {
    id,
    equipmentInstanceId,
    name: "Prospector's",
    bonusType: ToolBonusType.GatheringYieldPercent,
    amount,
  };
}
