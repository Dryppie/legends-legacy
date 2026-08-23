import { TestBed } from '@angular/core/testing';
import { setAttributeDefinitions } from '../../models/attribute-definition';
import { ModifierType } from '../../models/Dtos/attributesDto';
import { EquipmentSlotType } from '../../models/Dtos/equipment-slots/equipmentSlot';
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
  it('compares effective bonuses and compounds matching affixes', () => {
    const hovered = display([]);
    const equipped = display([]);
    hovered.toolBonuses = [
      toolBonus('hovered-base', 5),
      toolBonus('hovered-affix', 7),
    ];
    equipped.toolBonuses = [toolBonus('equipped-affix', 8)];

    const [comparison] = buildToolBonusComparisons(hovered, equipped);

    expect(comparison).toEqual(
      jasmine.objectContaining({
        bonusType: ToolBonusType.GatheringYieldPercent,
        scopeId: undefined,
        equippedAmount: 8,
      }),
    );
    expect(comparison.hoveredAmount).toBeCloseTo(12.35, 10);
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

  it('maps persisted roll ranges for equipment previews', () => {
    const item = equipmentInstance('rolled-item', AttributeType.MaxHealth, 113);
    item.rollRange = {
      minimumPotential: 260,
      maximumPotential: 380,
      attributes: [
        {
          attributeType: AttributeType.MaxHealth,
          minimumAmount: 93,
          maximumAmount: 124,
        },
      ],
    };

    const displayItem = mapInstanceToDisplay(item);

    expect(displayItem.minimumPotential).toBe(260);
    expect(displayItem.maximumPotential).toBe(380);
    expect(displayItem.attributeRollRanges).toEqual(item.rollRange.attributes);
  });

  it('maps the character level required to equip the item', () => {
    const item = equipmentInstance('tier-two-item', AttributeType.Armor, 100);
    item.tier = 2;
    item.requiredLevel = 50;

    expect(mapInstanceToDisplay(item).requiredLevel).toBe(50);
  });

  it('does not expose a character level requirement for tools', () => {
    const item = toolInstance();
    item.requiredLevel = 50;

    expect(mapInstanceToDisplay(item).requiredLevel).toBe(1);
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
        equipmentDisplayName: 'Crowd Control Resistance',
        equipmentDescription: 'Direct crowd-control resistance from this item.',
        equipmentUnit: 'PercentagePoints',
        equipmentDisplayPrecision: 2,
        equipmentDisplaySuffix: '%',
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

  it('renders attribute roll ranges and keeps potential as a plain footer value', async () => {
    await TestBed.configureTestingModule({
      imports: [EquipmentDisplayComponent],
    }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentDisplayComponent);
    const item = equipmentInstance('rolled-item', AttributeType.MaxHealth, 113);
    item.potential = 310;
    item.rollRange = {
      minimumPotential: 260,
      maximumPotential: 380,
      attributes: [
        {
          attributeType: AttributeType.MaxHealth,
          minimumAmount: 93,
          maximumAmount: 124,
        },
      ],
    };

    fixture.componentRef.setInput('item', item);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    const fills = [
      ...fixture.nativeElement.querySelectorAll('.equipment-design-fill'),
    ] as HTMLElement[];

    expect(text).toContain('Potential');
    expect(text).toContain('310');
    expect(text).not.toContain('/ 260–380');
    expect(text).toContain('/ 93–124');
    expect(fills.length).toBe(1);
    expect(Number.parseFloat(fills[0].style.width)).toBeCloseTo(64.52, 1);
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

  it('makes tool bonus rows focusable for their mechanic tooltips', async () => {
    await TestBed.configureTestingModule({
      imports: [EquipmentDisplayComponent],
    }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentDisplayComponent);

    fixture.componentRef.setInput('item', toolInstance());
    fixture.detectChanges();

    const row: HTMLElement | null = fixture.nativeElement.querySelector(
      '[data-testid="tool-attributes"] .ll-item-stat-row',
    );

    expect(row?.classList).toContain('cursor-help');
    expect(row?.getAttribute('tabindex')).toBe('0');
  });

  it('renders separate main-hand and off-hand weapon comparisons', async () => {
    await TestBed.configureTestingModule({
      imports: [EquipmentDisplayComponent],
    }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentDisplayComponent);
    const hovered = equipmentInstance(
      'hovered-weapon',
      AttributeType.Power,
      15,
      EquipmentType.OneHanded,
    );
    const mainHand = equipmentInstance(
      'main-hand-weapon',
      AttributeType.Power,
      10,
      EquipmentType.OneHanded,
    );
    const offHand = equipmentInstance(
      'off-hand-weapon',
      AttributeType.Power,
      5,
      EquipmentType.OffHand,
    );
    mainHand.displayName = 'Main Hand Blade';
    offHand.displayName = 'Off Hand Shield';

    fixture.componentRef.setInput('item', hovered);
    fixture.componentRef.setInput('comparisonItems', [
      {
        slotType: EquipmentSlotType.MainHand,
        equipmentInstance: mainHand,
      },
      {
        slotType: EquipmentSlotType.OffHand,
        equipmentInstance: offHand,
      },
    ]);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    const differences = [
      ...fixture.nativeElement.querySelectorAll(
        '[data-testid="comparison-difference"]',
      ),
    ].map((element: Element) => element.textContent?.trim());

    expect(text).toContain('Equipped · Main Hand');
    expect(text).toContain('Main Hand Blade');
    expect(text).toContain('Equipped · Off Hand');
    expect(text).toContain('Off Hand Shield');
    expect(differences).toEqual(['+5', '+10']);
  });

  it('renders absent attributes as dashes instead of zero-value rolls', async () => {
    await TestBed.configureTestingModule({
      imports: [EquipmentDisplayComponent],
    }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentDisplayComponent);

    fixture.componentRef.setInput(
      'item',
      equipmentInstance('hovered', AttributeType.MaxHealth, 20),
    );
    fixture.componentRef.setInput('comparisonItems', [
      {
        slotType: EquipmentSlotType.Head,
        equipmentInstance: equipmentInstance(
          'equipped',
          AttributeType.Armor,
          10,
        ),
      },
    ]);
    fixture.detectChanges();

    const values = (testId: string) =>
      [
        ...fixture.nativeElement.querySelectorAll(`[data-testid="${testId}"]`),
      ].map((element: Element) => element.textContent?.trim());

    expect(values('hovered-attribute-value')).toEqual(['20', '—']);
    expect(values('equipped-attribute-value')).toEqual(['—', '10']);
  });

  it('renders a single selected item with inline stat differences', async () => {
    await TestBed.configureTestingModule({
      imports: [EquipmentDisplayComponent],
    }).compileComponents();
    const fixture = TestBed.createComponent(EquipmentDisplayComponent);
    const selected = equipmentInstance('selected', AttributeType.MaxHealth, 20);
    const equipped = equipmentInstance('equipped', AttributeType.Armor, 7);
    selected.displayName = 'Selected Cowl';
    equipped.displayName = 'Equipped Helm';

    fixture.componentRef.setInput('item', selected);
    fixture.componentRef.setInput('comparisonItem', equipped);
    fixture.componentRef.setInput('inlineComparison', true);
    fixture.componentRef.setInput('embedded', true);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    const differences = [
      ...fixture.nativeElement.querySelectorAll(
        '[data-testid="inline-comparison-difference"]',
      ),
    ].map((element: Element) => element.textContent?.trim());

    expect(text).not.toContain('Selected Cowl');
    expect(text).not.toContain('Equipped Helm');
    expect(text).not.toContain('Compared with equipped');
    expect(text).toContain('Attributes');
    expect(text).not.toContain('Rolled attributes');
    expect(differences).toEqual(['+20', '-7']);
    expect(
      fixture.nativeElement.querySelector('.equipment-comparison-grid'),
    ).toBeNull();
    expect(
      fixture.nativeElement.querySelector('.equipment-design-card'),
    ).toHaveClass('equipment-design-card-embedded');
    expect(
      fixture.nativeElement.querySelector('.equipment-design-card'),
    ).not.toHaveClass('bg-texture');
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
    requiredLevel: 1,
    statModelVersion: 15,
    toolBonuses: [],
    toolAffixes: [],
    baseToolBonuses: [],
  };
}

function equipmentInstance(
  id: string,
  attributeType: AttributeType,
  amount: number,
  equipmentType = EquipmentType.Head,
): EquipmentInstance {
  const equipmentBase: Equipment = {
    id: `${id}-base`,
    name: 'Heavy Helm',
    rarity: Rarity.Common,
    itemType: ItemType.Equipment,
    description: '',
    stackable: false,
    equipmentType,
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
    isGuildBorrowed: false,
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
    isGuildBorrowed: false,
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
