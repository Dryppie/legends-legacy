import { GuildRole } from './guildRole';

export interface GuildRolePermission {
  role: GuildRole;
  canInvite: boolean;
  canManageApplications: boolean;
  canPromoteDemote: boolean;
  canKick: boolean;
  canBorrowVault: boolean;
}
