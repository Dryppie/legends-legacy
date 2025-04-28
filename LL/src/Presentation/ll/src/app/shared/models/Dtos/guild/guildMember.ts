import { GuildRole } from './guildRole';

export interface GuildMember {
  characterId: string;
  role: GuildRole;
  joinedAt: string; // ISO
}
