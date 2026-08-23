import { ToolBonusType } from '../../../../models/item';

export interface ToolBonusTooltipDefinition {
  title: string;
  description: string;
}

const TOOL_BONUS_TOOLTIPS: Partial<
  Record<ToolBonusType, ToolBonusTooltipDefinition>
> = {
  [ToolBonusType.NodeSuccessChancePercent]: {
    title: 'Node Success Chance',
    description:
      'Increases the gathering chance by this percentage, relative to its base chance. For example, +25% turns a 40% chance into 50%.',
  },
  [ToolBonusType.GatheringYieldPercent]: {
    title: 'Gathering Yield',
    description:
      'Increases the quantity of every item gathered after a successful reward roll.',
  },
  [ToolBonusType.DoubleGatherChancePercent]: {
    title: 'Double Gather Chance',
    description:
      'After a successful attempt produces a reward, this rolls once to double all quantities from that attempt.',
  },
  [ToolBonusType.BonusRollChancePercent]: {
    title: 'Bonus Roll Chance',
    description:
      "After the gathering attempt succeeds, this is the odds for a second roll of the node's entire reward table.",
  },
  [ToolBonusType.RareMaterialChancePercent]: {
    title: 'Rare Material Chance',
    description:
      'Increases the chance to receive Catalysts on successful gathering attempts.',
  },
};

export function toolBonusTooltip(
  bonusType: ToolBonusType,
): ToolBonusTooltipDefinition | null {
  return TOOL_BONUS_TOOLTIPS[bonusType] ?? null;
}
