import { ToolBonusType } from '../../../../models/item';
import { toolBonusDisplayLabel, toolBonusTooltip } from './tool-bonus-tooltip';

describe('toolBonusTooltip', () => {
  it('explains that bonus and double rolls are independent', () => {
    expect(
      toolBonusTooltip(ToolBonusType.BonusRollChancePercent)?.description,
    ).toContain('independent of Double Gather Chance');
    expect(
      toolBonusTooltip(ToolBonusType.DoubleGatherChancePercent)?.description,
    ).toContain('separate from Bonus Roll Chance');
  });

  it('identifies rare materials by reward-table tags and examples', () => {
    const tooltip = toolBonusTooltip(ToolBonusType.RareMaterialChancePercent);
    const description = tooltip?.description;

    expect(tooltip?.title).toContain('Catalytic');
    expect(description).toContain('tagged as rare');
    expect(description).toContain('Fury Heart');
    expect(description).toContain('does not change normal materials');
  });

  it('documents that double gather has no base chance', () => {
    expect(
      toolBonusTooltip(ToolBonusType.DoubleGatherChancePercent)?.description,
    ).toContain('no base chance');
  });

  it('uses the Catalytic label for legacy rare-material bonus types', () => {
    expect(toolBonusDisplayLabel(ToolBonusType.RareMaterialChancePercent)).toBe(
      'Catalytic · Catalyst Chance',
    );
  });
});
