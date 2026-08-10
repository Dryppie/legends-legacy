import { effect, Injectable, NgZone, signal } from '@angular/core';
import { firstValueFrom, ReplaySubject, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { HubConnection } from '@microsoft/signalr';
import { environment } from '../../../../../environments/environment';
import { ChatApiService } from '../chat-api.service';
import { HttpParams } from '@angular/common/http';
import { toObservable } from '@angular/core/rxjs-interop';
import { AuthService } from '../../api/auth/auth.service';
import { GuildStateService } from '../../api/guild/guild-state.service';
import { CharacterService } from '../../api/character/character.service';
import { GameEventService } from '../../real-time/game-event.service';
import { EquipmentInstance } from '../../../../shared/models/item';

export interface ChatMessageDto {
  id: string;
  channelType: ChatChannelType;
  contextKey: string;
  senderId: string;
  senderName: string;
  senderTitleDisplayName?: string | null;
  targetCharacterId?: string;
  targetCharacterName?: string;
  targetCharacterTitleDisplayName?: string | null;
  body: string;
  linkedItem?: EquipmentInstance | null;
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
  private readonly whisperDraftRequests = new Subject<string>();
  private activeIdentity?: string;
  private activeGuildId: string | null = null;
  private activeAuthenticationContextVersion = -1;
  private connectAndLoadPromise?: Promise<void>;
  private unavailableUntil = 0;
  private lastConnectionWarningAt = 0;
  private readonly connectionWarningThrottleMs = 30_000;
  private readonly unavailableRetryDelayMs = 30_000;
  private readonly systemSenderId = '00000000-0000-0000-0000-000000000000';
  // expose an observable stream of all messages
  private readonly messageList = signal<ChatMessageDto[]>([]);
  private readonly _onlinePlayerCount = signal<number | null>(null);
  public messages$ = toObservable(this.messageList);
  public whisperDraftTarget$ = this.whisperDraftRequests.asObservable();
  public readonly onlinePlayerCount = this._onlinePlayerCount.asReadonly();
  private readonly apiBase = environment.chatApiRoot; // e.g. https://api.legends-legacy.com

  constructor(
    private zone: NgZone,
    private chatApi: ChatApiService,
    private characterService: CharacterService,
    private guildState: GuildStateService,
    private gameEvents: GameEventService,
    private auth: AuthService,
  ) {
    this.incoming$.subscribe((msg) => {
      this.messageList.update((prev) => [...prev, msg]);
    });

    effect(
      () => {
        const envelope = this.gameEvents.eventEnvelope.AchievementUnlockedMsg();
        const payload = envelope?.payload;
        if (!payload?.message) return;

        this.addMessage({
          id: envelope?.updateId ?? crypto.randomUUID(),
          channelType: ChatChannelType.System,
          contextKey: 'system',
          senderId: this.systemSenderId,
          senderName: payload.isGlobal ? 'World' : 'System',
          body: payload.message,
          sentAt: new Date(envelope?.occurredAt ?? Date.now()),
        });
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const envelope = this.gameEvents.eventEnvelope.PlayerTransferMsg();
        const payload = envelope?.payload;
        if (!payload?.message || !payload.messageId) return;

        this.addMessage({
          id: payload.messageId,
          channelType: ChatChannelType.System,
          contextKey: 'system',
          senderId: this.systemSenderId,
          senderName: 'System',
          body: payload.message,
          sentAt: new Date(envelope?.occurredAt ?? Date.now()),
        });
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const id = this.auth.identity(); // ← depends on username + login
        const guildId = this.guildState.guild()?.id ?? null;
        const authenticationContextVersion =
          this.auth.authenticationContextVersion();

        if (!id) {
          if (this.hub || this.activeIdentity) {
            void this.disconnect();
          }
          return;
        }

        const connectionContextChanged =
          this.activeIdentity !== id ||
          this.activeGuildId !== guildId ||
          this.activeAuthenticationContextVersion !==
            authenticationContextVersion;

        if (
          !connectionContextChanged &&
          (this.connectAndLoadPromise ||
            this.hub?.state === signalR.HubConnectionState.Connected ||
            this.hub?.state === signalR.HubConnectionState.Reconnecting)
        ) {
          return;
        }

        if (this.isTemporarilyUnavailable()) return;

        const replaceExistingHub = !!this.hub && connectionContextChanged;
        const clearMessages =
          this.activeIdentity !== undefined && this.activeIdentity !== id;

        this.activeIdentity = id;
        this.activeGuildId = guildId;
        this.activeAuthenticationContextVersion = authenticationContextVersion;

        const connectionAttempt = this.connectForContext(
          guildId ?? undefined,
          replaceExistingHub,
          clearMessages,
        ).catch((error) => {
          this.unavailableUntil = Date.now() + this.unavailableRetryDelayMs;
          this.handleConnectionError(error);
        });

        this.connectAndLoadPromise = connectionAttempt;
        void connectionAttempt.finally(() => {
          if (this.connectAndLoadPromise === connectionAttempt) {
            this.connectAndLoadPromise = undefined;
          }
        }); // or current channel from state
      },
      { allowSignalWrites: true },
    );
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
    await this.hub!.invoke(
      'Send',
      contextKey,
      body,
      channelType,
      null,
      null,
      null,
      this.currentSenderTitleDisplayName(),
    );
  }

  async sendGuild(guildId: string, body: string): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke(
      'Send',
      guildId,
      body,
      ChatChannelType.Guild,
      null,
      null,
      null,
      this.currentSenderTitleDisplayName(),
    );
  }

  async sendWhisperToName(targetName: string, body: string): Promise<void> {
    const target = await firstValueFrom(
      this.characterService.searchCharacter(targetName),
    );
    if (!target?.id) return;

    return this.sendWhisper(
      target.id,
      target.name || targetName,
      body,
      target.equippedTitle?.displayName ?? null,
    );
  }

  prepareWhisperToName(targetName: string): void {
    const trimmed = targetName.trim();
    if (!trimmed) return;

    this.whisperDraftRequests.next(trimmed);
  }

  async sendWhisper(
    targetUserId: string,
    targetName: string,
    body: string,
    targetTitleDisplayName?: string | null,
  ): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke(
      'Send',
      '',
      body,
      ChatChannelType.Whisper,
      targetUserId,
      targetName,
      targetTitleDisplayName ?? null,
      this.currentSenderTitleDisplayName(),
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
    this.messageList.update((prev) =>
      prev.some((existing) => existing.id === msg.id) ? prev : [...prev, msg],
    );
  }

  private async connectForContext(
    guildId: string | undefined,
    replaceExistingHub: boolean,
    clearMessages: boolean,
  ): Promise<void> {
    if (replaceExistingHub) {
      await this.stopHubConnection(clearMessages);
    } else if (clearMessages) {
      this.messageList.set([]);
    }

    await this.connectAndLoad(guildId);
  }

  private async buildHubConnection(): Promise<void> {
    await firstValueFrom(this.auth.ensureValidToken());

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiBase}/hub`, {
        accessTokenFactory: () => this.auth.getAccessToken(),
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retry) =>
          Math.min(10_000, retry.previousRetryCount * 2_000),
      })
      .configureLogging(
        environment.isLocal
          ? signalR.LogLevel.None
          : environment.production
            ? signalR.LogLevel.Warning
            : signalR.LogLevel.Information,
      )
      .build();

    // server method name is Receive(msg)
    this.hub.off('Receive');

    this.hub.on('Receive', (msg: ChatMessageDto) => {
      this.zone.run(() => this.addMessage(msg));
    });

    this.hub.off('OnlineCountChanged');
    this.hub.on('OnlineCountChanged', (count: number) => {
      this.zone.run(() => this.setOnlinePlayerCount(count));
    });

    try {
      await this.hub.start();
    } catch (error) {
      if (
        this.hub &&
        this.hub.state !== signalR.HubConnectionState.Disconnected
      ) {
        await this.hub.stop();
      }
      this.hub = undefined;
      this._onlinePlayerCount.set(null);
      throw error;
    }

    try {
      const onlineCount = await this.hub.invoke<number>('GetOnlineCount');
      this.zone.run(() => this.setOnlinePlayerCount(onlineCount));
    } catch {
      // Keep Chat usable during a rolling deployment with an older Chat API.
    }
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
    await this.stopHubConnection(true);
    this.activeIdentity = undefined;
    this.activeGuildId = null;
    this.activeAuthenticationContextVersion = -1;
    this.connectAndLoadPromise = undefined;

    this.incoming$.complete(); // ends the old stream
    this.incoming$ = new ReplaySubject<ChatMessageDto>();
  }

  private async stopHubConnection(clearMessages: boolean): Promise<void> {
    const hub = this.hub;

    if (hub && hub.state !== signalR.HubConnectionState.Disconnected) {
      await hub.stop();
    }

    if (this.hub === hub) {
      this.hub = undefined;
    }

    if (clearMessages) {
      this.messageList.set([]);
    }

    this._onlinePlayerCount.set(null);
  }

  private handleConnectionError(error: unknown): void {
    const now = Date.now();
    if (now - this.lastConnectionWarningAt < this.connectionWarningThrottleMs) {
      return;
    }

    this.lastConnectionWarningAt = now;
    console.warn('Chat service unavailable; continuing without chat.');
  }

  private isTemporarilyUnavailable(): boolean {
    return Date.now() < this.unavailableUntil;
  }

  private currentSenderTitleDisplayName(): string | null {
    return (
      this.auth.currentCharacter()?.equippedTitle?.displayName?.trim() || null
    );
  }

  private setOnlinePlayerCount(count: number): void {
    this._onlinePlayerCount.set(
      Number.isFinite(count) ? Math.max(0, Math.trunc(count)) : null,
    );
  }
}
