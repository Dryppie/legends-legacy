import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { catchError, map, Observable, throwError } from 'rxjs';
import { InviteToGuild } from '../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { GuildMissionOverview } from '../../../../shared/models/Dtos/guild/guildMission';
import { GuildShopOverview } from '../../../../shared/models/Dtos/guild/guildShop';
import {
  GuildBuildingOverview,
  GuildBuildingType,
} from '../../../../shared/models/Dtos/guild/guildBuilding';

@Injectable({
  providedIn: 'root',
})
export class GuildService {
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

  getBuildings(): Observable<GuildBuildingOverview | null> {
    return this.api.get('guild/getBuildings').pipe(
      map((buildings) => {
        return buildings;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get guild buildings'));
      }),
    );
  }

  getMissions(): Observable<GuildMissionOverview | null> {
    return this.api.get('guild/getMissions').pipe(
      map((missions) => {
        return missions;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get guild missions'));
      }),
    );
  }

  selectMission(missionOptionId: string): Observable<GuildMissionOverview> {
    return this.api.post('guild/selectMission', missionOptionId).pipe(
      map((missions) => {
        return missions;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to select guild mission'));
      }),
    );
  }

  claimOrderReward(orderId: string): Observable<GuildMissionOverview> {
    return this.api.post('guild/claimOrderReward', orderId).pipe(
      map((missions) => {
        return missions;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to claim guild order'));
      }),
    );
  }

  claimWeeklyMissionReward(): Observable<GuildMissionOverview> {
    return this.api.post('guild/claimWeeklyMissionReward').pipe(
      map((missions) => {
        return missions;
      }),

      catchError(() => {
        return throwError(
          () => new Error('Failed to claim weekly guild mission reward'),
        );
      }),
    );
  }

  getShop(): Observable<GuildShopOverview | null> {
    return this.api.get('guild/getShop').pipe(
      map((shop) => {
        return shop;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to get guild shop'));
      }),
    );
  }

  purchaseShopItem(itemKey: string): Observable<GuildShopOverview> {
    return this.api.post('guild/purchaseShopItem', itemKey).pipe(
      map((shop) => {
        return shop;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to purchase guild item'));
      }),
    );
  }

  constructBuilding(buildingType: GuildBuildingType): Observable<GuildBuildingOverview> {
    return this.api.post('guild/constructBuilding', buildingType).pipe(
      map((buildings) => {
        return buildings;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to construct building'));
      }),
    );
  }

  upgradeBuilding(id: string): Observable<GuildBuildingOverview> {
    return this.api.post('guild/upgradeBuilding', id).pipe(
      map((buildings) => {
        return buildings;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to upgrade building'));
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
