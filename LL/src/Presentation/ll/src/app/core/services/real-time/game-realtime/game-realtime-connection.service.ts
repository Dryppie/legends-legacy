import { Injectable, NgZone, computed, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../api/auth/auth.service';
import { GameConnectionStatus } from '../connection-status.model';
import { GameRealtimeDiagnostics } from './game-realtime-diagnostics.service';
import { GameRealtimeEnvelope } from './game-realtime-contracts';
import { isGameRealtimeEnabled } from './game-realtime-feature';

@Injectable({ providedIn: 'root' })
export class GameRealtimeConnection {
  private readonly hubUrl = `${environment.apiBaseUrl}/hub/game`;
  private readonly zone = inject(NgZone);
  private readonly auth = inject(AuthService);
  private readonly diagnostics = inject(GameRealtimeDiagnostics);
  private readonly eventsSubject = new Subject<GameRealtimeEnvelope>();
  private readonly _connectionStatus =
    signal<GameConnectionStatus>('disconnected');
  private readonly _reconnectCount = signal(0);
  private hub?: HubConnection;
  private connectPromise?: Promise<void>;
  private handlersRegistered = false;
  private readonly guildSubscriptions = new Set<string>();
  private readonly activeGuildSubscriptions = new Set<string>();
  private readonly raidSubscriptions = new Set<string>();
  private readonly activeRaidSubscriptions = new Set<string>();
  private worldSubscriptionRequested = false;
  private worldSubscriptionActive = false;
  private readonly tournamentGroundsSubscriptionOwners = new Set<string>();
  private tournamentGroundsSubscriptionActive = false;

  readonly events$ = this.eventsSubject.asObservable();
  readonly connectionStatus = computed(() => this._connectionStatus());
  readonly reconnectCount = computed(() => this._reconnectCount());

  async connect(): Promise<void> {
    if (!isGameRealtimeEnabled()) return;
    this.diagnostics.start();

    if (this.hub?.state === HubConnectionState.Connected) return;

    if (this.connectPromise) {
      await this.connectPromise;
      return;
    }

    if (!this.hub) {
      this.hub = new HubConnectionBuilder()
        .withUrl(this.hubUrl, {
          accessTokenFactory: () => this.auth.getAccessToken(),
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (r) =>
            Math.min(10_000, r.previousRetryCount * 2_000),
        })
        .configureLogging(LogLevel.Warning)
        .build();
      this.registerHubHandlers();
    }

    this._connectionStatus.set('connecting');
    this.connectPromise = this.hub
      .start()
      .then(async () => {
        this.zone.run(() => this._connectionStatus.set('connected'));
        await this.resubscribeAudiences();
      })
      .catch((error) => {
        this.zone.run(() => this._connectionStatus.set('disconnected'));
        throw error;
      })
      .finally(() => {
        this.connectPromise = undefined;
      });

    await this.connectPromise;
  }

  async disconnect(): Promise<void> {
    await this.hub?.stop();
    this.guildSubscriptions.clear();
    this.activeGuildSubscriptions.clear();
    this.raidSubscriptions.clear();
    this.activeRaidSubscriptions.clear();
    this.worldSubscriptionRequested = false;
    this.worldSubscriptionActive = false;
    this.tournamentGroundsSubscriptionOwners.clear();
    this.tournamentGroundsSubscriptionActive = false;
    this._connectionStatus.set('disconnected');
  }

  async reconnect(): Promise<void> {
    await this.disconnect();
    await this.connect();
  }

  async dispose(): Promise<void> {
    await this.disconnect();
    this.hub = undefined;
    this.handlersRegistered = false;
  }

  isConnected(): boolean {
    return this.hub?.state === HubConnectionState.Connected;
  }

  async subscribeToWorld(): Promise<void> {
    this.worldSubscriptionRequested = true;
    await this.ensureConnected();
    if (this.worldSubscriptionActive) return;

    await this.hub?.invoke('SubscribeToWorld');
    this.worldSubscriptionActive = true;
  }

  async subscribeToGuild(guildId: string): Promise<void> {
    this.guildSubscriptions.add(guildId);
    await this.ensureConnected();
    if (this.activeGuildSubscriptions.has(guildId)) return;

    await this.hub?.invoke('SubscribeToGuild', guildId);
    this.activeGuildSubscriptions.add(guildId);
  }

  async setGuildSubscription(guildId: string | null): Promise<void> {
    const obsoleteGuildIds = [...this.guildSubscriptions].filter(
      (existingGuildId) => existingGuildId !== guildId,
    );

    for (const obsoleteGuildId of obsoleteGuildIds) {
      this.guildSubscriptions.delete(obsoleteGuildId);
      if (
        this.activeGuildSubscriptions.delete(obsoleteGuildId) &&
        this.hub?.state === HubConnectionState.Connected
      ) {
        await this.hub.invoke('UnsubscribeFromGuild', obsoleteGuildId);
      }
    }

    if (guildId) {
      await this.subscribeToGuild(guildId);
    }
  }

  async subscribeToRaid(raidRunId: string): Promise<void> {
    this.raidSubscriptions.add(raidRunId);
    await this.ensureConnected();
    if (this.activeRaidSubscriptions.has(raidRunId)) return;

    await this.hub?.invoke('SubscribeToRaid', raidRunId);
    this.activeRaidSubscriptions.add(raidRunId);
  }

  async setRaidSubscription(raidRunId: string | null): Promise<void> {
    const obsoleteRaidIds = [...this.raidSubscriptions].filter(
      (existingRaidId) => existingRaidId !== raidRunId,
    );

    for (const obsoleteRaidId of obsoleteRaidIds) {
      this.raidSubscriptions.delete(obsoleteRaidId);
      if (
        this.activeRaidSubscriptions.delete(obsoleteRaidId) &&
        this.hub?.state === HubConnectionState.Connected
      ) {
        await this.hub.invoke('UnsubscribeFromRaid', obsoleteRaidId);
      }
    }

    if (raidRunId) {
      await this.subscribeToRaid(raidRunId);
    }
  }

  async setTournamentGroundsSubscription(
    active: boolean,
    owner = 'default',
  ): Promise<void> {
    if (active) this.tournamentGroundsSubscriptionOwners.add(owner);
    else this.tournamentGroundsSubscriptionOwners.delete(owner);

    if (this.tournamentGroundsSubscriptionOwners.size === 0) {
      if (
        this.tournamentGroundsSubscriptionActive &&
        this.hub?.state === HubConnectionState.Connected
      ) {
        await this.hub.invoke('UnsubscribeFromTournamentGrounds');
      }
      this.tournamentGroundsSubscriptionActive = false;
      return;
    }

    await this.ensureTournamentGroundsSubscription();
  }

  private registerHubHandlers(): void {
    if (!this.hub || this.handlersRegistered) return;
    this.handlersRegistered = true;

    this.hub.on('ReceiveEvent', (envelope: GameRealtimeEnvelope) => {
      this.diagnostics.recordReceive(envelope);
      this.zone.run(() => this.eventsSubject.next(envelope));
    });

    this.hub.onreconnecting((error) => {
      this.activeGuildSubscriptions.clear();
      this.activeRaidSubscriptions.clear();
      this.worldSubscriptionActive = false;
      this.tournamentGroundsSubscriptionActive = false;
      this.zone.run(() => this._connectionStatus.set('reconnecting'));
      if (error) console.warn('Game realtime reconnecting', error);
    });

    this.hub.onreconnected(() => {
      this.zone.run(() => {
        this._connectionStatus.set('connected');
        this._reconnectCount.update((count) => count + 1);
      });
      void this.resubscribeAudiences();
    });

    this.hub.onclose((error) => {
      this.activeGuildSubscriptions.clear();
      this.activeRaidSubscriptions.clear();
      this.worldSubscriptionActive = false;
      this.tournamentGroundsSubscriptionActive = false;
      this.zone.run(() => this._connectionStatus.set('disconnected'));
      if (error) console.warn('Game realtime disconnected', error);
    });
  }

  private async ensureConnected(): Promise<void> {
    if (this.hub?.state === HubConnectionState.Connected) return;
    await this.connect();
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

    for (const raidRunId of this.raidSubscriptions) {
      try {
        await this.subscribeToRaid(raidRunId);
      } catch (error) {
        console.warn('Failed to resubscribe to raid realtime', error);
      }
    }

    if (this.tournamentGroundsSubscriptionOwners.size > 0) {
      try {
        await this.ensureTournamentGroundsSubscription();
      } catch (error) {
        console.warn(
          'Failed to resubscribe to Tournament Grounds realtime',
          error,
        );
      }
    }
  }

  private async ensureTournamentGroundsSubscription(): Promise<void> {
    await this.ensureConnected();
    if (this.tournamentGroundsSubscriptionActive) return;
    await this.hub?.invoke('SubscribeToTournamentGrounds');
    this.tournamentGroundsSubscriptionActive = true;
  }
}
