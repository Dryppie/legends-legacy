import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LiveOpsApiService } from './liveops-api.service';
import {
  ApiResponse,
  ItemCatalogEntry,
  OperatorSession,
  PlayerDetails,
  PlayerSummary,
  TimelineEntry,
} from './liveops.models';

type WorkspaceSection = 'account' | 'chat' | 'grant' | 'audit';
type DurationOption = '1h' | '24h' | '7d' | 'permanent';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  readonly permissions = {
    read: 'liveops.read',
    account: 'liveops.accounts.moderate',
    chat: 'liveops.chat.moderate',
    economy: 'liveops.economy.compensate',
    superadmin: 'liveops.superadmin',
  };

  session: OperatorSession | null = null;
  authenticationRequired = false;
  loadingSession = true;
  searchQuery = '';
  searchResults: PlayerSummary[] = [];
  selectedPlayer: PlayerDetails | null = null;
  activeSection: WorkspaceSection = 'account';
  loadingSearch = false;
  loadingPlayer = false;
  busyAction = '';
  message = '';
  messageTone: 'success' | 'error' | 'info' = 'info';

  banReason = '';
  banNotes = '';
  banDuration: DurationOption = '24h';
  unbanReason = '';
  muteReason = '';
  muteDuration: DurationOption = '1h';
  unmuteReason = '';

  itemQuery = '';
  itemResults: ItemCatalogEntry[] = [];
  selectedItem: ItemCatalogEntry | null = null;
  grantQuantity = 1;
  grantReason = '';
  grantNotes = '';
  loadingItems = false;

  private readonly pendingOperationIds = new Map<string, string>();

  constructor(private readonly api: LiveOpsApiService) {}

  async ngOnInit(): Promise<void> {
    try {
      this.session = await this.api.session();
      await this.api.initializeAntiforgery();
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        this.authenticationRequired = true;
      } else {
        this.showError(this.errorMessage(error));
      }
    } finally {
      this.loadingSession = false;
    }
  }

  login(): void {
    window.location.assign('/auth/login?returnUrl=/');
  }

  async logout(): Promise<void> {
    try {
      await this.api.logout();
      window.location.assign('/');
    } catch (error) {
      this.showError(this.errorMessage(error));
    }
  }

  hasPermission(permission: string): boolean {
    return !!this.session &&
      (this.session.permissions.some(
        (value) => value.toLowerCase() === permission.toLowerCase(),
      ) ||
        this.session.permissions.some(
          (value) =>
            value.toLowerCase() === this.permissions.superadmin.toLowerCase(),
        ));
  }

  async searchPlayers(): Promise<void> {
    const query = this.searchQuery.trim();
    if (query.length < 2 && !this.looksLikeGuid(query)) {
      this.showError('Enter at least two characters or a complete ID.');
      return;
    }

    this.loadingSearch = true;
    this.message = '';
    try {
      const response = await this.api.searchPlayers(query);
      if (!response.isSuccess) {
        this.showError(response.errorMessage);
        return;
      }
      this.searchResults = response.data ?? [];
      if (this.searchResults.length === 0) {
        this.showInfo('No players matched that search.');
      }
    } catch (error) {
      this.showError(this.errorMessage(error));
    } finally {
      this.loadingSearch = false;
    }
  }

  async selectPlayer(player: PlayerSummary): Promise<void> {
    this.loadingPlayer = true;
    this.selectedPlayer = null;
    this.activeSection = 'account';
    this.message = '';
    try {
      const response = await this.api.playerDetails(player.characterId);
      if (!response.isSuccess || !response.data) {
        this.showError(response.errorMessage || 'The player could not be loaded.');
        return;
      }
      this.selectedPlayer = response.data;
    } catch (error) {
      this.showError(this.errorMessage(error));
    } finally {
      this.loadingPlayer = false;
    }
  }

  async applyBan(): Promise<void> {
    const player = this.selectedPlayer?.player;
    if (!player || !this.requireReason(this.banReason)) return;

    if (this.banDuration === 'permanent') {
      const typed = window.prompt(
        `Permanent ban: type ${player.characterName} to confirm.`,
      );
      if (typed !== player.characterName) {
        this.showInfo('Permanent ban cancelled.');
        return;
      }
    } else if (!window.confirm(
      `Ban ${player.characterName} for ${this.durationLabel(this.banDuration)}?`,
    )) {
      return;
    }

    await this.runMutation('ban', async (operationId) =>
      this.api.ban(player.accountId, {
        operationId,
        reason: this.banReason.trim(),
        internalNotes: this.cleanOptional(this.banNotes),
        expiresAt: this.expiresAt(this.banDuration),
      }),
    );
  }

  async revokeBan(): Promise<void> {
    const restrictionId = this.selectedPlayer?.player.activeBanId;
    if (!restrictionId || !this.requireReason(this.unbanReason)) return;
    if (!window.confirm('Revoke this account ban now?')) return;

    await this.runMutation('unban', async (operationId) =>
      this.api.unban(restrictionId, {
        operationId,
        reason: this.unbanReason.trim(),
      }),
    );
  }

  async applyMute(): Promise<void> {
    const player = this.selectedPlayer?.player;
    if (!player || !this.requireReason(this.muteReason)) return;
    if (!window.confirm(
      `Mute ${player.characterName} for ${this.durationLabel(this.muteDuration)}?`,
    )) {
      return;
    }

    await this.runMutation('mute', async (operationId) =>
      this.api.mute(player.characterId, {
        operationId,
        reason: this.muteReason.trim(),
        expiresAt: this.expiresAt(this.muteDuration),
      }),
    );
  }

  async revokeMute(): Promise<void> {
    const restrictionId = this.selectedPlayer?.activeMute?.id;
    if (!restrictionId || !this.requireReason(this.unmuteReason)) return;
    if (!window.confirm('Remove this chat mute now?')) return;

    await this.runMutation('unmute', async (operationId) =>
      this.api.unmute(restrictionId, {
        operationId,
        reason: this.unmuteReason.trim(),
      }),
    );
  }

  async searchItems(): Promise<void> {
    const query = this.itemQuery.trim();
    if (query.length < 2) {
      this.showError('Enter at least two characters of an item name or ID.');
      return;
    }

    this.loadingItems = true;
    try {
      const response = await this.api.searchItems(query);
      if (!response.isSuccess) {
        this.showError(response.errorMessage);
        return;
      }
      this.itemResults = response.data ?? [];
      if (this.itemResults.length === 0) {
        this.showInfo('No grantable items matched that search.');
      }
    } catch (error) {
      this.showError(this.errorMessage(error));
    } finally {
      this.loadingItems = false;
    }
  }

  chooseItem(item: ItemCatalogEntry): void {
    this.selectedItem = item;
    this.itemResults = [];
    this.itemQuery = item.name;
  }

  async grantItems(): Promise<void> {
    const player = this.selectedPlayer?.player;
    const item = this.selectedItem;
    if (!player || !item || !this.requireReason(this.grantReason)) return;
    if (!Number.isInteger(this.grantQuantity) || this.grantQuantity < 1) {
      this.showError('Quantity must be a positive whole number.');
      return;
    }

    const riskNote = !item.stackable || this.grantQuantity >= 100
      ? '\nThis is a high-attention grant. Verify the item and quantity carefully.'
      : '';
    if (!window.confirm(
      `Grant ${this.grantQuantity} × ${item.name} (${item.id}) to ${player.characterName}?${riskNote}`,
    )) {
      return;
    }

    await this.runMutation('grant', async (operationId) =>
      this.api.grantItems(player.characterId, {
        operationId,
        itemBaseId: item.id,
        quantity: this.grantQuantity,
        reason: this.grantReason.trim(),
        internalNotes: this.cleanOptional(this.grantNotes),
      }),
    );
  }

  setSection(section: WorkspaceSection): void {
    this.activeSection = section;
    this.message = '';
  }

  get timeline(): TimelineEntry[] {
    if (!this.selectedPlayer) return [];
    const game = this.selectedPlayer.administrationHistory.map((entry) => ({
      operationId: entry.operationId,
      actionType: entry.actionType,
      actorDisplayName: entry.actorDisplayName,
      reason: entry.reason,
      occurredAt: entry.occurredAt,
      source: 'Game' as const,
    }));
    const chat = this.selectedPlayer.chatHistory.map((entry) => ({
      operationId: entry.operationId,
      actionType: entry.actionType,
      actorDisplayName: entry.actorDisplayName,
      reason: entry.reason,
      occurredAt: entry.occurredAt,
      source: 'Chat' as const,
    }));
    return [...game, ...chat].sort(
      (a, b) => Date.parse(b.occurredAt) - Date.parse(a.occurredAt),
    );
  }

  private async runMutation(
    kind: string,
    request: (operationId: string) => Promise<ApiResponse<unknown>>,
  ): Promise<void> {
    this.busyAction = kind;
    this.message = '';
    const operationId = this.operationId(kind);
    try {
      const response = await request(operationId);
      if (!response.isSuccess) {
        this.showError(response.errorMessage);
        return;
      }
      this.pendingOperationIds.delete(kind);
      this.showSuccess(`Operation completed. Reference: ${operationId}`);
      await this.refreshSelected();
    } catch (error) {
      this.showError(
        `${this.errorMessage(error)} Retry to safely reuse operation ${operationId}.`,
      );
    } finally {
      this.busyAction = '';
    }
  }

  private async refreshSelected(): Promise<void> {
    const characterId = this.selectedPlayer?.player.characterId;
    if (!characterId) return;
    const response = await this.api.playerDetails(characterId);
    if (response.isSuccess && response.data) {
      this.selectedPlayer = response.data;
      this.searchResults = this.searchResults.map((player) =>
        player.characterId === characterId ? response.data!.player : player,
      );
    }
  }

  private operationId(kind: string): string {
    const existing = this.pendingOperationIds.get(kind);
    if (existing) return existing;
    const created = crypto.randomUUID();
    this.pendingOperationIds.set(kind, created);
    return created;
  }

  private requireReason(reason: string): boolean {
    if (reason.trim().length === 0) {
      this.showError('A reason or support reference is required.');
      return false;
    }
    return true;
  }

  private expiresAt(duration: DurationOption): string | null {
    const hours = duration === '1h' ? 1 : duration === '24h' ? 24 : duration === '7d' ? 168 : 0;
    return hours === 0
      ? null
      : new Date(Date.now() + hours * 60 * 60 * 1000).toISOString();
  }

  durationLabel(duration: DurationOption): string {
    return duration === '1h'
      ? '1 hour'
      : duration === '24h'
        ? '24 hours'
        : duration === '7d'
          ? '7 days'
          : 'permanently';
  }

  private cleanOptional(value: string): string | null {
    return value.trim().length > 0 ? value.trim() : null;
  }

  private looksLikeGuid(value: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(value);
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) return 'Your operator session has expired. Sign in again.';
      if (error.status === 403) return 'Your staff role does not permit this action.';
      return error.error?.errorMessage ?? error.error?.message ?? error.message;
    }
    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }

  private showSuccess(message: string): void {
    this.message = message;
    this.messageTone = 'success';
  }

  private showError(message: string): void {
    this.message = message;
    this.messageTone = 'error';
  }

  private showInfo(message: string): void {
    this.message = message;
    this.messageTone = 'info';
  }
}
