import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { LiveOpsApiService } from '../../liveops-api.service';
import {
  ActionPreview,
  ApiResponse,
  ItemCatalogEntry,
  PlayerDetails,
  PlayerMessageHistoryEntry,
  PlayerTransferHistory,
  PlayerSupportSnapshot,
  PlayerSummary,
  TimelineEntry,
  TransferConversationPage,
} from '../../liveops.models';
import { OperatorContextService } from '../../operator-context.service';
import { ActionPreviewComponent } from '../../shared/action-preview/action-preview.component';
import { SupportSnapshotComponent } from '../../shared/support-snapshot/support-snapshot.component';

type WorkspaceSection = 'support' | 'account' | 'chat' | 'grant' | 'audit';
type DurationOption = '1h' | '24h' | '7d' | 'permanent';

@Component({
  selector: 'app-player-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule, ActionPreviewComponent, SupportSnapshotComponent],
  templateUrl: './player-workspace.component.html',
})
export class PlayerWorkspaceComponent implements OnInit, OnDestroy {
  get permissions() { return this.operator.permissions; }
  searchQuery = '';
  searchResults: PlayerSummary[] = [];
  selectedPlayer: PlayerDetails | null = null;
  supportSnapshot: PlayerSupportSnapshot | null = null;
  supportSnapshotLoading = false;
  supportSnapshotError = '';
  transferHistoryLoading = false;
  transferHistoryError = '';
  selectedTransfer: PlayerTransferHistory | null = null;
  transferConversation: TransferConversationPage | null = null;
  transferConversationLoading = false;
  transferConversationError = '';
  playerMessages: PlayerMessageHistoryEntry[] = [];
  playerMessagesNextCursor: string | null = null;
  playerMessagesLoading = false;
  playerMessagesError = '';
  private playerMessagesLoaded = false;
  activeSection: WorkspaceSection = 'support';
  loadingSearch = false;
  loadingPlayer = false;
  busyAction = '';
  message = '';
  messageTone: 'success' | 'error' | 'info' = 'info';

  banReason = '';
  banNotes = '';
  banDuration: DurationOption = '24h';
  unbanReason = '';
  multiplayerRestrictionReason = '';
  multiplayerRestrictionNotes = '';
  multiplayerRestrictionDuration: DurationOption = '24h';
  multiplayerRestrictionRevokeReason = '';
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

  actionPreview: ActionPreview | null = null;
  previewConfirmation = '';
  previewSubmitting = false;
  private previewKind = '';
  private previewSubmit: (() => Promise<boolean>) | null = null;
  private readonly pendingOperationIds = new Map<string, string>();
  private routeSubscription?: Subscription;

  constructor(
    private readonly api: LiveOpsApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    readonly operator: OperatorContextService,
  ) {}

  ngOnInit(): void {
    this.routeSubscription = this.route.paramMap.subscribe((params) => {
      const characterId = params.get('characterId');
      if (characterId) void this.loadPlayer(characterId);
      else this.clearPlayer();
    });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
  }

  hasPermission(permission: string): boolean {
    return this.operator.hasPermission(permission);
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
      if (!response.isSuccess) { this.showError(response.errorMessage); return; }
      this.searchResults = response.data ?? [];
      if (!this.searchResults.length) this.showInfo('No players matched that search.');
    } catch (error) {
      this.showError(this.errorMessage(error));
    } finally {
      this.loadingSearch = false;
    }
  }

  selectPlayer(player: PlayerSummary): void {
    void this.router.navigate(['/players', player.characterId]);
  }

  async loadSupportSnapshot(characterId?: string): Promise<void> {
    const requestedId = characterId ?? this.selectedPlayer?.player.characterId;
    if (!requestedId) return;
    this.supportSnapshotLoading = true;
    this.supportSnapshotError = '';
    this.transferHistoryError = '';
    try {
      const response = await this.api.playerSupportSnapshot(requestedId);
      if (this.selectedPlayer?.player.characterId !== requestedId) return;
      if (!response.isSuccess || !response.data) {
        this.supportSnapshotError = response.errorMessage || 'The support snapshot could not be loaded.';
        return;
      }
      this.supportSnapshot = response.data;
    } catch (error) {
      if (this.selectedPlayer?.player.characterId === requestedId) {
        this.supportSnapshotError = this.errorMessage(error);
      }
    } finally {
      if (this.selectedPlayer?.player.characterId === requestedId) this.supportSnapshotLoading = false;
    }
  }

  async loadMoreTransfers(): Promise<void> {
    const characterId = this.selectedPlayer?.player.characterId;
    const current = this.supportSnapshot?.transfers.data;
    const cursor = current?.nextCursor;
    if (!characterId || !current || !cursor || this.transferHistoryLoading) return;

    this.transferHistoryLoading = true;
    this.transferHistoryError = '';
    try {
      const response = await this.api.playerTransferHistory(characterId, cursor, current.historyLimit);
      if (this.selectedPlayer?.player.characterId !== characterId ||
          !this.supportSnapshot ||
          this.supportSnapshot.transfers.data !== current) return;
      const section = response.data;
      if (!response.isSuccess || !section?.isAvailable || !section.data) {
        this.transferHistoryError = response.errorMessage || section?.message || 'More transfer history could not be loaded.';
        return;
      }
      const seen = new Set(current.entries.map((entry) => entry.transferId));
      const additions = section.data.entries.filter((entry) => !seen.has(entry.transferId));
      this.supportSnapshot = {
        ...this.supportSnapshot,
        transfers: {
          ...section,
          data: {
            ...section.data,
            entries: [...current.entries, ...additions],
          },
        },
      };
    } catch (error) {
      if (this.selectedPlayer?.player.characterId === characterId) {
        this.transferHistoryError = this.errorMessage(error);
      }
    } finally {
      if (this.selectedPlayer?.player.characterId === characterId) {
        this.transferHistoryLoading = false;
      }
    }
  }

  async inspectTransferConversation(transfer: PlayerTransferHistory): Promise<void> {
    this.selectedTransfer = transfer;
    this.transferConversation = null;
    this.transferConversationError = '';
    await this.loadTransferConversation(false);
  }

  async loadMoreTransferConversation(): Promise<void> {
    await this.loadTransferConversation(true);
  }

  closeTransferConversation(): void {
    this.resetTransferConversation();
  }

  private async loadTransferConversation(loadMore: boolean): Promise<void> {
    const characterId = this.selectedPlayer?.player.characterId;
    const transfer = this.selectedTransfer;
    const current = this.transferConversation;
    const cursor = loadMore ? current?.nextCursor : null;
    if (!characterId || !transfer || this.transferConversationLoading ||
        (loadMore && !cursor)) return;

    this.transferConversationLoading = true;
    this.transferConversationError = '';
    try {
      const response = await this.api.playerTransferConversation(
        characterId,
        transfer.transferId,
        cursor,
        25,
      );
      if (this.selectedPlayer?.player.characterId !== characterId ||
          this.selectedTransfer?.transferId !== transfer.transferId) return;
      if (!response.isSuccess || !response.data) {
        this.transferConversationError = response.errorMessage || 'Transfer conversation evidence could not be loaded.';
        return;
      }

      const existing = loadMore ? current?.messages ?? [] : [];
      const seen = new Set(existing.map((message) => message.id));
      this.transferConversation = {
        ...response.data,
        messages: [
          ...existing,
          ...response.data.messages.filter((message) => !seen.has(message.id)),
        ],
      };
    } catch (error) {
      if (this.selectedPlayer?.player.characterId === characterId &&
          this.selectedTransfer?.transferId === transfer.transferId) {
        this.transferConversationError = this.errorMessage(error);
      }
    } finally {
      if (this.selectedPlayer?.player.characterId === characterId &&
          this.selectedTransfer?.transferId === transfer.transferId) {
        this.transferConversationLoading = false;
      }
    }
  }

  async loadPlayerMessages(loadMore = false): Promise<void> {
    const characterId = this.selectedPlayer?.player.characterId;
    const cursor = loadMore ? this.playerMessagesNextCursor : null;
    if (!characterId || this.playerMessagesLoading || (loadMore && !cursor)) return;

    this.playerMessagesLoading = true;
    this.playerMessagesError = '';
    try {
      const response = await this.api.playerMessageHistory(characterId, cursor, 25);
      if (this.selectedPlayer?.player.characterId !== characterId) return;
      if (!response.isSuccess || !response.data) {
        this.playerMessagesError = response.errorMessage || 'Player message history could not be loaded.';
        return;
      }

      const existing = loadMore ? this.playerMessages : [];
      const seen = new Set(existing.map((entry) => entry.id));
      this.playerMessages = [
        ...existing,
        ...response.data.entries.filter((entry) => !seen.has(entry.id)),
      ];
      this.playerMessagesNextCursor = response.data.nextCursor;
      this.playerMessagesLoaded = true;
    } catch (error) {
      if (this.selectedPlayer?.player.characterId === characterId) {
        this.playerMessagesError = this.errorMessage(error);
      }
    } finally {
      if (this.selectedPlayer?.player.characterId === characterId) {
        this.playerMessagesLoading = false;
      }
    }
  }

  messageChannelLabel(entry: PlayerMessageHistoryEntry): string {
    const context = entry.contextKey.length > 18
      ? `${entry.contextKey.slice(0, 8)}…`
      : entry.contextKey;
    switch (entry.channelType.toLowerCase()) {
      case 'whisper': {
        const target = entry.targetCharacterName
          || (entry.targetCharacterId ? `${entry.targetCharacterId.slice(0, 8)}…` : 'unknown player');
        return `Whisper → ${target}`;
      }
      case 'guild': return `Guild · ${context}`;
      case 'raid': return `Raid · ${context}`;
      default: return entry.channelType;
    }
  }

  async copyIdentifier(value: string, label: string): Promise<void> {
    try { await navigator.clipboard.writeText(value); this.showSuccess(`${label} copied.`); }
    catch { this.showError(`Could not copy the ${label.toLowerCase()}.`); }
  }

  async applyBan(): Promise<void> {
    const player = this.selectedPlayer?.player;
    if (!player || !this.requireReason(this.banReason)) return;
    const body = { reason: this.banReason.trim(), internalNotes: this.cleanOptional(this.banNotes), expiresAt: this.expiresAt(this.banDuration) };
    await this.openActionPreview(
      'ban',
      (operationId) => this.api.previewBan(player.accountId, { operationId, ...body }),
      (previewToken, operationId) => this.api.ban(player.accountId, { previewToken, operationId, ...body }),
    );
  }

  async revokeBan(): Promise<void> {
    const restrictionId = this.selectedPlayer?.player.activeBanId;
    if (!restrictionId || !this.requireReason(this.unbanReason)) return;
    const body = { reason: this.unbanReason.trim() };
    await this.openActionPreview(
      'unban',
      (operationId) => this.api.previewUnban(restrictionId, { operationId, ...body }),
      (previewToken, operationId) => this.api.unban(restrictionId, { previewToken, operationId, ...body }),
    );
  }

  async applyMultiplayerRestriction(): Promise<void> {
    const player = this.selectedPlayer?.player;
    if (!player || !this.requireReason(this.multiplayerRestrictionReason)) return;
    const body = {
      reason: this.multiplayerRestrictionReason.trim(),
      internalNotes: this.cleanOptional(this.multiplayerRestrictionNotes),
      expiresAt: this.expiresAt(this.multiplayerRestrictionDuration),
    };
    await this.openActionPreview(
      'multiplayer-restriction',
      (operationId) => this.api.previewMultiplayerRestriction(player.accountId, { operationId, ...body }),
      (previewToken, operationId) => this.api.restrictMultiplayer(player.accountId, { previewToken, operationId, ...body }),
    );
  }

  async revokeMultiplayerRestriction(): Promise<void> {
    const restrictionId = this.selectedPlayer?.player.activeMultiplayerRestrictionId;
    if (!restrictionId || !this.requireReason(this.multiplayerRestrictionRevokeReason)) return;
    const body = { reason: this.multiplayerRestrictionRevokeReason.trim() };
    await this.openActionPreview(
      'multiplayer-restriction-revoke',
      (operationId) => this.api.previewRevokeMultiplayerRestriction(restrictionId, { operationId, ...body }),
      (previewToken, operationId) => this.api.revokeMultiplayerRestriction(restrictionId, { previewToken, operationId, ...body }),
    );
  }

  async applyMute(): Promise<void> {
    const player = this.selectedPlayer?.player;
    if (!player || !this.requireReason(this.muteReason)) return;
    const body = { reason: this.muteReason.trim(), expiresAt: this.expiresAt(this.muteDuration) };
    await this.openActionPreview(
      'mute',
      (operationId) => this.api.previewMute(player.characterId, { operationId, ...body }),
      (previewToken, operationId) => this.api.mute(player.characterId, { previewToken, operationId, ...body }),
    );
  }

  async revokeMute(): Promise<void> {
    const restrictionId = this.selectedPlayer?.activeMute?.id;
    const characterId = this.selectedPlayer?.player.characterId;
    if (!restrictionId || !characterId || !this.requireReason(this.unmuteReason)) return;
    const body = { characterId, reason: this.unmuteReason.trim() };
    await this.openActionPreview(
      'unmute',
      (operationId) => this.api.previewUnmute(restrictionId, { operationId, ...body }),
      (previewToken, operationId) => this.api.unmute(restrictionId, { previewToken, operationId, ...body }),
    );
  }

  async searchItems(): Promise<void> {
    const query = this.itemQuery.trim();
    if (query.length < 2) { this.showError('Enter at least two characters of an item name or ID.'); return; }
    this.loadingItems = true;
    try {
      const response = await this.api.searchItems(query);
      if (!response.isSuccess) { this.showError(response.errorMessage); return; }
      this.itemResults = response.data ?? [];
      if (!this.itemResults.length) this.showInfo('No grantable items matched that search.');
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
    if (!Number.isInteger(this.grantQuantity) || this.grantQuantity < 1) { this.showError('Quantity must be a positive whole number.'); return; }
    const body = { itemBaseId: item.id, quantity: this.grantQuantity, reason: this.grantReason.trim(), internalNotes: this.cleanOptional(this.grantNotes) };
    await this.openActionPreview(
      'grant',
      (operationId) => this.api.previewGrantItems(player.characterId, { operationId, ...body }),
      (previewToken, operationId) => this.api.grantItems(player.characterId, { previewToken, operationId, ...body }),
    );
  }

  async confirmActionPreview(): Promise<void> {
    if (!this.previewSubmit || this.previewSubmitting) return;
    this.previewSubmitting = true;
    try {
      if (await this.previewSubmit()) this.closeActionPreview(false);
    } finally {
      this.previewSubmitting = false;
    }
  }

  closeActionPreview(cancelOperation = true): void {
    if (cancelOperation && this.previewKind) this.pendingOperationIds.delete(this.previewKind);
    this.actionPreview = null;
    this.previewConfirmation = '';
    this.previewSubmit = null;
    this.previewKind = '';
  }

  setSection(section: WorkspaceSection): void {
    this.activeSection = section;
    this.message = '';
    if (section === 'chat' && !this.playerMessagesLoaded) {
      void this.loadPlayerMessages();
    }
  }

  get timeline(): TimelineEntry[] {
    if (!this.selectedPlayer) return [];
    const game = this.selectedPlayer.administrationHistory.map((entry) => ({ operationId: entry.operationId, actionType: entry.actionType, actorDisplayName: entry.actorDisplayName, reason: entry.reason, occurredAt: entry.occurredAt, source: 'Game' as const }));
    const chat = this.selectedPlayer.chatHistory.map((entry) => ({ operationId: entry.operationId, actionType: entry.actionType, actorDisplayName: entry.actorDisplayName, reason: entry.reason, occurredAt: entry.occurredAt, source: 'Chat' as const }));
    return [...game, ...chat].sort((a, b) => Date.parse(b.occurredAt) - Date.parse(a.occurredAt));
  }

  private async loadPlayer(characterId: string): Promise<void> {
    this.loadingPlayer = true;
    this.selectedPlayer = null;
    this.supportSnapshot = null;
    this.supportSnapshotError = '';
    this.transferHistoryError = '';
    this.transferHistoryLoading = false;
    this.resetTransferConversation();
    this.resetPlayerMessages();
    this.activeSection = 'support';
    this.message = '';
    try {
      const response = await this.api.playerDetails(characterId);
      if (!response.isSuccess || !response.data) { this.showError(response.errorMessage || 'The player could not be loaded.'); return; }
      this.selectedPlayer = response.data;
      void this.loadSupportSnapshot(characterId);
    } catch (error) {
      this.showError(this.errorMessage(error));
    } finally {
      this.loadingPlayer = false;
    }
  }

  private clearPlayer(): void {
    this.selectedPlayer = null;
    this.supportSnapshot = null;
    this.supportSnapshotError = '';
    this.transferHistoryError = '';
    this.transferHistoryLoading = false;
    this.resetTransferConversation();
    this.resetPlayerMessages();
    this.loadingPlayer = false;
    this.activeSection = 'support';
  }

  private resetPlayerMessages(): void {
    this.playerMessages = [];
    this.playerMessagesNextCursor = null;
    this.playerMessagesLoading = false;
    this.playerMessagesError = '';
    this.playerMessagesLoaded = false;
  }

  private resetTransferConversation(): void {
    this.selectedTransfer = null;
    this.transferConversation = null;
    this.transferConversationLoading = false;
    this.transferConversationError = '';
  }

  private async openActionPreview(
    kind: string,
    previewRequest: (operationId: string) => Promise<ApiResponse<ActionPreview>>,
    submitRequest: (previewToken: string, operationId: string) => Promise<ApiResponse<unknown>>,
  ): Promise<void> {
    this.busyAction = kind;
    this.message = '';
    const operationId = this.operationId(kind);
    try {
      const response = await previewRequest(operationId);
      if (!response.isSuccess || !response.data) { this.showError(response.errorMessage || 'The action preview could not be created.'); return; }
      this.actionPreview = response.data;
      this.previewKind = kind;
      this.previewConfirmation = '';
      this.previewSubmit = () => this.runMutation(kind, operationId, () => submitRequest(response.data!.previewToken, operationId));
    } catch (error) {
      this.showError(this.errorMessage(error));
    } finally {
      this.busyAction = '';
    }
  }

  private async runMutation(kind: string, operationId: string, request: () => Promise<ApiResponse<unknown>>): Promise<boolean> {
    this.busyAction = kind;
    this.message = '';
    try {
      const response = await request();
      if (!response.isSuccess) { this.showError(response.errorMessage); return false; }
      this.pendingOperationIds.delete(kind);
      this.showSuccess(`Operation completed. Reference: ${operationId}`);
      await this.refreshSelected();
      return true;
    } catch (error) {
      this.showError(`${this.errorMessage(error)} Retry to safely reuse operation ${operationId}.`);
      return false;
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
      this.searchResults = this.searchResults.map((player) => player.characterId === characterId ? response.data!.player : player);
      void this.loadSupportSnapshot(characterId);
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
    if (!reason.trim()) { this.showError('A reason or support reference is required.'); return false; }
    return true;
  }

  private expiresAt(duration: DurationOption): string | null {
    const hours = duration === '1h' ? 1 : duration === '24h' ? 24 : duration === '7d' ? 168 : 0;
    return hours ? new Date(Date.now() + hours * 60 * 60 * 1000).toISOString() : null;
  }

  private cleanOptional(value: string): string | null { return value.trim() || null; }
  private looksLikeGuid(value: string): boolean { return /^[0-9a-f]{8}-[0-9a-f-]{27}$/i.test(value); }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 401) return 'Your operator session has expired. Sign in again.';
      if (error.status === 403) return 'Your staff role does not permit this action.';
      return error.error?.errorMessage ?? error.error?.message ?? error.message;
    }
    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }

  private showSuccess(message: string): void { this.message = message; this.messageTone = 'success'; }
  private showError(message: string): void { this.message = message; this.messageTone = 'error'; }
  private showInfo(message: string): void { this.message = message; this.messageTone = 'info'; }
}
