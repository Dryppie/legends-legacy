import { GuildInvite } from './guildInvite';
import { GuildMember } from './guildMember';

export interface Guild {
  id: string;
  name: string;
  tag: string;
  description?: string;
  members: GuildMember[];
  maxMembers: number;
  invites: GuildInvite[];
}

export interface GuildSimple {
  id: string;
  name: string;
  ownerName: string;
  memberCount: number;
  maxMembers: number;
}
