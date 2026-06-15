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

@Injectable({ providedIn: 'root' })
export class GameEventService {
  private readonly hubUrl = `${environment.apiBaseUrl}/hub`;
  private hub?: HubConnection;
  private readonly zone = inject(NgZone);

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

  /* -------------  connection boilerplate (unchanged)  ------------- */

  async connect(audience?: AudienceDto): Promise<void> {
    if (this.hub?.state === HubConnectionState.Connected) return;

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

    await this.hub.start();

    // Character stream is automatic; subscribe to extra audiences if requested.
    if (audience) {
      // await this.hub.invoke('SubscribeToAudience', audience);
    }
  }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.hub = undefined;

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
