export interface CombatStylesOverviewDto {
  activeStyleId: string | null;
  styles: CombatStyleDto[];
}

export interface CombatStyleDto {
  id: string;
  name: string;
  description: string;
  resourceId: string;
  coreMechanic: string;
  level: number;
  experience: number;
  experienceForCurrentLevel: number;
  experienceForNextLevel: number;
  isActive: boolean;
  selectedFocusId: string | null;
  skillPointsEarned: number;
  skillPointsSpent: number;
  skillPointsAvailable: number;
  focuses: CombatStyleFocusDto[];
  skillTree: CombatStyleSkillTreeDto;
  ruleSummaries: CombatStyleRuleSummaryDto[];
}

export interface CombatStyleFocusDto {
  id: string;
  name: string;
  description: string;
  unlockLevel: number;
  isUnlocked: boolean;
  isSelected: boolean;
}

export interface CombatStyleRuleSummaryDto {
  id: string;
  text: string;
}

export interface CombatStyleSkillTreeDto {
  branches: CombatStyleSkillTreeBranchDto[];
}

export interface CombatStyleSkillTreeBranchDto {
  id: string;
  name: string;
  description: string;
  pointsSpent: number;
  nodes: CombatStyleSkillTreeNodeDto[];
}

export interface CombatStyleSkillTreeNodeDto {
  id: string;
  branchId: string;
  name: string;
  description: string;
  rank: number;
  maxRank: number;
  requiredLevel: number;
  requiredNodeId: string | null;
  x: number;
  y: number;
  isUnlocked: boolean;
  canRankUp: boolean;
  tags: string[];
  row: number;
  lane: string;
  nodeType: string;
  mutatorKind: string | null;
  mutatorGroups: string[];
  tooltip: CombatStyleNodeTooltipDto;
}

export interface CombatStyleNodeTooltipDto {
  affects: string[];
  tradeoffs: string[];
  doesNotAffect: string[];
}

export interface ActivateCombatStyleResponseDto {
  success: boolean;
  activeStyleId: string;
  message: string;
}

export interface CombatStyleMutationResponseDto {
  success: boolean;
  message: string;
  style: CombatStyleDto | null;
}

