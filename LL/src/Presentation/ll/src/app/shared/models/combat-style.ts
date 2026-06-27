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
  recommendedTags: string[];
  recommendedStats: string[];
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
  recommendedTags: string[];
  recommendedStats: string[];
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
  recommendedTags: string[];
  recommendedStats: string[];
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
  effects: string[];
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

export interface CombatBuildPreviewDto {
  activeStyleId: string;
  activeStyleName: string;
  selectedFocusId: string | null;
  selectedFocusName: string | null;
  buildName: string;
  topTags: TagScoreDto[];
  recommendedStats: string[];
  notes: string[];
}

export interface TagScoreDto {
  tag: string;
  score: number;
}
