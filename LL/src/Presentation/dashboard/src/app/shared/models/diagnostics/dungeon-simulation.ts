export interface DungeonSimulationOptions {
  dungeons: DungeonSimulationDungeonOption[];
  essences: DungeonSimulationEssenceOption[];
  equipmentSlots: DungeonSimulationEquipmentSlotOption[];
  equipmentRarities: DungeonSimulationEquipmentRarityOption[];
}

export interface DungeonSimulationDungeonOption {
  id: string;
  familyId: string;
  name: string;
  difficulty: string;
  tier: number;
}

export interface DungeonSimulationEssenceOption {
  id: string;
  name: string;
}

export interface DungeonSimulationEquipmentSlotOption {
  id: string;
  name: string;
  attributeBonuses: Record<string, number>;
}

export interface DungeonSimulationEquipmentRarityOption {
  id: string;
  name: string;
  multiplier: number;
}

export interface DungeonSimulationEquipment {
  rarity: string;
  equippedSlots: string[];
}

export interface DungeonSimulationCharacter {
  name: string;
  level: number;
  maxHealth: number;
  power: number;
  fortitude: number;
  spirit: number;
  armor: number;
  resistance: number;
  precision: number;
  critChance: number;
  critDamage: number;
  attackSpeed: number;
  healthRegeneration: number;
  essenceIds: string[];
  equipment: DungeonSimulationEquipment;
}

export interface DungeonSimulationRequest {
  dungeonDefinitionId: string;
  runCount: number;
  randomSeed: number;
  masteryLevel: number;
  routeStrategy: 'Random' | 'Safest' | 'Hardest';
  character: DungeonSimulationCharacter;
}

export interface DungeonSimulationReport {
  dungeonDefinitionId: string;
  dungeonName: string;
  difficulty: string;
  tier: number;
  requestedRuns: number;
  completedRuns: number;
  failedRuns: number;
  clearRate: number;
  averageFinalVigor: number;
  averageRoomsCleared: number;
  randomSeed: number;
  routeStrategy: string;
  runs: DungeonSimulationRunResult[];
}

export interface DungeonSimulationRunResult {
  runNumber: number;
  seed: number;
  completed: boolean;
  outcome: string;
  finalVigor: number;
  roomsCleared: number;
  totalCombatTicks: number;
  rooms: DungeonSimulationRoomResult[];
}

export interface DungeonSimulationRoomResult {
  roomIndex: number;
  name: string;
  roomType: string;
  outcome: string;
  vigorBefore: number;
  vigorAfter: number;
  vigorChange: number;
  combatTicks: number;
  damageTaken: number;
  enemies: string[];
}
