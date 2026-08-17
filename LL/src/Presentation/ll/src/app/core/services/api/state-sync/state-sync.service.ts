import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { StateSyncCheckpoint } from '../../real-time/game-realtime/game-realtime-contracts';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class StateSyncService {
  constructor(private readonly api: ApiService) {}

  getCheckpoint(): Observable<StateSyncCheckpoint> {
    return this.api.get('StateSync/checkpoint');
  }
}
