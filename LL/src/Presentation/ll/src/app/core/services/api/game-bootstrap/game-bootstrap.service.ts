import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { ApiResponse } from '../../../../shared/models/response';
import { CombatAreaAccess, QuestJournal } from '../../../../shared/models/quest';
import { AttributeDefinition } from '../../../../shared/models/attribute-definition';
import { ApiService } from '../api.service';
import { StateVersionMap } from '../../real-time/game-realtime/game-realtime-contracts';

export interface GameBootstrapDto {
  character: CharacterDto;
  questJournal: QuestJournal;
  areaAccess: CombatAreaAccess[];
  currentAction: CharacterActionDto | null;
  serverTimeUtc: string;
  attributeDefinitions: AttributeDefinition[];
  accountAccess: AccountAccessDto;
  stateVersions: StateVersionMap;
}

export interface AccountAccessDto {
  canParticipate: boolean;
  isPubliclyEligible: boolean;
  restrictionCode: string | null;
  expiresAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class GameBootstrapService {
  constructor(private readonly api: ApiService) {}

  get(): Observable<GameBootstrapDto> {
    return this.api
      .get('GameBootstrap')
      .pipe(map((response) => this.unwrapResponse<GameBootstrapDto>(response)));
  }

  private unwrapResponse<T>(response: T | ApiResponse<T>): T {
    if (
      response &&
      typeof response === 'object' &&
      'isSuccess' in response &&
      'data' in response
    ) {
      const apiResponse = response as ApiResponse<T>;
      if (!apiResponse.isSuccess) {
        throw new Error(apiResponse.errorMessage ?? 'Request failed');
      }

      if (apiResponse.data == null) {
        throw new Error('Response did not include data');
      }

      return apiResponse.data;
    }

    return response as T;
  }
}
