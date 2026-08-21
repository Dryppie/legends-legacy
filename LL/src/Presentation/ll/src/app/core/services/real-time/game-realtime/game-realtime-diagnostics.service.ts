import { Injectable, NgZone, inject } from '@angular/core';
import { Router } from '@angular/router';
import { GameRealtimeEnvelope } from './game-realtime-contracts';
import { isProductionRuntime } from './game-realtime-feature';
import { StateSyncDiagnostics } from './state-sync-diagnostics.service';

export interface GameRealtimeDiagnosticEvent {
  event: string;
  updateId?: string;
  timestamp: number;
  payloadBytes: number;
  handlerStart?: number;
  handlerDurationMs?: number;
  causedHttpRequest?: boolean;
  updatedState?: boolean;
  route?: string;
  disposition: 'received' | 'handled' | 'duplicate' | 'unknown' | 'failed';
  handlerError?: string;
}

@Injectable({ providedIn: 'root' })
export class GameRealtimeDiagnostics {
  private readonly maxEvents = 100;
  private readonly slowHandlerMs = 50;
  private readonly freezeThresholdMs = 250;
  private readonly events: GameRealtimeDiagnosticEvent[] = [];
  private readonly router = inject(Router);
  private readonly zone = inject(NgZone);
  private readonly stateSyncDiagnostics = inject(StateSyncDiagnostics);
  private heartbeatStarted = false;

  start(): void {
    if (
      isProductionRuntime() ||
      this.heartbeatStarted ||
      typeof window === 'undefined'
    )
      return;
    this.heartbeatStarted = true;
    this.installDebugApi();
    this.startFreezeHeartbeat();
  }

  recordReceive(envelope: GameRealtimeEnvelope): void {
    if (isProductionRuntime()) return;
    this.push({
      event: envelope.event,
      updateId: envelope.updateId,
      timestamp: Date.now(),
      payloadBytes: this.estimatePayloadBytes(envelope.payload),
      route: this.router.url,
      disposition: 'received',
    });
  }

  recordDuplicate(envelope: GameRealtimeEnvelope): void {
    if (isProductionRuntime()) return;
    const entry = this.findEntry(envelope.updateId);
    if (entry) entry.disposition = 'duplicate';
  }

  recordUnknown(envelope: GameRealtimeEnvelope): void {
    if (isProductionRuntime()) return;
    const entry = this.findEntry(envelope.updateId);
    if (entry) entry.disposition = 'unknown';
  }

  runHandler(
    envelope: GameRealtimeEnvelope,
    handler: () => void,
    updatedState: boolean,
    causedHttpRequest = false,
  ): void {
    if (isProductionRuntime()) {
      handler();
      return;
    }

    const start = performance.now();
    const entry = this.findEntry(envelope.updateId);
    if (entry) {
      entry.handlerStart = Date.now();
      entry.causedHttpRequest = causedHttpRequest;
      entry.updatedState = updatedState;
      entry.route = this.router.url;
    }

    try {
      handler();
      if (entry) entry.disposition = 'handled';
    } catch (error) {
      if (entry) {
        entry.disposition = 'failed';
        entry.handlerError =
          error instanceof Error ? error.message : String(error);
      }
      throw error;
    } finally {
      const duration = performance.now() - start;
      if (entry) {
        entry.handlerDurationMs = Math.round(duration * 10) / 10;
      }

      if (duration > this.slowHandlerMs) {
        console.warn(
          `[GameRealtime] Slow handler: ${envelope.event} ${duration.toFixed(1)}ms`,
          envelope.payload,
        );
      }
    }
  }

  recentEvents(): GameRealtimeDiagnosticEvent[] {
    return [...this.events];
  }

  printRecentEvents(): void {
    console.table(this.recentEvents());
  }

  clear(): void {
    this.events.length = 0;
  }

  private push(entry: GameRealtimeDiagnosticEvent): void {
    this.events.push(entry);
    while (this.events.length > this.maxEvents) {
      this.events.shift();
    }
  }

  private findEntry(
    updateId?: string,
  ): GameRealtimeDiagnosticEvent | undefined {
    if (!updateId) return this.events[this.events.length - 1];
    for (let i = this.events.length - 1; i >= 0; i -= 1) {
      if (this.events[i].updateId === updateId) return this.events[i];
    }
    return undefined;
  }

  private estimatePayloadBytes(payload: unknown): number {
    try {
      return new Blob([JSON.stringify(payload)]).size;
    } catch {
      return -1;
    }
  }

  private startFreezeHeartbeat(): void {
    this.zone.runOutsideAngular(() => {
      let last = performance.now();
      window.setInterval(() => {
        const now = performance.now();
        const blockedFor = now - last - 1000;
        last = now;

        if (blockedFor > this.freezeThresholdMs) {
          console.warn(
            `[GameRealtime] Main thread blocked for ${blockedFor.toFixed(0)}ms. Recent game realtime events:`,
          );
          this.printRecentEvents();
        }
      }, 1000);
    });
  }

  private installDebugApi(): void {
    if (isProductionRuntime() || typeof window === 'undefined') return;

    (window as any).__gameSignalRDebug = {
      printRecentEvents: () => this.printRecentEvents(),
      clear: () => this.clear(),
      recentEvents: () => this.recentEvents(),
      stateSync: {
        snapshot: () => this.stateSyncDiagnostics.snapshot(),
        print: () => this.stateSyncDiagnostics.print(),
        clear: () => this.stateSyncDiagnostics.clear(),
      },
    };
  }
}
