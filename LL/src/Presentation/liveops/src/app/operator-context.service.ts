import { Injectable } from '@angular/core';
import { OperatorSession } from './liveops.models';

@Injectable({ providedIn: 'root' })
export class OperatorContextService {
  readonly permissions = {
    read: 'liveops.read',
    account: 'liveops.accounts.moderate',
    chat: 'liveops.chat.moderate',
    economy: 'liveops.economy.compensate',
    superadmin: 'liveops.superadmin',
  };

  session: OperatorSession | null = null;

  hasPermission(permission: string): boolean {
    if (!this.session) return false;
    const requested = permission.toLowerCase();
    const superadmin = this.permissions.superadmin.toLowerCase();
    return this.session.permissions.some((value) => {
      const granted = value.toLowerCase();
      return granted === requested || granted === superadmin;
    });
  }
}
