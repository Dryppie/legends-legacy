export type SoulstoneUpgradeBranch =
  | 'EssenceArchive'
  | 'CombatProgression'
  | 'Gathering'
  | 'Crafting'
  | 'Dungeons'
  | 'AccountConvenience';

export interface SoulstoneUpgradeView {
  id: string;
  branch: SoulstoneUpgradeBranch;
  displayName: string;
  description: string;
  currentRank: number;
  maxRank: number;
  currentEffectText: string;
  nextEffectText?: string | null;
  nextCost?: number | null;
  canPurchase: boolean;
  disabledReason?: string | null;
  appliesTo: string[];
  doesNotApplyTo: string[];
  isRegionCapped: boolean;
  requiredRegionForNextRank?: number | null;
  refundValue: number;
  sortOrder: number;
  frontendHint?: string | null;
}

export interface SoulstoneUpgradeMutationResult {
  upgrades: SoulstoneUpgradeView[];
  soulstones: number;
  refundedSoulstones: number;
}
