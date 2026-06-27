import { GuildInvite } from './guildInvite';
import { GuildMember } from './guildMember';
import { GuildResource } from './guildResource';

export interface Guild {
  id: string;
  name: string;
  tag: string;
  description?: string;
  guildXp: number;
  guildLevel: number;
  members: GuildMember[];
  maxMembers: number;
  invites: GuildInvite[];
  resources: GuildResource[];
}

export interface GuildSimple {
  id: string;
  name: string;
  ownerName: string;
  memberCount: number;
  maxMembers: number;
  upgrades: number;
}
