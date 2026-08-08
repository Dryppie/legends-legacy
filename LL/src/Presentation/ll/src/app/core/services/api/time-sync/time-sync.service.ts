import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class TimeSyncService {
  private serverOffset = 0;

  constructor(private api: ApiService) {}

  sync(): Observable<void> {
    const clientSend = Date.now();
    const params = new HttpParams().set('_', clientSend.toString());

    return this.api.get('timesync', params).pipe(
      map((res) => {
        const clientReceive = Date.now();
        this.updateOffset(res, clientSend, clientReceive);
      }),
      map(() => void 0),
    );
  }

  updateFromServerTime(serverTimeUtc: string | number | Date): void {
    const clientNow = Date.now();
    this.updateOffset(serverTimeUtc, clientNow, clientNow);
  }

  now(): number {
    return Date.now() + this.serverOffset;
  }

  private updateOffset(
    serverTimeValue: string | number | Date,
    clientSend: number,
    clientReceive: number,
  ): void {
    const serverTime = this.parseServerTime(serverTimeValue);
    if (!Number.isFinite(serverTime)) {
      console.warn('[TimeSync] Ignored invalid server time', serverTimeValue);
      return;
    }

    const latency = (clientReceive - clientSend) / 2;
    this.serverOffset = serverTime + latency - clientReceive;
  }

  private parseServerTime(value: string | number | Date): number {
    if (typeof value === 'number') return value;

    if (typeof value === 'string' && /^\d+$/.test(value)) {
      return Number(value);
    }

    return new Date(value).getTime();
  }
}
