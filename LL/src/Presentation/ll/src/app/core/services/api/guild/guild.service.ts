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
    this.api
      .post('guild/createGuild', name)
      .pipe(
        map((guild) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return guild;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to create guild'));
        }),
      )
      .subscribe(() => {
        this.getMyGuild();
      });
  }

  getMyGuild() {
    this.api
      .get('guild/getMyGuild')
      .pipe(
        map((guild) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return guild;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to get guild'));
        }),
      )
      .subscribe((guild) => {
        if (guild) {
          this._guild$.next(guild);
          return;
        }

        this.getAllGuilds();
        this.getMyInvites();
      });
  }

  getAllGuilds() {
    this.api
      .get('guild/getAllGuilds')
      .pipe(
        map((guilds) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return guilds;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to get guilds'));
        }),
      )
      .subscribe((guilds) => {
        this._allGuilds$.next(guilds);
      });
  }

  applyToGuild(guildId: string) {
    this.api
      .post('guild/applyToGuild', guildId)
      .pipe(
        map((opponents) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return opponents;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to apply to guild'));
        }),
      )
      .subscribe(() => {
        const invite: GuildInvite = {
          guildId: guildId,
          guildName: '',
          characterId: '',
          characterName: '',
          isInvite: false,
        };
        const currentInvites = this._invites$.value;
        this._invites$.next([...currentInvites, invite]);
      });
  }

  invite(inviteToGuild: InviteToGuild): Observable<void> {
    return this.api.post('guild/invite', inviteToGuild).pipe(
      map((opponents) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return opponents;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to invite character'));
      }),
    );
  }

  inviteCharacterByName(inviteToGuild: InviteToGuild) {
    this.api
      .post('guild/inviteCharacterByName', inviteToGuild)
      .pipe(
        map((opponents) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return opponents;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(
            () => new Error('Failed to invite character by name'),
          );
        }),
      )
      .subscribe();
  }

  getMyInvites() {
    this.api
      .get('guild/getMyinvites')
      .pipe(
        map((opponents) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return opponents;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to get guild invites'));
        }),
      )
      .subscribe((invites) => {
        this._invites$.next(invites);
      });
  }

  acceptInvite(guildId: string) {
    this.api
      .post('guild/acceptInvite', guildId)
      .pipe(
        map((opponents) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return opponents;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to accept guild invite'));
        }),
      )
      .subscribe(() => {
        this.getMyGuild();
      });
  }

  rejectInvite(guildId: string) {
    this.api
      .post('guild/rejectInvite', guildId)
      .pipe(
        map((guild) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return guild;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to reject guild invite'));
        }),
      )
      .subscribe(() => {
        const filteredInvites = this._invites$.value.filter(
          (i) => i.guildId !== guildId,
        );
        this._invites$.next(filteredInvites);
      });
  }

  approveApplication(characterId: string) {
    this.api
      .post('guild/approveApplication', characterId)
      .pipe(
        map((guild) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return guild;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to approve application'));
        }),
      )
      .subscribe(() => {
        this.getMyGuild();
      });
  }

  rejectApplication(characterId: string) {
    this.api
      .post('guild/rejectApplication', characterId)
      .pipe(
        map((guild) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return guild;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to reject application'));
        }),
      )
      .subscribe(() => {
        let currentGuild = this._guild$.value;
        if (!currentGuild) return;
        currentGuild.invites = currentGuild.invites.filter(
          (i) => i.characterId !== characterId,
        );
        this._guild$.next(currentGuild);
      });
  }

  leave() {
    this.api
      .post('guild/leaveGuild')
      .pipe(
        map((opponents) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return opponents;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to leave guild'));
        }),
      )
      .subscribe(() => {
        this._guild$.next(null);
        this.getAllGuilds();
        this.getMyInvites();
      });
  }

  disband() {
    this.api
      .post('guild/disbandGuild')
      .pipe(
        map((opponents) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
          return opponents;
        }),

        catchError(() => {
          // this.toastService.showToast(
          //   'Login Failed',
          //   'Wrong email or password',
          //   'error',
          //   't',
          // );
          return throwError(() => new Error('Failed to disband guild'));
        }),
      )
      .subscribe(() => {
        this._guild$.next(null);
        this.getAllGuilds();
        this.getMyInvites();
      });
  }
}
