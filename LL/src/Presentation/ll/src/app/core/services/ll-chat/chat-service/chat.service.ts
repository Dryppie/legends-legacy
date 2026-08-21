import { effect, Injectable, NgZone, signal } from '@angular/core';
import { firstValueFrom, Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { HubConnection } from '@microsoft/signalr';
import { environment } from '../../../../../environments/environment';
import { ChatApiService } from '../chat-api.service';
import { HttpParams } from '@angular/common/http';
import { toObservable } from '@angular/core/rxjs-interop';
import { AuthService } from '../../api/auth/auth.service';
import { GuildStateService } from '../../api/guild/guild-state.service';
import { CharacterService } from '../../api/character/character.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { EquipmentInstance } from '../../../../shared/models/item';
import { RaidService } from '../../api/raid/raid.service';

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
  targetUrl?: string | null;
  isSystemGenerated?: boolean;
  linkedItem?: EquipmentInstance | null;
  sentAt: Date | string;
}
export enum ChatChannelType {
  General = 'General',
  Trade = 'Trade',
  Help = 'Help',
  Guild = 'Guild',
  Whisper = 'Whisper',
  System = 'System',
  Raid = 'Raid',
}

export function mergeChatMessagesChronologically(
  existing: readonly ChatMessageDto[],
  additions: readonly ChatMessageDto[],
): ChatMessageDto[] {
  const messagesById = new Map(
    existing.map((message) => [message.id, message]),
  );

  for (const message of additions) {
    if (!messagesById.has(message.id)) {
      messagesById.set(message.id, message);
    }
  }

  return [...messagesById.values()].sort((left, right) => {
    const timestampDifference =
      chatMessageTimestamp(left) - chatMessageTimestamp(right);
    return timestampDifference || left.id.localeCompare(right.id);
  });
}

function chatMessageTimestamp(message: ChatMessageDto): number {
  const timestamp =
    message.sentAt instanceof Date
      ? message.sentAt.getTime()
      : new Date(message.sentAt).getTime();
  return Number.isNaN(timestamp) ? Number.MAX_SAFE_INTEGER : timestamp;
}

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  private hub?: HubConnection;
  private readonly whisperDraftRequests = new Subject<string>();
  private activeIdentity?: string;
  private activeGuildId: string | null = null;
  private activeRaidId: string | null = null;
  private loadedRaidHistoryId: string | null = null;
  private raidHistoryRecoveryPromise?: Promise<void>;
  private raidLoadedForIdentity?: string;
  private activeAuthenticationContextVersion = -1;
  private connectAndLoadPromise?: Promise<void>;
  private unavailableUntil = 0;
  private lastConnectionWarningAt = 0;
  private readonly connectionWarningThrottleMs = 30_000;
  private readonly unavailableRetryDelayMs = 30_000;
  private lastPersistentMessageAt?: string;
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
    private raidService: RaidService,
    private gameEvents: GameRealtimeEventRegistry,
    private auth: AuthService,
  ) {
    this.gameEvents
      .eventEnvelope$('AchievementUnlocked')
      .subscribe((envelope) => {
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
      });

    this.gameEvents
      .eventEnvelope$('PlayerTransfer')
      .subscribe((envelope) => {
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
      });

    this.gameEvents
      .eventEnvelope$('GuildVaultChatMessage')
      .subscribe((envelope) => {
        const payload = envelope?.payload;
        if (!payload?.messageId || !payload.equipment) return;

        this.addMessage({
          id: payload.messageId,
          channelType: ChatChannelType.Guild,
          contextKey: payload.guildId,
          senderId: payload.actorCharacterId,
          senderName: payload.actorName,
          body: payload.action,
          linkedItem: payload.equipment,
          sentAt: new Date(
            payload.sentAt ?? envelope?.occurredAt ?? Date.now(),
          ),
        });
      });

    effect(
      () => {
        const id = this.auth.identity(); // ← depends on username + login
        const guildId = this.guildState.guild()?.id ?? null;
        const raidId = this.raidService.activeRaidChatId();
        const authenticationContextVersion =
          this.auth.authenticationContextVersion();

        if (!id) {
          this.raidService.clearActiveRaid();
          this.raidLoadedForIdentity = undefined;
          if (this.hub || this.activeIdentity) {
            void this.disconnect();
          }
          return;
        }

        if (this.raidLoadedForIdentity !== id) {
          this.raidLoadedForIdentity = id;
          this.raidService.getActiveRaid().subscribe({
            error: () => undefined,
          });
        }

        const connectionContextChanged =
          this.activeIdentity !== id ||
          this.activeGuildId !== guildId ||
          this.activeRaidId !== raidId ||
          this.activeAuthenticationContextVersion !==
            authenticationContextVersion;
        const guildMembershipChanged = this.activeGuildId !== guildId;
        const raidMembershipChanged = this.activeRaidId !== raidId;

        if (
          !connectionContextChanged &&
          (this.connectAndLoadPromise ||
            this.hub?.state === signalR.HubConnectionState.Connected ||
            this.hub?.state === signalR.HubConnectionState.Reconnecting)
        ) {
          return;
        }

        if (!connectionContextChanged && this.isTemporarilyUnavailable()) {
          return;
        }

        if (connectionContextChanged) {
          this.unavailableUntil = 0;
        }

        const hubAuthenticationContextChanged =
          this.activeIdentity !== id ||
          guildMembershipChanged ||
          this.activeAuthenticationContextVersion !==
            authenticationContextVersion;
        const replaceExistingHub =
          !!this.hub && hubAuthenticationContextChanged;
        const clearMessages =
          guildMembershipChanged ||
          (this.activeIdentity !== undefined && this.activeIdentity !== id);

        this.activeIdentity = id;
        this.activeGuildId = guildId;
        this.activeRaidId = raidId;
        this.activeAuthenticationContextVersion = authenticationContextVersion;

        const connectionAttempt = this.connectForContext(
          guildId ?? undefined,
          replaceExistingHub,
          clearMessages,
          guildMembershipChanged,
          raidMembershipChanged,
          raidId ?? undefined,
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
    const raidId = this.activeRaidId ?? undefined;
    await this.joinChannel();
    await this.loadHistory(guildId, take, undefined, raidId);
    if (raidId && this.activeRaidId === raidId) {
      this.loadedRaidHistoryId = raidId;
    }
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

  async sendRaid(raidRunId: string, body: string): Promise<void> {
    await this.ensureConnected();
    await this.hub!.invoke(
      'Send',
      raidRunId,
      body,
      ChatChannelType.Raid,
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

  async loadHistory(
    guildId?: string,
    take = 50,
    after?: string,
    raidId?: string,
  ): Promise<void> {
    let params = new HttpParams().set('Take', take.toString());

    if (guildId != null) {
      params = params.set('GuildChannel', guildId);
    }
    if (raidId != null) {
      params = params.set('RaidChannel', raidId);
    }
    if (after) {
      params = params.set('After', after);
    }

    const history = await firstValueFrom<ChatMessageDto[]>(
      this.chatApi.get('chat/GetChatHistory', params),
    );

    this.messageList.update((existing) =>
      mergeChatMessagesChronologically(existing, history),
    );
    this.recordPersistentMessages(history);
  }

  /* -------------------- private helpers -------------------- */

  private addMessage(msg: ChatMessageDto): void {
    this.messageList.update((prev) =>
      mergeChatMessagesChronologically(prev, [msg]),
    );
  }

  private async connectForContext(
    guildId: string | undefined,
    replaceExistingHub: boolean,
    clearMessages: boolean,
    guildMembershipChanged: boolean,
    raidMembershipChanged: boolean,
    raidId?: string,
  ): Promise<void> {
    if (raidMembershipChanged) {
      this.loadedRaidHistoryId = null;
      this.messageList.update((messages) =>
        messages.filter(
          (message) => message.channelType !== ChatChannelType.Raid,
        ),
      );
    }

    if (replaceExistingHub) {
      await this.stopHubConnection(clearMessages);
    } else if (clearMessages) {
      this.messageList.set([]);
    }

    // The Chat API authorizes guild groups from the access-token GuildId claim.
    // Guild state can update before its fire-and-forget token refresh completes,
    // so wait for that shared refresh before opening the replacement connection.
    if (guildMembershipChanged) {
      await firstValueFrom(this.auth.refreshSession());
    }

    this.activeRaidId = raidId ?? null;
    await this.connectAndLoad(guildId);
  }

  private async buildHubConnection(): Promise<void> {
    await firstValueFrom(this.auth.ensureValidToken());

    const hub = new signalR.HubConnectionBuilder()
      .withUrl(`${this.apiBase}/hub`, {
        accessTokenFactory: () => this.auth.getAccessToken(),
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retry) =>
          Math.min(10_000, retry.previousRetryCount * 2_000),
      })
      .configureLogging(
        environment.isLocal
          ? signalR.LogLevel.Warning
          : environment.production
            ? signalR.LogLevel.Warning
            : signalR.LogLevel.Information,
      )
      .build();
    this.hub = hub;

    // server method name is Receive(msg)
    hub.off('Receive');

    hub.on('Receive', (msg: ChatMessageDto) => {
      this.zone.run(() => {
        this.addMessage(msg);
        this.recordPersistentMessages([msg]);
        this.recoverActiveRaidHistory(msg);
      });
    });

    hub.off('OnlineCountChanged');
    hub.on('OnlineCountChanged', (count: number) => {
      this.zone.run(() => this.setOnlinePlayerCount(count));
    });

    hub.onreconnected(() => {
      void this.recoverAfterReconnect(hub);
    });

    try {
      await hub.start();
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
      const onlineCount = await hub.invoke<number>('GetOnlineCount');
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

  private async recoverAfterReconnect(hub: HubConnection): Promise<void> {
    if (this.hub !== hub) return;

    try {
      const guildId = this.activeGuildId ?? undefined;
      const raidId = this.activeRaidId ?? undefined;
      if (guildId) {
        await hub.invoke('JoinGuild', guildId);
      }

      await this.loadHistory(
        guildId,
        200,
        this.lastPersistentMessageAt,
        raidId,
      );
      if (raidId && this.activeRaidId === raidId) {
        this.loadedRaidHistoryId = raidId;
      }
      const onlineCount = await hub.invoke<number>('GetOnlineCount');
      this.zone.run(() => this.setOnlinePlayerCount(onlineCount));
    } catch (error) {
      this.handleConnectionError(error);
    }
  }

  private recoverActiveRaidHistory(message: ChatMessageDto): void {
    const raidId = this.activeRaidId;
    if (
      message.channelType !== ChatChannelType.Raid ||
      !raidId ||
      message.contextKey.toLowerCase() !== raidId.toLowerCase() ||
      this.loadedRaidHistoryId === raidId ||
      this.raidHistoryRecoveryPromise
    ) {
      return;
    }

    const guildId = this.activeGuildId ?? undefined;
    const recovery = this.loadHistory(guildId, 50, undefined, raidId)
      .then(() => {
        if (this.activeRaidId === raidId) {
          this.loadedRaidHistoryId = raidId;
        }
      })
      .catch((error) => this.handleConnectionError(error));

    this.raidHistoryRecoveryPromise = recovery;
    void recovery.finally(() => {
      if (this.raidHistoryRecoveryPromise === recovery) {
        this.raidHistoryRecoveryPromise = undefined;
      }
    });
  }

  // async reconnect(take = 50): Promise<void> {
  //   await this.disconnect();
  //   await this.connectAndLoad(take);
  // }

  async disconnect(): Promise<void> {
    await this.stopHubConnection(true);
    this.activeIdentity = undefined;
    this.activeGuildId = null;
    this.activeRaidId = null;
    this.loadedRaidHistoryId = null;
    this.raidHistoryRecoveryPromise = undefined;
    this.activeAuthenticationContextVersion = -1;
    this.connectAndLoadPromise = undefined;
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
      this.lastPersistentMessageAt = undefined;
    }

    this._onlinePlayerCount.set(null);
  }

  private handleConnectionError(error: unknown): void {
    const now = Date.now();
    if (now - this.lastConnectionWarningAt < this.connectionWarningThrottleMs) {
      return;
    }

    this.lastConnectionWarningAt = now;
    console.warn('Chat service unavailable; continuing without chat.', error);
  }

  private isTemporarilyUnavailable(): boolean {
    return Date.now() < this.unavailableUntil;
  }

  private currentSenderTitleDisplayName(): string | null {
    return (
      this.auth.currentCharacter()?.equippedTitle?.displayName?.trim() || null
    );
  }

  private recordPersistentMessages(messages: readonly ChatMessageDto[]): void {
    for (const message of messages) {
      const sentAt =
        message.sentAt instanceof Date
          ? message.sentAt
          : new Date(message.sentAt);
      if (Number.isNaN(sentAt.getTime())) continue;

      const iso = sentAt.toISOString();
      if (!this.lastPersistentMessageAt || iso > this.lastPersistentMessageAt) {
        this.lastPersistentMessageAt = iso;
      }
    }
  }

  private setOnlinePlayerCount(count: number): void {
    this._onlinePlayerCount.set(
      Number.isFinite(count) ? Math.max(0, Math.trunc(count)) : null,
    );
  }
}
