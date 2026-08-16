import {
  AttributeDefinition,
  setAttributeDefinitions,
} from '../../models/attribute-definition';
import { AttributeType } from '../../models/enums/attributeType';
import {
  formatAttributeTooltip,
  formatAttributeType,
} from './attribute-type-format/attribute-type-format.pipe';
import { formatAttributeValue } from './attribute-value-format/attribute-value-format.pipe';

describe('canonical attribute formatting', () => {
  beforeEach(() => {
    setAttributeDefinitions([
      definition({
        attributeType: AttributeType.CritChance,
        displayName: 'Critical Chance',
        description: 'Chance to critically strike.',
        unit: 'PercentagePoints',
        maximumValue: 75,
        capKind: 'Fixed',
        displaySuffix: '%',
      }),
      definition({
        attributeType: AttributeType.StatusResistance,
        displayName: 'Status Resistance',
        description: 'Status-duration resistance rating.',
        unit: 'Rating',
        displayPrecision: 0,
      }),
      definition({
        attributeType: AttributeType.CrowdControlResistance,
        displayName: 'Crowd Control Resistance',
        description: 'Reduces crowd-control duration.',
        unit: 'PercentagePoints',
        maximumValue: 80,
        capKind: 'Fixed',
        displayPrecision: 2,
        displaySuffix: '%',
      }),
      definition({
        attributeType: AttributeType.HealthRegeneration,
        displayName: 'Health Regen',
        description: 'Health restored every five seconds.',
        unit: 'HealthPerFiveSeconds',
        displayPrecision: 0,
        displaySuffix: ' HP/5s',
      }),
    ]);
  });

  afterEach(() => setAttributeDefinitions([]));

  it('uses server-provided labels, units, precision, and caps', () => {
    expect(formatAttributeType(AttributeType.CritChance)).toBe(
      'Critical Chance',
    );
    expect(formatAttributeValue(12.345, AttributeType.CritChance, true)).toBe(
      '+12.35%',
    );
    expect(formatAttributeTooltip(AttributeType.CritChance)).toBe(
      'Chance to critically strike. Cap: 75%.',
    );
  });

  it('separates raw equipment ratings from effective character percentages', () => {
    expect(formatAttributeType(AttributeType.CritChance, true)).toBe(
      'Critical Chance Rating',
    );
    expect(
      formatAttributeValue(12.345, AttributeType.CritChance, true, true),
    ).toBe('+12.35');
    expect(formatAttributeTooltip(AttributeType.CritChance, true)).toBe(
      'Higher rating improves this effect with diminishing returns.',
    );
    expect(formatAttributeValue(12.345, AttributeType.CritChance, true)).toBe(
      '+12.35%',
    );
  });

  it('rounds flat and rating attributes to integers', () => {
    expect(formatAttributeValue(25.49, AttributeType.StatusResistance)).toBe(
      '25',
    );
    expect(formatAttributeValue(25.5, AttributeType.StatusResistance)).toBe(
      '26',
    );
    expect(formatAttributeValue(4.5, AttributeType.HealthRegeneration)).toBe(
      '5 HP/5s',
    );
    expect(formatAttributeValue(9.933024, AttributeType.StatusResistance)).toBe(
      '10',
    );
    expect(
      formatAttributeValue(99.9, AttributeType.StatusResistance, true),
    ).toBe('+100');
  });

  it('formats crowd control resistance as percentage points', () => {
    expect(
      formatAttributeValue(
        9.933024,
        AttributeType.CrowdControlResistance,
      ),
    ).toBe('9.93%');
    expect(
      formatAttributeValue(8, AttributeType.CrowdControlResistance, true),
    ).toBe('+8%');
  });
});

function definition(
  overrides: Partial<AttributeDefinition> &
    Pick<AttributeDefinition, 'attributeType' | 'displayName' | 'description'>,
): AttributeDefinition {
  return {
    unit: 'FlatPoints',
    stackingRule: 'Additive',
    minimumValue: 0,
    maximumValue: null,
    capKind: 'None',
    isEquipmentEligible: true,
    isContentFacing: true,
    displayPrecision: 2,
    displaySuffix: '',
    relevantBenchmarkScenarios: ['PhysicalOffense'],
    equipmentDisplayName: `${overrides.displayName} Rating`,
    equipmentDescription:
      'Higher rating improves this effect with diminishing returns.',
    equipmentUnit: 'Rating',
    equipmentDisplayPrecision: 2,
    equipmentDisplaySuffix: '',
    ...overrides,
  };
}
