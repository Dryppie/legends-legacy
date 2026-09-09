import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CombatStyleOverview,
  CombatStyleSelectionRequest,
} from '../../../../shared/models/combat-styles';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class CombatStylesService {
  constructor(private readonly api: ApiService) {}

  get(): Observable<CombatStyleOverview> {
    return this.api.get('combat-styles');
  }

  preview(
    selection: CombatStyleSelectionRequest,
  ): Observable<CombatStyleOverview> {
    return this.api.post('combat-styles/preview', selection, {
      forceStateSyncRefresh: false,
    });
  }

  select(selection: CombatStyleSelectionRequest) {
    return this.api.putVersioned<CombatStyleOverview>(
      'combat-styles/selection',
      selection,
      {
        stateSyncScopesHandledByResponse: ['combat-styles'],
      },
    );
  }
}
