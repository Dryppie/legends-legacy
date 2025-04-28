import { GuildMember } from './guildMember';

export interface Guild {
  id: string;
  name: string;
  tag: string;
  description?: string;
  members: GuildMember[];
}
