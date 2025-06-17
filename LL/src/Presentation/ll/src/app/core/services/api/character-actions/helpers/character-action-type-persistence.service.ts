import { Injectable } from '@angular/core';
import { NamedStorageKeys } from '../../../../common/enums/named-storage-keys';
import { CharacterActionType } from '../../../../../shared/models/enums/characterActionType';

@Injectable({ providedIn: 'root' })
export class CharacterActionTypePersistenceService {
  private readonly key = NamedStorageKeys.CharacterActionType;

  set(type: CharacterActionType): void {
    localStorage.setItem(this.key, type);
  }

  get(): CharacterActionType | null {
    return localStorage.getItem(this.key) as CharacterActionType | null;
  }

  clear(): void {
    localStorage.removeItem(this.key);
  }
}
