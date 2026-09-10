export interface CombatStyleChoiceDefinition {
  id: string;
  name: string;
  description: string;
  tuning?: CombatStyleMasteryTuning | null;
}

// Mastery-relevant fields from the tuning already returned by the API.
export interface CombatStyleMasteryTuning {
  barrierFraction: number;
  barrierPerMasteryLevel: number;
  channeledBaseMultiplier: number;
  channeledPerCharge: number;
  channeledPerMasteryLevel: number;
}

export interface CombatStyleUpgradeDefinition
  extends CombatStyleChoiceDefinition {
  masteryDescription: string;
}

export interface CombatStyleDefinition {
  id: string;
  name: string;
  description: string;
  kind: 'Bastion' | 'Conduit';
  tuning?: CombatStyleMasteryTuning;
  refinements: CombatStyleChoiceDefinition[];
  upgrades: CombatStyleUpgradeDefinition[];
  openingTechnique?: { name: string; description: string } | null;
}

export interface CombatStyleEntry {
  definition: CombatStyleDefinition;
  level: number;
  currentXp: number;
  xpRequired: number;
  upgradeSlots: number;
  refinementId: string | null;
  upgradeIds: string[];
  masteredUpgradeId?: string | null;
}

export interface CombatStyleSelectionRequest {
  combatStyleId: string | null;
  refinementId: string | null;
  upgradeIds: string[];
  masteredUpgradeId?: string | null;
  restoreRememberedChoices?: boolean;
}

export interface CombatStyleSnapshot {
  combatStyleId: string;
  kind: 'Bastion' | 'Conduit';
  contentVersion: string;
  level: number;
  refinementId: string | null;
  upgradeIds: string[];
  masteredUpgradeId?: string | null;
  channeledPlayerEssenceId: string | null;
  channeledEssenceDefinitionId: string | null;
}

export interface CombatStylePreviewFact {
  label: string;
  value: string;
  condition: string | null;
}

export interface CombatStyleOverview {
  contentVersion: string;
  styles: CombatStyleEntry[];
  selection: CombatStyleSelectionRequest;
  effectiveStyle: CombatStyleSnapshot | null;
  validationIssue: string | null;
  previewFacts?: CombatStylePreviewFact[];
}

export function combatStyleNextMilestone(level: number): string {
  if (level >= 10) return 'Maximum mastery · two upgrade slots';
  const milestones: Record<number, string> = {
    3: 'Mastery bonus and refinements',
    5: 'Mastery bonus and first upgrade slot',
    7: 'Mastery bonus and Opening Technique',
    8: 'Mastery bonus and second upgrade slot',
    9: 'Mastery bonus and Upgrade Mastery',
    10: 'Maximum mastery bonus and level cap',
  };
  const next = Math.max(0, level) + 1;
  return `Level ${next}: ${milestones[next] ?? 'Mastery bonus'}`;
}
