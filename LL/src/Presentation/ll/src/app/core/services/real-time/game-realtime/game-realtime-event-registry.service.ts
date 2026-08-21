import {
  Injectable,
  Injector,
  Signal,
  WritableSignal,
  inject,
  signal,
} from '@angular/core';
import { Observable, ReplaySubject, Subscription } from 'rxjs';
import { GameRealtimeDiagnostics } from './game-realtime-diagnostics.service';
import {
  GameRealtimeEnvelope,
  GameRealtimeSignalEventMap,
  LootReceived,
  StateInvalidated,
  StateInvalidations,
  gameRealtimeEventNames,
  isGameRealtimeSignalEventName,
} from './game-realtime-contracts';
import { GameRealtimeConnection } from './game-realtime-connection.service';
import { isGameRealtimeEnabled } from './game-realtime-feature';
import { GameRealtimeStore } from './game-realtime-store.service';
import { StateSyncCoordinator } from './state-sync-coordinator.service';
import { RealtimeUpdateDeduper } from './realtime-deduplication';

type Handler = (envelope: GameRealtimeEnvelope) => void;
type RegistryEventMap = GameRealtimeSignalEventMap;
type RegistryEventName = keyof RegistryEventMap & string;
type RegistryEventSignalMap = {
  [K in RegistryEventName]: Signal<RegistryEventMap[K] | null>;
};
type RegistryEnvelopeSignalMap = {
  [K in RegistryEventName]: Signal<GameRealtimeEnvelope<
    RegistryEventMap[K]
  > | null>;
};

@Injectable({ providedIn: 'root' })
export class GameRealtimeEventRegistry {
  private readonly connection = inject(GameRealtimeConnection);
  private readonly diagnostics = inject(GameRealtimeDiagnostics);
  private readonly injector = inject(Injector);
  private readonly handlers = new Map<string, Handler>();
  private readonly updateDeduper = new RealtimeUpdateDeduper();
  private readonly channels = new Map<
    RegistryEventName,
    WritableSignal<unknown | null>
  >();
  private readonly envelopes = new Map<
    RegistryEventName,
    WritableSignal<GameRealtimeEnvelope | null>
  >();
  private readonly envelopeStreams = new Map<
    RegistryEventName,
    ReplaySubject<GameRealtimeEnvelope>
  >();
  private registered = false;
  private subscription?: Subscription;

  readonly event = new Proxy({} as RegistryEventSignalMap, {
    get: (_target, key: string) => this.eventSignal(key as RegistryEventName),
  }) as RegistryEventSignalMap;

  readonly eventEnvelope = new Proxy({} as RegistryEnvelopeSignalMap, {
    get: (_target, key: string) =>
      this.eventEnvelopeSignal(key as RegistryEventName),
  }) as RegistryEnvelopeSignalMap;

  initialize(): void {
    if (!isGameRealtimeEnabled() || this.registered) return;

    this.handlers.clear();
    this.registerHandlers();
    this.registered = true;
    this.subscription = this.connection.events$.subscribe((envelope) =>
      this.dispatch(envelope),
    );
  }

  dispose(): void {
    this.subscription?.unsubscribe();
    this.subscription = undefined;
    this.handlers.clear();
    this.channels.forEach((channel) => channel.set(null));
    this.envelopes.forEach((envelope) => envelope.set(null));
    this.updateDeduper.clear();
    this.registered = false;
  }

  eventSignal<K extends RegistryEventName>(
    name: K,
  ): Signal<RegistryEventMap[K] | null> {
    let channel = this.channels.get(name) as
      | WritableSignal<RegistryEventMap[K] | null>
      | undefined;
    if (!channel) {
      channel = signal<RegistryEventMap[K] | null>(null);
      this.channels.set(name, channel);
    }
    return channel.asReadonly();
  }

  eventEnvelopeSignal<K extends RegistryEventName>(
    name: K,
  ): Signal<GameRealtimeEnvelope<RegistryEventMap[K]> | null> {
    let envelope = this.envelopes.get(name) as
      | WritableSignal<GameRealtimeEnvelope<RegistryEventMap[K]> | null>
      | undefined;
    if (!envelope) {
      envelope = signal<GameRealtimeEnvelope<RegistryEventMap[K]> | null>(null);
      this.envelopes.set(
        name,
        envelope as WritableSignal<GameRealtimeEnvelope | null>,
      );
    }
    return envelope.asReadonly();
  }

  eventEnvelope$<K extends RegistryEventName>(
    name: K,
  ): Observable<GameRealtimeEnvelope<RegistryEventMap[K]>> {
    let stream = this.envelopeStreams.get(name);
    if (!stream) {
      stream = new ReplaySubject<GameRealtimeEnvelope>(100);
      this.envelopeStreams.set(name, stream);
    }
    return stream.asObservable() as Observable<
      GameRealtimeEnvelope<RegistryEventMap[K]>
    >;
  }

  private registerHandlers(): void {
    this.addHandler(gameRealtimeEventNames.lootReceived, (envelope) => {
      const payload = envelope.payload as LootReceived;
      this.injector
        .get(GameRealtimeStore)
        .addLoot(
          payload.items ?? [],
          envelope.occurredAt,
          payload.source,
          payload.location,
          payload.grantId,
        );

      // Inventory is refreshed from the matching StateInvalidated revision.
      // Applying this delta as well races the authoritative snapshot: when the
      // snapshot already includes the grant, adding the delta doubles the local
      // quantity even though the database and loot history are correct.
    });

    this.addHandler(gameRealtimeEventNames.stateInvalidated, (envelope) => {
      this.injector
        .get(StateSyncCoordinator)
        .acceptInvalidation(
          envelope.payload as StateInvalidated,
          envelope.updateId,
        );
    });

    this.addHandler(gameRealtimeEventNames.stateInvalidations, (envelope) => {
      const payload = envelope.payload as StateInvalidations;
      this.injector
        .get(StateSyncCoordinator)
        .acceptInvalidations(payload.revisions, envelope.updateId);
    });
  }

  private addHandler(eventName: string, handler: Handler): void {
    if (this.handlers.has(eventName)) {
      throw new Error(
        `Duplicate GameRealtime handler registered for ${eventName}`,
      );
    }

    this.handlers.set(eventName, handler);
  }

  private dispatch(envelope: GameRealtimeEnvelope): void {
    if (!this.updateDeduper.shouldProcess(envelope.updateId)) {
      this.diagnostics.recordDuplicate(envelope);
      return;
    }

    const handler = this.handlers.get(envelope.event);
    if (handler) {
      this.diagnostics.runHandler(envelope, () => handler(envelope), true);
      return;
    }

    if (!isGameRealtimeSignalEventName(envelope.event)) {
      this.diagnostics.recordUnknown(envelope);
      return;
    }

    const eventName = envelope.event as RegistryEventName;
    let channel = this.channels.get(eventName);
    if (!channel) {
      channel = signal<unknown | null>(null);
      this.channels.set(eventName, channel);
    }

    let envelopeChannel = this.envelopes.get(eventName);
    if (!envelopeChannel) {
      envelopeChannel = signal<GameRealtimeEnvelope | null>(null);
      this.envelopes.set(eventName, envelopeChannel);
    }

    this.diagnostics.runHandler(
      envelope,
      () => {
        channel.set(envelope.payload);
        envelopeChannel.set(envelope);
        this.envelopeStreams.get(eventName)?.next(envelope);
      },
      true,
    );
  }
}
