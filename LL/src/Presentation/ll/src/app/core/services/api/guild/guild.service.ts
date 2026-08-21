import { Injectable } from '@angular/core';
import { ApiService, VersionedMutationResult } from '../api.service';
import { catchError, map, Observable, throwError } from 'rxjs';
import { InviteToGuild } from '../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { GuildMissionOverview } from '../../../../shared/models/Dtos/guild/guildMission';
import {
  GuildShopOverview,
  GuildShopPurchaseResponse,
} from '../../../../shared/models/Dtos/guild/guildShop';
import {
  GuildBuildingOverview,
  GuildBuildingType,
} from '../../../../shared/models/Dtos/guild/guildBuilding';
import { GuildRole } from '../../../../shared/models/Dtos/guild/guildRole';
import { GuildRolePermission } from '../../../../shared/models/Dtos/guild/guildRolePermission';

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

  selectMission(
    missionOptionId: string,
  ): Observable<VersionedMutationResult<GuildMissionOverview>> {
    return this.api
      .postVersioned<GuildMissionOverview>(
        'guild/selectMission',
        missionOptionId,
        {
          stateSyncScopesHandledByResponse: ['guild-missions'],
        },
      )
      .pipe(
        map((missions) => {
          return missions;
        }),

        catchError(() => {
          return throwError(() => new Error('Failed to select guild mission'));
        }),
      );
  }

  claimOrderReward(
    orderId: string,
  ): Observable<VersionedMutationResult<GuildMissionOverview>> {
    return this.api
      .postVersioned<GuildMissionOverview>('guild/claimOrderReward', orderId, {
        stateSyncScopesHandledByResponse: ['guild-missions'],
      })
      .pipe(
        map((missions) => {
          return missions;
        }),

        catchError(() => {
          return throwError(() => new Error('Failed to claim guild order'));
        }),
      );
  }

  claimWeeklyMissionReward(): Observable<
    VersionedMutationResult<GuildMissionOverview>
  > {
    return this.api
      .postVersioned<GuildMissionOverview>(
        'guild/claimWeeklyMissionReward',
        {},
        {
          stateSyncScopesHandledByResponse: ['guild-missions'],
        },
      )
      .pipe(
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

  purchaseShopItem(
    itemKey: string,
  ): Observable<VersionedMutationResult<GuildShopPurchaseResponse>> {
    return this.api
      .postVersioned<GuildShopPurchaseResponse>(
        'guild/purchaseShopItem',
        itemKey,
        {
          stateSyncScopesHandledByResponse: ['guild-shop', 'inventory'],
        },
      )
      .pipe(
        map((shop) => {
          return shop;
        }),

        catchError(() => {
          return throwError(() => new Error('Failed to purchase guild item'));
        }),
      );
  }

  donateVaultItem(equipmentInstanceId: string): Observable<void> {
    return this.api.post('guild/donateVaultItem', equipmentInstanceId);
  }

  borrowVaultItem(vaultItemId: string): Observable<void> {
    return this.api.post('guild/borrowVaultItem', vaultItemId);
  }

  returnVaultItem(vaultItemId: string): Observable<void> {
    return this.api.post('guild/returnVaultItem', vaultItemId);
  }

  withdrawVaultItem(vaultItemId: string): Observable<void> {
    return this.api.post('guild/withdrawVaultItem', vaultItemId);
  }

  changeMemberRole(characterId: string, role: GuildRole): Observable<void> {
    return this.api.post('guild/changeMemberRole', { characterId, role });
  }

  kickMember(characterId: string): Observable<void> {
    return this.api.post('guild/kickMember', characterId);
  }

  updateRolePermissions(permissions: GuildRolePermission): Observable<void> {
    return this.api.post('guild/updateRolePermissions', permissions);
  }

  updateDescription(description: string): Observable<void> {
    return this.api.post('guild/updateDescription', { description });
  }

  constructBuilding(
    buildingType: GuildBuildingType,
  ): Observable<VersionedMutationResult<GuildBuildingOverview>> {
    return this.api
      .postVersioned<GuildBuildingOverview>(
        'guild/constructBuilding',
        buildingType,
        {
          stateSyncScopesHandledByResponse: ['guild-buildings'],
        },
      )
      .pipe(
        map((buildings) => {
          return buildings;
        }),

        catchError(() => {
          return throwError(() => new Error('Failed to construct building'));
        }),
      );
  }

  upgradeBuilding(
    id: string,
  ): Observable<VersionedMutationResult<GuildBuildingOverview>> {
    return this.api
      .postVersioned<GuildBuildingOverview>('guild/upgradeBuilding', id, {
        stateSyncScopesHandledByResponse: ['guild-buildings'],
      })
      .pipe(
        map((buildings) => {
          return buildings;
        }),

        catchError(() => {
          return throwError(() => new Error('Failed to upgrade building'));
        }),
      );
  }

  setBuildingTarget(
    buildingType: GuildBuildingType,
  ): Observable<VersionedMutationResult<GuildBuildingOverview>> {
    return this.api
      .postVersioned<GuildBuildingOverview>(
        'guild/setBuildingTarget',
        buildingType,
        {
          stateSyncScopesHandledByResponse: ['guild-buildings'],
        },
      )
      .pipe(
        catchError(() => {
          return throwError(() => new Error('Failed to set building target'));
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
