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
        approvedPrimarySource: AttributeType.Precision,
      }),
      definition({
        attributeType: AttributeType.StatusResistance,
        displayName: 'Status Resistance',
        description: 'Status-duration resistance rating.',
        unit: 'Rating',
      }),
      definition({
        attributeType: AttributeType.HealthRegeneration,
        displayName: 'Health Regen',
        description: 'Health restored per interval.',
        unit: 'HealthPerRegenerationInterval',
        displaySuffix: ' HP/interval',
      }),
    ]);
  });

  afterEach(() => setAttributeDefinitions([]));

  it('uses server-provided labels, units, precision, caps, and primary sources', () => {
    expect(formatAttributeType(AttributeType.CritChance)).toBe(
      'Critical Chance',
    );
    expect(formatAttributeValue(12.345, AttributeType.CritChance, true)).toBe(
      '+12.35%',
    );
    expect(formatAttributeTooltip(AttributeType.CritChance)).toBe(
      'Chance to critically strike. Cap: 75%. Also gained from Precision.',
    );
  });

  it('does not present rating attributes as percentages', () => {
    expect(formatAttributeValue(25, AttributeType.StatusResistance)).toBe('25');
    expect(formatAttributeValue(4.5, AttributeType.HealthRegeneration)).toBe(
      '4.5 HP/interval',
    );
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
    approvedPrimarySource: null,
    relevantBenchmarkScenarios: ['PhysicalOffense'],
    ...overrides,
  };
}
