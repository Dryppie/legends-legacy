import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService, VersionedMutationResult } from '../../api/api.service';
import {
  CreatureArchiveDto,
  EssenceCombatActivity,
  EssenceLoadoutDto,
  EssenceLoadoutsDto,
  EssenceCodexDto,
  EssenceMutationResponseDto,
  EssenceStateResponseDto,
  SaveEssenceLoadoutDto,
  SoulArchiveDto,
} from '../../../../shared/models/essence-system';

const ESSENCE_MUTATION_HANDLED_SCOPES = [
  'essences',
  'inventory',
  'equipment',
] as const;
const ESSENCE_STATE_HANDLED_SCOPES = ['essences'] as const;

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
  ): Observable<VersionedMutationResult<EssenceStateResponseDto>> {
    return this.apiService.postVersioned<EssenceStateResponseDto>(
      'essence/creatures/focus',
      { creatureId },
      { stateSyncScopesHandledByResponse: ESSENCE_STATE_HANDLED_SCOPES },
    );
  }

  public getCodex(): Observable<EssenceCodexDto> {
    return this.apiService.get('essence/codex');
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
    quantity = 1,
  ): Observable<VersionedMutationResult<EssenceMutationResponseDto>> {
    return this.apiService.postVersioned<EssenceMutationResponseDto>(
      `essence/items/${inventoryItemId}/dismantle`,
      { quantity },
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
  ): Observable<VersionedMutationResult<EssenceStateResponseDto>> {
    return this.apiService.postVersioned<EssenceStateResponseDto>(
      `essence/${playerEssenceId}/favorite`,
      { isFavorite },
      { stateSyncScopesHandledByResponse: ESSENCE_STATE_HANDLED_SCOPES },
    );
  }

  public saveLoadout(
    request: SaveEssenceLoadoutDto,
  ): Observable<VersionedMutationResult<EssenceStateResponseDto>> {
    return this.apiService.postVersioned<EssenceStateResponseDto>(
      'essence/loadouts',
      request,
      { stateSyncScopesHandledByResponse: ESSENCE_STATE_HANDLED_SCOPES },
    );
  }

  public updateLoadout(
    loadoutId: string,
    request: SaveEssenceLoadoutDto,
  ): Observable<VersionedMutationResult<EssenceStateResponseDto>> {
    return this.apiService.putVersioned<EssenceStateResponseDto>(
      `essence/loadouts/${loadoutId}`,
      request,
      { stateSyncScopesHandledByResponse: ESSENCE_STATE_HANDLED_SCOPES },
    );
  }

  public setLoadoutAutoUseActivities(
    loadoutId: string,
    activities: readonly EssenceCombatActivity[],
  ): Observable<VersionedMutationResult<EssenceStateResponseDto>> {
    return this.apiService.putVersioned<EssenceStateResponseDto>(
      `essence/loadouts/${loadoutId}/auto-use`,
      { activities },
      { stateSyncScopesHandledByResponse: ESSENCE_STATE_HANDLED_SCOPES },
    );
  }

  public deleteLoadout(
    loadoutId: string,
  ): Observable<VersionedMutationResult<EssenceStateResponseDto>> {
    return this.apiService.deleteVersioned<EssenceStateResponseDto>(
      `essence/loadouts/${loadoutId}`,
      {},
      { stateSyncScopesHandledByResponse: ESSENCE_STATE_HANDLED_SCOPES },
    );
  }
}
