import { GuildBuildingType } from './guildBuilding';
import { GuildRole } from './guildRole';

export interface GuildPublic {
  id: string;
  name: string;
  tag: string;
  maxMembers: number;
  members: {
    characterId: string;
    name: string;
    level: number;
    role: GuildRole;
  }[];
  buildings: { type: GuildBuildingType; level: number }[];
}
