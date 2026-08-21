import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService, VersionedMutationResult } from '../api.service';
import {
  AchievementCategory,
  AchievementDto,
  AchievementOverviewDto,
  EquippedTitleDto,
  TitleDto,
  TitleDisplayPosition,
  TitleRarity,
} from '../../../../shared/models/achievement';

@Injectable({
  providedIn: 'root',
})
export class AchievementService {
  constructor(private readonly api: ApiService) {}

  getOverview(): Observable<AchievementOverviewDto> {
    return this.api.get('Achievements/overview');
  }

  getAchievements(
    category?: AchievementCategory,
  ): Observable<AchievementDto[]> {
    let params = new HttpParams();
    if (category) {
      params = params.set('category', category);
    }

    return this.api.get('Achievements', params);
  }

  getTitles(
    filters: {
      category?: AchievementCategory;
      rarity?: TitleRarity;
      unlocked?: boolean;
    } = {},
  ): Observable<TitleDto[]> {
    let params = new HttpParams();
    if (filters.category) {
      params = params.set('category', filters.category);
    }
    if (filters.rarity) {
      params = params.set('rarity', filters.rarity);
    }
    if (filters.unlocked !== undefined) {
      params = params.set('unlocked', filters.unlocked);
    }

    return this.api.get('Titles', params);
  }

  equipTitle(
    titleKey: string,
    displayPosition: TitleDisplayPosition,
  ): Observable<VersionedMutationResult<EquippedTitleDto>> {
    return this.api.postVersioned<EquippedTitleDto>(
      'Titles/equip',
      { titleKey, displayPosition },
      { stateSyncScopesHandledByResponse: ['achievements', 'character'] },
    );
  }

  unequipTitle(): Observable<VersionedMutationResult<EquippedTitleDto | null>> {
    return this.api.postVersioned<EquippedTitleDto | null>(
      'Titles/unequip',
      {},
      {
        stateSyncScopesHandledByResponse: ['achievements', 'character'],
      },
    );
  }
}
