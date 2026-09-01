import { ToolBonusType } from '../../../../models/item';

export interface ToolBonusTooltipDefinition {
  title: string;
  description: string;
}

const TOOL_BONUS_LABELS: Partial<Record<ToolBonusType, string>> = {
  [ToolBonusType.GatheringYieldPercent]: 'Abundant · Gathering Yield',
  [ToolBonusType.NodeSuccessChancePercent]: 'Reliable · Node Success Chance',
  [ToolBonusType.RareMaterialChancePercent]: 'Catalytic · Catalyst Chance',
  [ToolBonusType.DoubleGatherChancePercent]:
    'Duplicating · Double Gather Chance',
  [ToolBonusType.BonusRollChancePercent]: "Opportunist's · Bonus Roll Chance",
};

const TOOL_BONUS_TOOLTIPS: Partial<
  Record<ToolBonusType, ToolBonusTooltipDefinition>
> = {
  [ToolBonusType.NodeSuccessChancePercent]: {
    title: 'Reliable · Node Success Chance',
    description:
      'The Reliable profile increases the gathering chance relative to its base chance. For example, +25% turns a 40% chance into 50%.',
  },
  [ToolBonusType.GatheringYieldPercent]: {
    title: 'Abundant · Gathering Yield',
    description:
      'The Abundant profile increases quantities after a successful reward roll. It primarily improves ordinary material output, not attempt success or rare weighting.',
  },
  [ToolBonusType.DoubleGatherChancePercent]: {
    title: 'Duplicating · Double Gather Chance',
    description:
      'The Duplicating profile rolls after an attempt produces a reward and doubles all quantities from that attempt. It has no base chance and is separate from Bonus Roll Chance.',
  },
  [ToolBonusType.BonusRollChancePercent]: {
    title: "Opportunist's · Bonus Roll Chance",
    description:
      "The Opportunist's profile gives a successful attempt a second roll of the node's entire reward table. This roll is independent of Double Gather Chance.",
  },
  [ToolBonusType.RareMaterialChancePercent]: {
    title: 'Catalytic · Catalyst Chance',
    description:
      "The Catalytic profile increases the relative weight of reward-table entries tagged as rare, such as Fury Catalyst and other Catalysts. It does not change normal materials or the node's base success chance.",
  },
};

export function toolBonusTooltip(
  bonusType: ToolBonusType,
): ToolBonusTooltipDefinition | null {
  return TOOL_BONUS_TOOLTIPS[bonusType] ?? null;
}

export function toolBonusDisplayLabel(bonusType: string): string | null {
  return TOOL_BONUS_LABELS[bonusType as ToolBonusType] ?? null;
}
