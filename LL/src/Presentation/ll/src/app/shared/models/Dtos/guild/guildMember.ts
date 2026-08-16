import { GuildRole } from './guildRole';

export interface GuildMember {
  characterId: string;
  name: string;
  level: number;
  role: GuildRole;
  joinedAt: string; // ISO
  isOnline: boolean;
  lastSeenAt?: string | null;
}
