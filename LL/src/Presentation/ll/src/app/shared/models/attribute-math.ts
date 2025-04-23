import { ATTRIBUTE_EFFECTS, Effect } from './attribute-effects';
import { AttributeType } from './enums/attributeType';

export interface Contribution {
  to: string; // secondary stat name
  value: number; // numeric gain
}

function apply(points: number, e: Effect): number {
  const raw =
    e.kind === 'linear'
      ? points * e.coefficient
      : Math.floor(points / e.every) * e.amount;

  // toFixed returns a string like "1.50", parseFloat then gives 1.5
  return parseFloat(raw.toFixed(2));
}
export function getContributions(
  stat: AttributeType,
  points: number,
): Contribution[] {
  const effects = ATTRIBUTE_EFFECTS[stat];
  if (!effects) return []; // secondary stats land here
  return effects
    .map((e) => ({ to: e.gives, value: apply(points, e) }))
    .filter((c) => c.value !== 0);
}
