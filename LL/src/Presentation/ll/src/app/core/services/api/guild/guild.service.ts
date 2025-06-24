import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { BehaviorSubject, catchError, map, Observable, throwError } from 'rxjs';
import { Guild, GuildSimple } from '../../../../shared/models/Dtos/guild/guild';
import { GuildInvite } from '../../../../shared/models/Dtos/guild/guildInvite';
import { InviteToGuild } from '../../../../shared/models/requestDtos/guilds/inviteToGuild';

@Injectable({
  providedIn: 'root',
})
export class GuildService {
  private _guild$ = new BehaviorSubject<Guild | null>(null);
  private _invites$ = new BehaviorSubject<GuildInvite[]>([]);
  private _allGuilds$ = new BehaviorSubject<GuildSimple[]>([]);

  readonly guild$ = this._guild$.asObservable();
  readonly invites$ = this._invites$.asObservable();
  readonly allGuilds$ = this._allGuilds$.asObservable();

  constructor(private api: ApiService) {}

  create(name: string) {
    return this.api.post('guild/createGuild', name).pipe(
      map((guild) => {
        return guild;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to create guild'));
      }),
    );
  }

  getMyGuild() {
    return this.api.get('guild/getMyGuild').pipe(
      map((guild) => {
        return guild;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get guild'));
      }),
    );
  }

  getAllGuilds() {
    return this.api.get('guild/getAllGuilds').pipe(
      map((guilds) => {
        return guilds;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get guilds'));
      }),
    );
  }

  getUpgrades() {
    return this.api.get('guild/getUpgrades').pipe(
      map((upgrades) => {
        return upgrades;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get guild upgrades'));
      }),
    );
  }

  applyToGuild(guildId: string) {
    return this.api.post('guild/applyToGuild', guildId).pipe(
      map((opponents) => {
        return opponents;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to apply to guild'));
      }),
    );
  }

  invite(inviteToGuild: InviteToGuild): Observable<void> {
    return this.api.post('guild/invite', inviteToGuild).pipe(
      map((opponents) => {
        return opponents;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to invite character'));
      }),
    );
  }

  inviteCharacterByName(inviteToGuild: InviteToGuild) {
    return this.api.post('guild/inviteCharacterByName', inviteToGuild).pipe(
      map((opponents) => {
        return opponents;
      }),

      catchError(() => {
        return throwError(
          () => new Error('Failed to invite character by name'),
        );
      }),
    );
  }

  getMyInvites() {
    return this.api.get('guild/getMyinvites').pipe(
      map((opponents) => {
        return opponents;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get guild invites'));
      }),
    );
  }

  acceptInvite(guildId: string) {
    return this.api.post('guild/acceptInvite', guildId).pipe(
      map((opponents) => {
        return opponents;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to accept guild invite'));
      }),
    );
  }

  rejectInvite(guildId: string) {
    return this.api.post('guild/rejectInvite', guildId).pipe(
      map((guild) => {
        return guild;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to reject guild invite'));
      }),
    );
  }

  approveApplication(characterId: string) {
    return this.api.post('guild/approveApplication', characterId).pipe(
      map((guild) => {
        return guild;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to approve application'));
      }),
    );
  }

  rejectApplication(characterId: string) {
    return this.api.post('guild/rejectApplication', characterId).pipe(
      map((guild) => {
        return guild;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to reject application'));
      }),
    );
  }

  leave() {
    return this.api.post('guild/leaveGuild').pipe(
      map((opponents) => {
        return opponents;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to leave guild'));
      }),
    );
  }

  disband() {
    return this.api.post('guild/disbandGuild').pipe(
      map((opponents) => {
        return opponents;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to disband guild'));
      }),
    );
  }
}
