import {
  Injectable,
  DestroyRef,
  computed,
  signal,
  WritableSignal,
} from '@angular/core';
import {
  Subject,
  takeUntil,
  timer,
  retry,
  OperatorFunction,
  Subscription,
} from 'rxjs';
import { webSocket, WebSocketSubject } from 'rxjs/webSocket';
import { Incoming } from './contracts';
import { environment } from '../../../../environments/environment';

function backoffRetry<T>(
  maxRetries: number,
  baseDelay: number,
): OperatorFunction<T, T> {
  return retry({
    count: maxRetries,
    delay: (_error, retryCount) => timer(retryCount * baseDelay),
    resetOnSuccess: true,
  });
}

function buildGameWsUrl(apiUrl: string): string {
  const wsProtocol = apiUrl.startsWith('http') ? 'ws' : 'wss';
  const withoutProtocol = apiUrl
    .replace(/^https?:\/\//, '')
    .replace(/\/+$/, '');
  return `${wsProtocol}://${withoutProtocol}/api/v1/GameHub/game`;
}
type IncomingByType = {
  [I in Incoming as I['type']]: I['payload'];
};
@Injectable({ providedIn: 'root' })
export class GameSocketService {
  /* ────────── outward API ────────── */

  /** true while the socket is open */
  readonly isConnected = signal(false);

  /** latest envelope the server pushed (null before first) */
  readonly lastMsg: WritableSignal<Incoming | null> = signal(null);

  /**
   * Helper: get a derived signal that yields the payload for a given type
   * or `null` when the latest envelope is something else.
   */
  ofType<T extends keyof IncomingByType>(type: T) {
    return computed<IncomingByType[T] | null>(() => {
      const m = this.lastMsg();
      return m?.type === type ? (m.payload as IncomingByType[T]) : null;
    });
  }

  /* ────────── internals ────────── */

  private socket$?: WebSocketSubject<Incoming>;
  private inboundSub?: Subscription;
  private outbound$ = new Subject<unknown>();
  private close$ = new Subject<void>();

  constructor(destroy: DestroyRef) {
    /* When the DI scope is destroyed (tests / HMR) ensure clean shutdown */
    destroy.onDestroy(() => this.disconnect());
  }

  /* ------------ public lifecycle ------------ */

  /** call after you have a JWT or session token */
  connect(): void {
    if (this.socket$) return; // already live
    /* create the WS subject */
    this.socket$ = webSocket<Incoming>({
      url: buildGameWsUrl('https://dev.legends-legacy.com'),
      deserializer: ({ data }) => {
        return JSON.parse(data);
      },
      serializer: (x) => JSON.stringify(x),
      openObserver: { next: () => this.isConnected.set(true) },
      closeObserver: { next: () => this.isConnected.set(false) },
    });

    /* outbound queue */
    this.outbound$
      .pipe(takeUntil(this.close$))
      .subscribe((msg) => this.socket$!.next(msg as any));

    /* inbound → writable signal */
    const inbound$ = this.socket$.pipe(
      backoffRetry(3, 1_500), // your helper
      takeUntil(this.close$),
    );
    this.inboundSub = inbound$.subscribe((m) => this.lastMsg.set(m));
  }

  /** close the connection and reset state */
  disconnect(): void {
    this.inboundSub?.unsubscribe();
    this.inboundSub = undefined;

    this.socket$?.complete();
    this.socket$ = undefined;

    this.close$.next(); // stop back-off
    this.close$ = new Subject(); // fresh notifier for next connect

    this.isConnected.set(false);
    this.lastMsg.set(null);
  }

  /** push a command to the server */
  send(cmd: unknown): void {
    this.outbound$.next(cmd);
  }
}
