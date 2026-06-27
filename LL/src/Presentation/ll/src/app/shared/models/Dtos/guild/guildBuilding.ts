import { GuildResourceType } from './guildResourceType';

export type GuildBuildingType =
  | 'GuildHall'
  | 'MissionBoard'
  | 'MarketOffice'
  | 'RaidHall'
  | 'WarRoom'
  | 'Workshop'
  | 'TrainingGrounds'
  | 'EssenceSanctum'
  | 'Treasury';

export type GuildBuildingStatus = 'Active' | 'UnderConstruction' | 'Upgrading';

export type GuildActivityLogType =
  | 'BuildingConstructionStarted'
  | 'BuildingConstructed'
  | 'BuildingUpgradeStarted'
  | 'BuildingUpgraded';

export interface GuildBuildingDefinition {
  type: GuildBuildingType;
  name: string;
  description: string;
  maxLevel: number;
  isPermanent: boolean;
  requiredGuildHallLevel: number;
  unlockSummary: string;
  benefits: GuildBuildingBenefit[];
}

export interface GuildBuildingBenefit {
  level: number;
  title: string;
  description: string;
  isImplemented: boolean;
}

export interface GuildBuilding {
  id?: string | null;
  definition: GuildBuildingDefinition;
  level: number;
  targetLevel?: number | null;
  status: GuildBuildingStatus;
  completesAt?: string | null;
  nextCost?: Partial<Record<GuildResourceType, number>> | null;
  canConstruct: boolean;
  canUpgrade: boolean;
  lockedReason?: string | null;
}

export interface GuildActivityLog {
  type: GuildActivityLogType;
  characterId?: string | null;
  message: string;
  createdAt: string;
}

export interface GuildBuildingOverview {
  guildId: string;
  guildHallLevel: number;
  guildSupplies: number;
  canManageBuildings: boolean;
  buildings: GuildBuilding[];
  activityLogs: GuildActivityLog[];
}
