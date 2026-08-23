import { InventoryItem } from '../inventoryItem';
import { CharacterActionType } from '../enums/characterActionType';

export interface TemperingQueueMutationResponse {
  removedInventoryItemIds: string[];
  returnedInventoryItems: InventoryItem[];
  removedQueueItemIds: string[];
  addedQueueItemId?: string | null;
  action?: TemperingActionState | null;
}

export interface TemperingActionState {
  characterActionType: CharacterActionType;
  updatedAt: Date;
  nextResolutionAtUtc?: Date | null;
  blockedUntilUtc?: Date | null;
  scheduleGeneration: number;
  isDeleted: boolean;
  resolutionIntervalMs?: number | null;
  revision: string;
}
