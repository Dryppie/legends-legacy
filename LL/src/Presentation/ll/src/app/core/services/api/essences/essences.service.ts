import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, VersionedMutationResult } from '../../api/api.service';
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

const ESSENCE_MUTATION_HANDLED_SCOPES = [
  'essences',
  'inventory',
  'equipment',
] as const;

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

  public absorb(
    inventoryItemId: string,
  ): Observable<VersionedMutationResult<EssenceMutationResponseDto>> {
    return this.apiService.postVersioned<EssenceMutationResponseDto>(
      `essence/items/${inventoryItemId}/absorb`,
      {},
      { stateSyncScopesHandledByResponse: ESSENCE_MUTATION_HANDLED_SCOPES },
    );
  }

  public dismantle(
    inventoryItemId: string,
  ): Observable<VersionedMutationResult<EssenceMutationResponseDto>> {
    return this.apiService.postVersioned<EssenceMutationResponseDto>(
      `essence/items/${inventoryItemId}/dismantle`,
      {},
      { stateSyncScopesHandledByResponse: ESSENCE_MUTATION_HANDLED_SCOPES },
    );
  }

  public spendDust(
    playerEssenceId: string,
    dustAmount: number,
  ): Observable<VersionedMutationResult<EssenceMutationResponseDto>> {
    return this.apiService.postVersioned<EssenceMutationResponseDto>(
      `essence/${playerEssenceId}/spend-dust`,
      { dustAmount },
      {
        stateSyncScopesHandledByResponse: ESSENCE_MUTATION_HANDLED_SCOPES,
      },
    );
  }

  public ascend(
    playerEssenceId: string,
  ): Observable<VersionedMutationResult<EssenceMutationResponseDto>> {
    return this.apiService.postVersioned<EssenceMutationResponseDto>(
      `essence/${playerEssenceId}/ascend`,
      {},
      { stateSyncScopesHandledByResponse: ESSENCE_MUTATION_HANDLED_SCOPES },
    );
  }

  public evolve(
    playerEssenceId: string,
  ): Observable<VersionedMutationResult<EssenceMutationResponseDto>> {
    return this.apiService.postVersioned<EssenceMutationResponseDto>(
      `essence/${playerEssenceId}/evolve`,
      {},
      { stateSyncScopesHandledByResponse: ESSENCE_MUTATION_HANDLED_SCOPES },
    );
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

  public activateLoadout(loadoutId: string): Observable<ResponseMessageDto> {
    return this.apiService.post(`essence/loadouts/${loadoutId}/activate`, {});
  }

  public deleteLoadout(loadoutId: string): Observable<ResponseMessageDto> {
    return this.apiService.delete(`essence/loadouts/${loadoutId}`);
  }
}
