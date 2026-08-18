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

  resolveCurrentAction(): Observable<CharacterActionDto | null> {
    return this.api
      .post('CharacterActions/Resolve', {}, { forceStateSyncRefresh: false })
      .pipe(
        map((response) =>
          this.unwrapNullableResponse<CharacterActionDto>(response),
        ),
      );
  }

  startCombat(data: StartCombatActionRequest): Observable<CharacterActionDto> {
    return this.api
      .post('CharacterActions/StartCombat', data)
      .pipe(map((response) => this.unwrapResponse<CharacterActionDto>(response)));
  }

  startCrafting(data: StartCraftingActionRequest): Observable<boolean> {
    return this.api.post('CharacterActions/StartCrafting', data);
  }

  resumeTempering(): Observable<CharacterActionDto> {
    return this.api
      .post('CharacterActions/ResumeTempering', {})
      .pipe(map((response) => this.unwrapResponse<CharacterActionDto>(response)));
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

  private unwrapNullableResponse<T>(
    response: T | null | ApiResponse<T | null>,
  ): T | null {
    if (
      response &&
      typeof response === 'object' &&
      'isSuccess' in response &&
      'data' in response
    ) {
      const apiResponse = response as ApiResponse<T | null>;
      if (!apiResponse.isSuccess) {
        throw new Error(apiResponse.errorMessage ?? 'Request failed');
      }

      return apiResponse.data;
    }

    return response as T | null;
  }
}
