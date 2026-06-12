import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../api/api.service';
import {
  DismantleEssenceResultDto,
  EssenceCatalogDto,
  EssenceLoadoutDto,
  EssenceLoadoutsDto,
  ResponseMessageDto,
  SaveEssenceLoadoutDto,
  SoulArchiveDto,
  SpendEssenceDustResultDto,
} from '../../../../shared/models/essence-system';

@Injectable({
  providedIn: 'root',
})
export class EssencesService {
  constructor(private apiService: ApiService) {}

  public getCatalog(): Observable<EssenceCatalogDto> {
    return this.apiService.get('essence/catalog');
  }

  public getArchive(): Observable<SoulArchiveDto> {
    return this.apiService.get('essence/archive');
  }

  public getLoadouts(): Observable<EssenceLoadoutsDto> {
    return this.apiService.get('essence/loadouts');
  }

  public getActiveLoadout(): Observable<EssenceLoadoutDto | null> {
    return this.apiService.get('essence/loadouts/active');
  }

  public absorb(inventoryItemId: string): Observable<ResponseMessageDto> {
    return this.apiService.post(`essence/items/${inventoryItemId}/absorb`, {});
  }

  public dismantle(
    inventoryItemId: string,
  ): Observable<DismantleEssenceResultDto> {
    return this.apiService.post(`essence/items/${inventoryItemId}/dismantle`, {});
  }

  public spendDust(
    playerEssenceId: string,
    dustAmount: number,
  ): Observable<SpendEssenceDustResultDto> {
    return this.apiService.post(`essence/${playerEssenceId}/spend-dust`, {
      dustAmount,
    });
  }

  public ascend(playerEssenceId: string): Observable<ResponseMessageDto> {
    return this.apiService.post(`essence/${playerEssenceId}/ascend`, {});
  }

  public evolve(playerEssenceId: string): Observable<ResponseMessageDto> {
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
