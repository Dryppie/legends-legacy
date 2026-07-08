import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from '../../api/api.service';
import {
  CharacterActionDto,
  StartCombatActionRequest,
  StartCraftingActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { ApiResponse } from '../../../../shared/models/response';

@Injectable({ providedIn: 'root' })
export class CharacterActionsService {
  constructor(private readonly api: ApiService) {}

  getCurrentAction(): Observable<CharacterActionDto | null> {
    return this.api.get('CharacterActions');
  }

  startCombat(data: StartCombatActionRequest): Observable<CharacterActionDto> {
    return this.api
      .post('CharacterActions/StartCombat', data)
      .pipe(map((response) => this.unwrapResponse<CharacterActionDto>(response)));
  }

  startCrafting(data: StartCraftingActionRequest): Observable<boolean> {
    return this.api.post('CharacterActions/StartCrafting', data);
  }

  stop(): Observable<void> {
    return this.api.delete('CharacterActions');
  }

  private unwrapResponse<T>(response: T | ApiResponse<T>): T {
    if (
      response &&
      typeof response === 'object' &&
      'isSuccess' in response &&
      'data' in response
    ) {
      const apiResponse = response as ApiResponse<T>;
      if (!apiResponse.isSuccess || apiResponse.data == null) {
        throw new Error(apiResponse.errorMessage ?? 'Request failed');
      }

      return apiResponse.data;
    }

    return response as T;
  }
}
