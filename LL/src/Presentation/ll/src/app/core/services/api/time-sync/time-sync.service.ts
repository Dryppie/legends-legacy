import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class TimeSyncService {
  private serverOffset = 0;

  constructor(private api: ApiService) {}

  sync(): Observable<void> {
    const clientSend = Date.now();
    return this.api.get('timesync').pipe(
      map((res) => {
        const clientReceive = Date.now();
        const serverTime = new Date(res).getTime();
        const latency = (clientReceive - clientSend) / 2;
        this.serverOffset = serverTime + latency - clientReceive;
      }),
      map(() => void 0),
    );
  }

  now(): number {
    return Date.now() + this.serverOffset;
  }
}
