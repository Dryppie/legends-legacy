import {
  inject,
  Injectable,
  NgZone,
  Signal,
  signal,
  WritableSignal,
} from '@angular/core';
import { environment } from '../../../../environments/environment';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import {
  GameEventMap,
  GameEventName,
  GameEventEnvelopeSignalMap,
  GameEventSignalMap,
  isGameEventName,
} from './game-event/game-event.map';
import { AudienceDto } from './audience/aducienceDto';
import { GameEventEnvelope } from './game-event/game-event-envelope';
import { GameConnectionStatus } from './connection-status.model';

@Injectable({ providedIn: 'root' })
export class GameEventService {
  private readonly hubUrl = `${environment.apiBaseUrl}/hub`;
  private hub?: HubConnection;
  private connectPromise?: Promise<void>;
  private readonly guildSubscriptions = new Set<string>();
  private readonly activeGuildSubscriptions = new Set<string>();
  private readonly handledUpdateIds = new Set<string>();
  private readonly handledUpdateIdQueue: string[] = [];
  private worldSubscriptionRequested = false;
  private worldSubscriptionActive = false;
  private readonly zone = inject(NgZone);
  private readonly _connectionStatus =
    signal<GameConnectionStatus>('disconnected');
  private readonly _reconnectCount = signal(0);

  /** One *signal* per event – this is what the new code will use. */
  private readonly channelsSig = new Map<
    GameEventName,
    WritableSignal<unknown | null>
  >();
  private readonly envelopeSig = new Map<
    GameEventName,
    WritableSignal<GameEventEnvelope<GameEventName> | null>
  >();

  /* ------------  strongly-typed public signals  ------------ */
  event = new Proxy({} as GameEventSignalMap, {
    get: (_t, key: string) => this.onSig(key as GameEventName),
  }) as GameEventSignalMap;
  eventEnvelope = new Proxy({} as GameEventEnvelopeSignalMap, {
    get: (_t, key: string) => this.onEnvelopeSig(key as GameEventName),
  }) as GameEventEnvelopeSignalMap;
  // add one line per new event, or code-gen them
  readonly connectionStatus = this._connectionStatus.asReadonly();
  readonly reconnectCount = this._reconnectCount.asReadonly();

  /* -------------  connection boilerplate (unchanged)  ------------- */

  async connect(audience?: AudienceDto): Promise<void> {
    if (this.hub?.state === HubConnectionState.Connected) {
      if (audience) {
        await this.subscribeToAudience(audience);
      }
      return;
    }

    if (this.connectPromise) {
      await this.connectPromise;
      if (audience) {
        await this.subscribeToAudience(audience);
      }
      return;
    }

    this.hub = new HubConnectionBuilder()
      .withUrl(this.hubUrl, { withCredentials: true })
      // .withHubProtocol(new MessagePackHubProtocol())
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (r) =>
          Math.min(10_000, r.previousRetryCount * 2_000),
      })
      .configureLogging(LogLevel.Warning)
      .build();

    this.hub.on('Publish', (env: GameEventEnvelope<string>) =>
      this.dispatch(env),
    );

    this.hub.onreconnecting((error) => {
      this.activeGuildSubscriptions.clear();
      this.worldSubscriptionActive = false;
      this.zone.run(() => this._connectionStatus.set('reconnecting'));
      if (error) console.warn('Game realtime reconnecting', error);
    });

    this.hub.onreconnected(() => {
      this.activeGuildSubscriptions.clear();
      this.worldSubscriptionActive = false;
      this.zone.run(() => {
        this._connectionStatus.set('connected');
        this._reconnectCount.update((count) => count + 1);
      });
      void this.resubscribeAudiences();
    });

    this.hub.onclose((error) => {
      this.activeGuildSubscriptions.clear();
      this.worldSubscriptionActive = false;
      this.zone.run(() => this._connectionStatus.set('disconnected'));
      if (error) console.warn('Game realtime disconnected', error);
    });

    this._connectionStatus.set('connecting');
    this.connectPromise = this.hub
      .start()
      .then(() => {
        this.zone.run(() => this._connectionStatus.set('connected'));
      })
      .catch((error) => {
        this.zone.run(() => this._connectionStatus.set('disconnected'));
        throw error;
      })
      .finally(() => {
        this.connectPromise = undefined;
      });

    await this.connectPromise;

    // Character stream is automatic; subscribe to extra audiences if requested.
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
    this.guildSubscriptions.add(guildId);
    await this.ensureConnected();
    if (this.activeGuildSubscriptions.has(guildId)) return;

    await this.hub?.invoke('SubscribeToGuild', guildId);
    this.activeGuildSubscriptions.add(guildId);
  }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.hub = undefined;
    this.guildSubscriptions.clear();
    this.activeGuildSubscriptions.clear();
    this.worldSubscriptionRequested = false;
    this.worldSubscriptionActive = false;
    this._connectionStatus.set('disconnected');

    /* reset signals to null so new components can distinguish old vs. new data */
    this.channelsSig.forEach((sig) => sig.set(null));
    this.envelopeSig.forEach((sig) => sig.set(null));
    this.handledUpdateIds.clear();
    this.handledUpdateIdQueue.length = 0;
  }

  /* --------------------  PUBLIC API  -------------------- */

  /** Generic accessor for any event as a signal (typed). */
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

  /* -----------------  internal fan-out  ----------------- */

  private dispatch(env: GameEventEnvelope<string>): void {
    if (env.updateId && this.hasHandledUpdate(env.updateId)) {
      return;
    }

    if (!isGameEventName(env.event)) {
      console.warn(`Unknown game event ignored: ${env.event}`);
      return;
    }

    /* Update the signal (in the Angular zone so change detection runs). */
    let sig = this.channelsSig.get(env.event);
    if (!sig) {
      sig = signal<unknown | null>(null);
      this.channelsSig.set(env.event, sig);
    }

    let envelopeSignal = this.envelopeSig.get(env.event);
    if (!envelopeSignal) {
      envelopeSignal = signal<GameEventEnvelope<GameEventName> | null>(null);
      this.envelopeSig.set(env.event, envelopeSignal);
    }

    this.zone.run(() => {
      (sig as WritableSignal<unknown>).set(env.payload);
      (
        envelopeSignal as WritableSignal<GameEventEnvelope<GameEventName>>
      ).set(env as GameEventEnvelope<GameEventName>);
    });
  }

  private async ensureConnected(): Promise<void> {
    if (this.hub?.state === HubConnectionState.Connected) return;
    await this.connect();
  }

  private async subscribeToWorld(): Promise<void> {
    this.worldSubscriptionRequested = true;
    await this.ensureConnected();
    if (this.worldSubscriptionActive) return;

    await this.hub?.invoke('SubscribeToWorld');
    this.worldSubscriptionActive = true;
  }

  private async resubscribeAudiences(): Promise<void> {
    if (this.worldSubscriptionRequested) {
      try {
        await this.subscribeToWorld();
      } catch (error) {
        console.warn('Failed to resubscribe to world realtime', error);
      }
    }

    for (const guildId of this.guildSubscriptions) {
      try {
        await this.subscribeToGuild(guildId);
      } catch (error) {
        console.warn('Failed to resubscribe to guild realtime', error);
      }
    }
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
