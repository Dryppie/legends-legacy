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
  private readonly zone = inject(NgZone);
  private readonly _connectionStatus =
    signal<GameConnectionStatus>('disconnected');
  private readonly _reconnectCount = signal(0);

  /** One *signal* per event – this is what the new code will use. */
  private readonly channelsSig = new Map<
    GameEventName,
    WritableSignal<unknown | null>
  >();

  /* ------------  strongly-typed public signals  ------------ */
  event = new Proxy({} as GameEventSignalMap, {
    get: (_t, key: string) => this.onSig(key as GameEventName),
  }) as GameEventSignalMap;
  // add one line per new event, or code-gen them
  readonly connectionStatus = this._connectionStatus.asReadonly();
  readonly reconnectCount = this._reconnectCount.asReadonly();

  /* -------------  connection boilerplate (unchanged)  ------------- */

  async connect(audience?: AudienceDto): Promise<void> {
    if (this.hub?.state === HubConnectionState.Connected) return;
    if (this.connectPromise) return this.connectPromise;

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
      this.zone.run(() => this._connectionStatus.set('reconnecting'));
      if (error) console.warn('Game realtime reconnecting', error);
    });

    this.hub.onreconnected(() => {
      this.zone.run(() => {
        this._connectionStatus.set('connected');
        this._reconnectCount.update((count) => count + 1);
      });
    });

    this.hub.onclose((error) => {
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
      // await this.hub.invoke('SubscribeToAudience', audience);
    }
  }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.hub = undefined;
    this._connectionStatus.set('disconnected');

    /* reset signals to null so new components can distinguish old vs. new data */
    this.channelsSig.forEach((sig) => sig.set(null));
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

  /* -----------------  internal fan-out  ----------------- */

  private dispatch(env: GameEventEnvelope<string>): void {
    if (!isGameEventName(env.event)) {
      console.warn(`Unknown game event ignored: ${env.event}`);
      return;
    }

    /* Update the signal (in the Angular zone so change detection runs). */
    const sig = this.channelsSig.get(env.event);
    if (sig) {
      this.zone.run(() => (sig as WritableSignal<unknown>).set(env.payload));
    }
  }
}
