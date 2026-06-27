import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  ActivateCombatStyleResponseDto,
  CombatBuildPreviewDto,
  CombatStyleDto,
  CombatStyleMutationResponseDto,
  CombatStylesOverviewDto,
} from '../../../../shared/models/combat-style';

@Injectable({
  providedIn: 'root',
})
export class CombatStyleService {
  constructor(private readonly api: ApiService) {}

  getCombatStyles(): Observable<CombatStylesOverviewDto> {
    return this.api.get('combat-styles');
  }

  activateStyle(
    styleId: string,
  ): Observable<ActivateCombatStyleResponseDto> {
    return this.api.post(`combat-styles/${styleId}/activate`);
  }

  selectFocus(
    styleId: string,
    focusId: string,
  ): Observable<CombatStyleDto> {
    return this.api.post(`combat-styles/${styleId}/focus/${focusId}/select`);
  }

  rankUpNode(
    styleId: string,
    nodeId: string,
  ): Observable<CombatStyleMutationResponseDto> {
    return this.api.post(`combat-styles/${styleId}/tree/nodes/${nodeId}/rank-up`);
  }

  resetTree(styleId: string): Observable<CombatStyleMutationResponseDto> {
    return this.api.post(`combat-styles/${styleId}/tree/reset`);
  }

  getBuildPreview(): Observable<CombatBuildPreviewDto> {
    return this.api.get('combat-styles/build-preview');
  }
}
