import { GuildRole } from './guildRole';

export interface GuildMember {
  characterId: string;
  name: string;
  role: GuildRole;
  joinedAt: string; // ISO
}
