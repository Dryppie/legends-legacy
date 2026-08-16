import { AttributeType } from './enums/attributeType';

export type AttributeUnit =
  | 'FlatPoints'
  | 'PercentagePoints'
  | 'Rating'
  | 'HealthPerFiveSeconds'
  | 'MultiplierInput';

export type AttributeStackingRule =
  | 'Additive'
  | 'Multiplicative'
  | 'Maximum'
  | 'DerivedOnly';

export type AttributeCapKind = 'None' | 'Fixed' | 'ContextDependent';

export interface AttributeDefinition {
  attributeType: AttributeType;
  displayName: string;
  description: string;
  unit: AttributeUnit;
  stackingRule: AttributeStackingRule;
  minimumValue: number;
  maximumValue: number | null;
  capKind: AttributeCapKind;
  isEquipmentEligible: boolean;
  isContentFacing: boolean;
  displayPrecision: number;
  displaySuffix: string;
  relevantBenchmarkScenarios: string[];
  equipmentDisplayName?: string;
  equipmentDescription?: string;
  equipmentUnit?: AttributeUnit;
  equipmentDisplayPrecision?: number;
  equipmentDisplaySuffix?: string;
}

let definitions = new Map<AttributeType, AttributeDefinition>();

export function setAttributeDefinitions(
  values: readonly AttributeDefinition[] | null | undefined,
): void {
  definitions = new Map(
    (values ?? []).map((definition) => [
      definition.attributeType,
      definition,
    ]),
  );
}

export function getAttributeDefinition(
  attribute?: string | null,
): AttributeDefinition | undefined {
  if (!attribute) return undefined;
  return definitions.get(attribute as AttributeType);
}
