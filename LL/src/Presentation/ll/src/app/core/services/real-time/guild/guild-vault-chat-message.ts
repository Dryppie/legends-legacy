import { EquipmentInstance } from '../../../../shared/models/item';

export interface GuildVaultChatMessageMsg {
  guildId: string;
  messageId: string;
  actorCharacterId: string;
  actorName: string;
  action: 'donated' | 'withdrew';
  equipment: EquipmentInstance;
  sentAt: string;
}
