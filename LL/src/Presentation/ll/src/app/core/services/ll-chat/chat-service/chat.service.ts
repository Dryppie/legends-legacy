import { effect, Injectable, NgZone, signal } from '@angular/core';
import { firstValueFrom, ReplaySubject, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { HubConnection } from '@microsoft/signalr';
import { environment } from '../../../../../environments/environment';
import { ChatApiService } from '../chat-api.service';
import { HttpParams } from '@angular/common/http';
import { toObservable } from '@angular/core/rxjs-interop';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { AuthService } from '../../api/auth/auth.service';
import { GuildStateService } from '../../api/guild/guild-state.service';

export interface ChatMessageDto {
  id: string;
  channelType: ChatChannelType;
  contextKey: string;
  senderId: string;
  senderName: string;
  body: string;
  sentAt: Date;
}
export enum ChatChannelType {
  General = 'General',
  Trade = 'Trade',
  Help = 'Help',
  Guild = 'Guild',
  Whisper = 'Whisper',
  System = 'System',
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
    private guildState: GuildStateService,
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

      const guild = this.guildState.guild();
      if (!guild) return;

      this.connectAndLoad(guild.id); // or current channel from state

      /* make sure we disconnect when the effect re-runs or is destroyed */
      onCleanup(() => this.disconnect());
    });
  }

  /** Connect + load history in one call. */
  async connectAndLoad(guildId?: string, take = 50): Promise<void> {
    await this.joinChannel();
    await this.loadHistory(guildId, take);
    if (guildId) await this.joinGuildChannel(guildId);
  }

  /** Opens (or re-uses) a connection and joins the requested channel. */
  async joinChannel(): Promise<void> {
    if (!this.hub || this.hub.state !== signalR.HubConnectionState.Connected) {
      await this.buildHubConnection();
    } else {
      // already connected → just add to another group server-side
      // await this.hub.invoke('AddToGroup', channel);
    }
  }

  async joinGuildChannel(guildId: string): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke('JoinGuild', guildId);
  }

  /** Sends a chat message to the backend. */
  async sendPublic(
    channelType: ChatChannelType,
    contextKey: string,
    body: string,
  ): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke('Send', contextKey, body, channelType, null);
  }

  async sendGuild(guildId: string, body: string): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke('Send', guildId, body, ChatChannelType.Guild, null);
  }

  async sendWhisper(targetUserId: string, body: string): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke(
      'Send',
      '',
      body,
      ChatChannelType.Whisper,
      targetUserId,
    );
  }

  async loadHistory(guildId?: string, take = 50): Promise<void> {
    let params = new HttpParams().set('Take', take.toString());

    if (guildId != null) {
      params = params.set('GuildChannel', guildId);
    }

    const history = await firstValueFrom<ChatMessageDto[]>(
      this.chatApi.get('chat/GetChatHistory', params),
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

  private async buildHubConnection(): Promise<void> {
    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiBase}/hub?}`, {
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
    this.hub.off('Receive');

    this.hub.on('Receive', (msg: ChatMessageDto) => {
      this.zone.run(() => this.addMessage(msg));
    });

    await this.hub.start();
  }

  private async ensureConnected(): Promise<void> {
    if (!this.hub || this.hub.state !== signalR.HubConnectionState.Connected) {
      await this.buildHubConnection();
    }
  }

  // async reconnect(take = 50): Promise<void> {
  //   await this.disconnect();
  //   await this.connectAndLoad(take);
  // }

  async disconnect(): Promise<void> {
    if (
      this.hub &&
      this.hub.state !== signalR.HubConnectionState.Disconnected
    ) {
      await this.hub.stop();
    }
    this.hub = undefined;
    this.messageList.set([]);

    this.incoming$.complete(); // ends the old stream
    this.incoming$ = new ReplaySubject<ChatMessageDto>();
  }
}
