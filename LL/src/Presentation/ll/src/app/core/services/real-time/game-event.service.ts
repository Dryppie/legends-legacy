import { Injectable, Signal, signal, WritableSignal } from '@angular/core';
import {
  GameEventMap,
  GameEventName,
  GameEventEnvelopeSignalMap,
  GameEventSignalMap,
  isGameEventName,
} from './game-event/game-event.map';
import { AudienceDto } from './audience/aducienceDto';
import { GameEventEnvelope } from './game-event/game-event-envelope';
import { GameRealtimeDiagnostics } from './game-realtime/game-realtime-diagnostics.service';
import { GameRealtimeConnection } from './game-realtime/game-realtime-connection.service';
import { GameRealtimeEnvelope } from './game-realtime/game-realtime-contracts';
import { isGameRealtimeEnabled } from './game-realtime/game-realtime-feature';

@Injectable({ providedIn: 'root' })
export class GameEventService {
  private readonly handledUpdateIds = new Set<string>();
  private readonly handledUpdateIdQueue: string[] = [];
  private initialized = false;

  /** One *signal* per event - this is what consumers use. */
  private readonly channelsSig = new Map<
    GameEventName,
    WritableSignal<unknown | null>
  >();
  private readonly envelopeSig = new Map<
    GameEventName,
    WritableSignal<GameEventEnvelope<GameEventName> | null>
  >();

  event = new Proxy({} as GameEventSignalMap, {
    get: (_target, key: string) => this.onSig(key as GameEventName),
  }) as GameEventSignalMap;

  eventEnvelope = new Proxy({} as GameEventEnvelopeSignalMap, {
    get: (_target, key: string) => this.onEnvelopeSig(key as GameEventName),
  }) as GameEventEnvelopeSignalMap;

  readonly connectionStatus;
  readonly reconnectCount;

  constructor(
    private readonly connection: GameRealtimeConnection,
    private readonly diagnostics: GameRealtimeDiagnostics,
  ) {
    this.connectionStatus = this.connection.connectionStatus;
    this.reconnectCount = this.connection.reconnectCount;
  }

  async connect(audience?: AudienceDto): Promise<void> {
    if (!isGameRealtimeEnabled()) return;

    this.initialize();
    await this.connection.connect();

    if (audience) {
      await this.subscribeToAudience(audience);
    }
  }

  async subscribeToAudience(audience: AudienceDto): Promise<void> {
    switch (audience.kind) {
      case 'Guild':
        await this.subscribeToGuild(audience.guildId);
        break;
      case 'World':
        await this.subscribeToWorld();
        break;
    }
  }

  async subscribeToGuild(guildId: string): Promise<void> {
    await this.connection.subscribeToGuild(guildId);
  }

  async setGuildSubscription(guildId: string | null): Promise<void> {
    await this.connection.setGuildSubscription(guildId);
  }

  async subscribeToWorld(): Promise<void> {
    await this.connection.subscribeToWorld();
  }

  async disconnect(): Promise<void> {
    this.resetSignals();
  }

  onSig<K extends GameEventName>(name: K): Signal<GameEventMap[K] | null> {
    let sig = this.channelsSig.get(name) as
      | WritableSignal<GameEventMap[K] | null>
      | undefined;
    if (!sig) {
      sig = signal<GameEventMap[K] | null>(null);
      this.channelsSig.set(name, sig);
    }
    return sig.asReadonly();
  }

  onEnvelopeSig<K extends GameEventName>(
    name: K,
  ): Signal<GameEventEnvelope<K> | null> {
    let sig = this.envelopeSig.get(name) as
      | WritableSignal<GameEventEnvelope<K> | null>
      | undefined;
    if (!sig) {
      sig = signal<GameEventEnvelope<K> | null>(null);
      this.envelopeSig.set(
        name,
        sig as WritableSignal<GameEventEnvelope<GameEventName> | null>,
      );
    }
    return sig.asReadonly();
  }

  private initialize(): void {
    if (this.initialized) return;
    this.initialized = true;
    this.diagnostics.start();
    this.connection.events$.subscribe((envelope) => this.dispatch(envelope));
  }

  private dispatch(envelope: GameRealtimeEnvelope): void {
    if (envelope.updateId && this.hasHandledUpdate(envelope.updateId)) {
      return;
    }

    if (!isGameEventName(envelope.event)) {
      return;
    }

    const legacyEnvelope = envelope as GameEventEnvelope<GameEventName>;

    let sig = this.channelsSig.get(legacyEnvelope.event);
    if (!sig) {
      sig = signal<unknown | null>(null);
      this.channelsSig.set(legacyEnvelope.event, sig);
    }

    let envelopeSignal = this.envelopeSig.get(legacyEnvelope.event);
    if (!envelopeSignal) {
      envelopeSignal = signal<GameEventEnvelope<GameEventName> | null>(null);
      this.envelopeSig.set(legacyEnvelope.event, envelopeSignal);
    }

    this.diagnostics.runHandler(
      {
        updateId: envelope.updateId,
        occurredAt: envelope.occurredAt,
        event: `compat:${legacyEnvelope.event}`,
        payload: envelope.payload,
      },
      () => {
        (sig as WritableSignal<unknown>).set(legacyEnvelope.payload);
        (
          envelopeSignal as WritableSignal<GameEventEnvelope<GameEventName>>
        ).set(legacyEnvelope);
      },
      true,
    );
  }

  private resetSignals(): void {
    this.channelsSig.forEach((sig) => sig.set(null));
    this.envelopeSig.forEach((sig) => sig.set(null));
    this.handledUpdateIds.clear();
    this.handledUpdateIdQueue.length = 0;
  }

  private hasHandledUpdate(updateId: string): boolean {
    if (this.handledUpdateIds.has(updateId)) return true;

    this.handledUpdateIds.add(updateId);
    this.handledUpdateIdQueue.push(updateId);

    const maxTrackedUpdates = 500;
    while (this.handledUpdateIdQueue.length > maxTrackedUpdates) {
      const oldestUpdateId = this.handledUpdateIdQueue.shift();
      if (oldestUpdateId) {
        this.handledUpdateIds.delete(oldestUpdateId);
      }
    }

    return false;
  }
}
