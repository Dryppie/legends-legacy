import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LootHistoryEntry } from '../../../../shared/models/loot-history';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class LootHistoryService {
  constructor(private readonly api: ApiService) {}

  getRecent(): Observable<LootHistoryEntry[]> {
    return this.api.get('LootHistory');
  }

  clear(): Observable<number> {
    return this.api.delete('LootHistory');
  }
}
