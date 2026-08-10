import { EquipmentInstance } from '../../item';

export interface GuildVaultItem {
  id: string;
  equipment: EquipmentInstance;
  donatedByCharacterId: string;
  donatedByName: string;
  donatedAt: string;
  borrowedByCharacterId?: string | null;
  borrowedByName?: string | null;
  borrowedAt?: string | null;
}
