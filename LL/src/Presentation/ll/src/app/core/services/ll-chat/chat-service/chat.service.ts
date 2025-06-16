import { effect, Injectable, NgZone, signal } from '@angular/core';
import {
  combineLatest,
  distinctUntilChanged,
  firstValueFrom,
  ReplaySubject,
  Subject,
  switchMap,
} from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { HubConnection } from '@microsoft/signalr';
import { environment } from '../../../../../environments/environment';
import { ChatApiService } from '../chat-api.service';
import { HttpParams } from '@angular/common/http';
import { toObservable } from '@angular/core/rxjs-interop';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { AuthService } from '../../api/auth/auth.service';

export interface ChatMessageDto {
  id: string;
  channel: string;
  senderId: string;
  senderName: string;
  body: string;
  sentAt: Date;
}

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  private hub?: HubConnection;
  private incoming$ = new Subject<ChatMessageDto>();
  // expose an observable stream of all messages
  private readonly messageList = signal<ChatMessageDto[]>([]);
  public messages$ = toObservable(this.messageList);
  private readonly apiBase = environment.chatApiRoot; // e.g. https://api.legends-legacy.com

  constructor(
    private zone: NgZone,
    private chatApi: ChatApiService,
    private eventBus: EventBusService,
    private auth: AuthService,
  ) {
    this.incoming$.subscribe((msg) => {
      this.messageList.update((prev) => [...prev, msg]);
    });
    effect((onCleanup) => {
      const id = this.auth.identity(); // ← depends on username + login
      if (!id) {
        this.disconnect();
        return;
      }

      this.connectAndLoad('global'); // or current channel from state

      /* make sure we disconnect when the effect re-runs or is destroyed */
      onCleanup(() => this.disconnect());
    });
  }

  /** Connect + load history in one call. */
  async connectAndLoad(channel: string, take = 50): Promise<void> {
    await this.joinChannel(channel);
    await this.loadHistory(channel, take);
  }

  /** Opens (or re-uses) a connection and joins the requested channel. */
  async joinChannel(channel: string): Promise<void> {
    if (
      !this.hub ||
      this.hub.state === signalR.HubConnectionState.Disconnected
    ) {
      await this.buildHubConnection(channel);
    } else {
      // already connected → just add to another group server-side
      // await this.hub.invoke('AddToGroup', channel);
    }
  }

  /** Sends a chat message to the backend. */
  async send(channel: string, body: string): Promise<void> {
    await this.ensureConnected(channel);
    return this.hub!.invoke('Send', channel, body);
  }

  async loadHistory(channel: string = 'global', take = 50): Promise<void> {
    const history = await firstValueFrom<ChatMessageDto[]>(
      this.chatApi.get(
        'chat/GetChatHistory',
        new HttpParams().set('Channel', channel).set('Take', take.toString()),
      ),
    );

    history
      .sort(
        (a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime(),
      )
      .forEach((m) => this.addMessage(m));
  }

  /* -------------------- private helpers -------------------- */

  private addMessage(msg: ChatMessageDto): void {
    this.messageList.update((prev) => [...prev, msg]);
  }

  private async buildHubConnection(channel: string): Promise<void> {
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiBase}/hub?channel=${encodeURIComponent(channel)}`, {
        withCredentials: true, // send AccessToken cookie
        // DEV ONLY – include bearer if you keep tokens outside cookies
        accessTokenFactory: () => localStorage.getItem('DevAuth') ?? '',
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retry) =>
          Math.min(10_000, retry.previousRetryCount * 2_000),
      })
      .configureLogging(
        environment.production
          ? signalR.LogLevel.Warning
          : signalR.LogLevel.Information,
      )
      .build();

    // server method name is Receive(msg)
    this.hub.on('Receive', (msg: ChatMessageDto) => {
      this.zone.run(() => this.addMessage(msg));
    });

    await this.hub.start();
  }

  private async ensureConnected(channel: string): Promise<void> {
    if (!this.hub || this.hub.state !== signalR.HubConnectionState.Connected) {
      await this.buildHubConnection(channel);
    }
  }

  async reconnect(channel: string = 'global', take = 50): Promise<void> {
    await this.disconnect();
    await this.connectAndLoad(channel, take);
  }

  async disconnect(): Promise<void> {
    if (
      this.hub &&
      this.hub.state !== signalR.HubConnectionState.Disconnected
    ) {
      await this.hub.stop();
    }
    this.hub = undefined;
    this.messageList.set([]);

    // 🧼 Reset incoming$ so loadHistory won't push duplicates
    this.incoming$.complete(); // ends the old stream
    this.incoming$ = new ReplaySubject<ChatMessageDto>();
  }
}
