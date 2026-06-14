export interface DungeonRecordEntryData {
  characterId: string;
  characterName: string;
  firstClearedAt: string;
  lastClearedAt: string;
  totalClears: number;
}

export interface DungeonTierRecordsData {
  dungeonDefinitionId: string;
  difficulty: string;
  grade: string;
  records: DungeonRecordEntryData[];
}

export interface DungeonRecordsData {
  familyId: string;
  familyTitle: string;
  tiers: DungeonTierRecordsData[];
}
