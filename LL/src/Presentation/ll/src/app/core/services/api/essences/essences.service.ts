import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../api/api.service';
import {
  CreatureArchiveDto,
  EssenceLoadoutDto,
  EssenceLoadoutsDto,
  EssenceCodexDto,
  EssenceMutationResponseDto,
  ResponseMessageDto,
  SaveEssenceLoadoutDto,
  SoulArchiveDto,
} from '../../../../shared/models/essence-system';

@Injectable({
  providedIn: 'root',
})
export class EssencesService {
  constructor(private apiService: ApiService) {}

  public getArchive(): Observable<SoulArchiveDto> {
    return this.apiService.get('essence/archive');
  }

  public getLoadouts(): Observable<EssenceLoadoutsDto> {
    return this.apiService.get('essence/loadouts');
  }

  public getCreatureArchive(): Observable<CreatureArchiveDto> {
    return this.apiService.get('essence/creatures');
  }

  public setEssenceFocus(
    creatureId: string | null,
  ): Observable<CreatureArchiveDto> {
    return this.apiService.post('essence/creatures/focus', { creatureId });
  }

  public getCodex(): Observable<EssenceCodexDto> {
    return this.apiService.get('essence/codex');
  }

  public getActiveLoadout(): Observable<EssenceLoadoutDto | null> {
    return this.apiService.get('essence/loadouts/active');
  }

  public absorb(inventoryItemId: string): Observable<EssenceMutationResponseDto> {
    return this.apiService.post(`essence/items/${inventoryItemId}/absorb`, {});
  }

  public dismantle(
    inventoryItemId: string,
  ): Observable<EssenceMutationResponseDto> {
    return this.apiService.post(`essence/items/${inventoryItemId}/dismantle`, {});
  }

  public spendDust(
    playerEssenceId: string,
    dustAmount: number,
  ): Observable<EssenceMutationResponseDto> {
    return this.apiService.post(
      `essence/${playerEssenceId}/spend-dust`,
      { dustAmount },
      {
        // The response patches the changed archive/inventory state. These other
        // legacy Essence scopes are unchanged by leveling and need no refetch.
        stateSyncScopesHandledByResponse: [
          'essences',
          'inventory',
          'character',
          'equipment',
          'quests',
        ],
      },
    );
  }

  public ascend(playerEssenceId: string): Observable<EssenceMutationResponseDto> {
    return this.apiService.post(`essence/${playerEssenceId}/ascend`, {});
  }

  public evolve(playerEssenceId: string): Observable<EssenceMutationResponseDto> {
    return this.apiService.post(`essence/${playerEssenceId}/evolve`, {});
  }

  public setFavorite(
    playerEssenceId: string,
    isFavorite: boolean,
  ): Observable<ResponseMessageDto> {
    return this.apiService.post(`essence/${playerEssenceId}/favorite`, {
      isFavorite,
    });
  }

  public saveLoadout(
    request: SaveEssenceLoadoutDto,
  ): Observable<EssenceLoadoutDto> {
    return this.apiService.post('essence/loadouts', request);
  }

  public updateLoadout(
    loadoutId: string,
    request: SaveEssenceLoadoutDto,
  ): Observable<EssenceLoadoutDto> {
    return this.apiService.put(`essence/loadouts/${loadoutId}`, request);
  }

  public activateLoadout(
    loadoutId: string,
  ): Observable<ResponseMessageDto> {
    return this.apiService.post(`essence/loadouts/${loadoutId}/activate`, {});
  }

  public deleteLoadout(
    loadoutId: string,
  ): Observable<ResponseMessageDto> {
    return this.apiService.delete(`essence/loadouts/${loadoutId}`);
  }
}
