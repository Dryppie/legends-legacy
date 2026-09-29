import { SupportEvidenceComponent } from '../../shared/support-evidence.component';
import { OperatorDraftService } from '../../operator-draft.service';
import { PreparationDraft } from '../../shared/preparation-draft';
import { DraftStatusComponent } from '../../shared/draft-status.component';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { LiveOpsApiService } from '../../liveops-api.service';
import {
  ActionPreview, CompensationPackage, CompensationPackageLine,
  CompensationEquipmentOption,
  CompensationEquipmentOptions,
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
import { WorkspaceStateService } from '../../workspace-state.service';
import { OperationJournalService, OperationReceipt } from '../../operation-journal.service';
import { actionLabel, operationSummary } from '../../shared/admin-presentation';

type WorkspaceSection = 'support' | 'inventory' | 'activity' | 'account' | 'chat' | 'grant' | 'signets' | 'audit';
type DurationOption = '1h' | '24h' | '7d' | 'permanent';

@Component({
  selector: 'app-player-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ActionPreviewComponent, SupportSnapshotComponent, DraftStatusComponent, SupportEvidenceComponent],
  templateUrl: './player-workspace.component.html',
})
export class PlayerWorkspaceComponent implements OnInit, OnDestroy {
  readonly actionLabel = actionLabel;
  readonly timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  searchMessage = '';
  searched = false;
  playerError = '';
  finderCollapsed = false;
  receipt: OperationReceipt | null = null;
  issue = '';
  caseId = ''; caseTitle = ''; caseStatus = '';
  readonly preparation: PreparationDraft;
  private targetId: string | null = null;
  private generation = 0;
  private searchGeneration = 0;
  private snapshotGeneration = 0;
  private querySubscription?: Subscription;
  private previewRefresh: (() => Promise<void>) | null = null;
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
  equipmentOptions: CompensationEquipmentOptions | null = null;
  loadingEquipmentOptions = false;
  equipmentDefinitionId = '';
  equipmentTier = 1;
  equipmentRank = 0;
  equipmentStyleId = '';

  get selectedEquipmentDefinition(): CompensationEquipmentOption | undefined {
    return this.equipmentOptions?.options.find(option => option.definitionId === this.equipmentDefinitionId);
  }

  chooseEquipmentDefinition(): void {
    this.equipmentTier = this.selectedEquipmentDefinition?.minimumTier ?? 1;
    this.equipmentStyleId = this.selectedEquipmentDefinition?.nativeStyleId ?? '';
  }
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
    private readonly workspace: WorkspaceStateService = new WorkspaceStateService(),
    readonly journal: OperationJournalService = new OperationJournalService(),
    drafts?: OperatorDraftService,
  ) { this.preparation = new PreparationDraft(drafts, this, ["usePackageId", "compensationMode", "banReason", "banNotes", "banDuration", "unbanReason", "multiplayerRestrictionReason", "multiplayerRestrictionNotes", "multiplayerRestrictionDuration", "multiplayerRestrictionRevokeReason", "muteReason", "muteDuration", "unmuteReason", "itemQuery", "selectedItem", "grantQuantity", "equipmentDefinitionId", "equipmentTier", "equipmentRank", "equipmentStyleId", "grantReason", "grantNotes", "signetReason", "signetQuantity", "packageId", "packageVersion", "packageName", "packagePurpose", "packageArchived", "packageLines"]); }

  ngOnInit(): void {
    const search = this.workspace.playerSearch;
    this.searchQuery = search.query;
    this.searchResults = search.results;
    this.searchMessage = search.message;
    this.searched = search.searched;
    this.querySubscription = this.route.queryParamMap?.subscribe(params => {
      if (params.has('lookup')) { this.searchQuery = this.workspace.playerSearch.query; this.searchResults = []; this.searchMessage = this.workspace.playerSearch.message; this.finderCollapsed = false; }
      const section = params.get('section');
      if (section && ['support', 'inventory', 'activity', 'account', 'chat', 'grant', 'signets', 'audit'].includes(section)) {
        this.activeSection = section as WorkspaceSection;
      }
      this.caseId = params.get('case') ?? '';
      if (this.caseId) this.finderCollapsed = true;
      void this.loadCaseContext();
      if (this.activeSection === 'chat' && this.selectedPlayer && !this.playerMessagesLoaded) void this.loadPlayerMessages();
    });
    this.routeSubscription = this.route.paramMap.subscribe((params) => {
      const characterId = params.get('characterId');
      if (characterId) void this.loadPlayer(characterId);
      else this.clearPlayer();
    });
  }

  ngOnDestroy(): void {
    this.preparation.detach();
    this.generation++;
    this.searchGeneration++;
    this.snapshotGeneration++;
    this.routeSubscription?.unsubscribe();
    this.querySubscription?.unsubscribe();
  }

  hasPermission(permission: string): boolean {
    return this.operator.hasPermission(permission);
  }

  async searchPlayers(): Promise<void> {
    const query = this.searchQuery.trim();
    if (query.length < 2 && !this.looksLikeGuid(query)) {
      this.searchMessage = 'Enter at least two characters or a complete ID.';
      return;
    }
    this.loadingSearch = true;
    this.searchMessage = '';
    const generation = ++this.searchGeneration;
    try {
      const response = await this.api.searchPlayers(query);
      if (generation !== this.searchGeneration) return;
      if (!response.isSuccess) { this.searchMessage = response.errorMessage; return; }
      this.searchResults = response.data ?? [];
      this.searched = true;
      this.searchMessage = !this.searchResults.length ? 'No players matched that search.' : this.searchResults.length === 20 ? 'Showing the first 20 matches. Refine the name or use an exact account/character ID.' : `${this.searchResults.length} player(s) found.`;
      this.workspace.playerSearch = { query, results: this.searchResults, message: this.searchMessage, searched: true };
    } catch (error) {
      if (generation === this.searchGeneration) this.searchMessage = this.errorMessage(error);
    } finally {
      if (generation === this.searchGeneration) this.loadingSearch = false;
    }
  }

  selectPlayer(player: PlayerSummary): void {
    if (this.previewSubmitting || this.busyAction) return;
    void this.router.navigate(['/players', player.characterId]);
  }

  retryPlayer(): void { if (this.targetId) void this.loadPlayer(this.targetId); }
  backToSearch(): void { void this.router.navigate(['/players']); }
  investigate(issue: string): void {
    this.issue = issue;
    this.setSection(issue === 'reward' ? 'inventory' : issue === 'activity' || issue === 'transfer' ? 'activity' : issue === 'chat' ? 'chat' : 'account');
  }

  async loadSupportSnapshot(characterId?: string): Promise<void> {
    const requestedId = characterId ?? this.selectedPlayer?.player.characterId;
    if (!requestedId) return;
    const generation = this.generation;
    const request = ++this.snapshotGeneration;
    this.supportSnapshotLoading = true;
    this.supportSnapshotError = '';
    this.transferHistoryError = '';
    try {
      const response = await this.api.playerSupportSnapshot(requestedId);
      if (generation !== this.generation || request !== this.snapshotGeneration || this.selectedPlayer?.player.characterId !== requestedId) return;
      if (!response.isSuccess || !response.data) {
        this.supportSnapshotError = response.errorMessage || 'The support snapshot could not be loaded.';
        return;
      }
      this.supportSnapshot = response.data;
    } catch (error) {
      if (generation === this.generation && request === this.snapshotGeneration && this.selectedPlayer?.player.characterId === requestedId) {
        this.supportSnapshotError = this.errorMessage(error);
      }
    } finally {
      if (generation === this.generation && request === this.snapshotGeneration && this.selectedPlayer?.player.characterId === requestedId) this.supportSnapshotLoading = false;
    }
  }

  async loadMoreTransfers(): Promise<void> {
    const generation = this.generation;
    const characterId = this.selectedPlayer?.player.characterId;
    const current = this.supportSnapshot?.transfers.data;
    const cursor = current?.nextCursor;
    if (!characterId || !current || !cursor || this.transferHistoryLoading) return;

    this.transferHistoryLoading = true;
    this.transferHistoryError = '';
    try {
      const response = await this.api.playerTransferHistory(characterId, cursor, current.historyLimit);
      if (generation !== this.generation || this.selectedPlayer?.player.characterId !== characterId ||
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
      if (generation === this.generation && this.selectedPlayer?.player.characterId === characterId) {
        this.transferHistoryError = this.errorMessage(error);
      }
    } finally {
      if (generation === this.generation && this.selectedPlayer?.player.characterId === characterId) {
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
    const generation = this.generation;
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
      if (generation !== this.generation || this.selectedPlayer?.player.characterId !== characterId ||
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
      if (generation === this.generation && this.selectedPlayer?.player.characterId === characterId &&
          this.selectedTransfer?.transferId === transfer.transferId) {
        this.transferConversationError = this.errorMessage(error);
      }
    } finally {
      if (generation === this.generation && this.selectedPlayer?.player.characterId === characterId &&
          this.selectedTransfer?.transferId === transfer.transferId) {
        this.transferConversationLoading = false;
      }
    }
  }

  async loadPlayerMessages(loadMore = false): Promise<void> {
    const generation = this.generation;
    const characterId = this.selectedPlayer?.player.characterId;
    const cursor = loadMore ? this.playerMessagesNextCursor : null;
    if (!characterId || this.playerMessagesLoading || (loadMore && !cursor)) return;

    this.playerMessagesLoading = true;
    this.playerMessagesError = '';
    try {
      const response = await this.api.playerMessageHistory(characterId, cursor, 25);
      if (generation !== this.generation || this.selectedPlayer?.player.characterId !== characterId) return;
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
      if (generation === this.generation && this.selectedPlayer?.player.characterId === characterId) {
        this.playerMessagesError = this.errorMessage(error);
      }
    } finally {
      if (generation === this.generation && this.selectedPlayer?.player.characterId === characterId) {
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

  async restorePreparation(characterId: string): Promise<void> {
    await this.preparation.open('player:' + characterId);
    if (this.targetId !== characterId || !this.selectedItem || this.selectedItem.itemType !== 'Equipment') return;
    const itemId = this.selectedItem.id, generation = this.generation;
    try { const result = await this.api.compensationEquipmentOptions(characterId, itemId);
      if (generation === this.generation && this.selectedItem?.id === itemId) this.equipmentOptions = result.data;
    } catch { if (generation === this.generation) this.showInfo('Equipment options could not be refreshed. Select the item again before reviewing a grant.'); }
  }
  private async loadCaseContext(): Promise<void> {
    const caseId = this.caseId, generation = this.generation;
    this.caseTitle = ''; this.caseStatus = '';
    if (!caseId || !this.selectedPlayer || !this.hasPermission(this.permissions.account)) return;
    try {
      const result = await this.api.supportCase(caseId);
      if (generation !== this.generation || caseId !== this.caseId) return;
      if (result.data?.case.characterId === this.selectedPlayer?.player.characterId) {
        this.caseTitle = result.data.case.title; this.caseStatus = result.data.case.status;
      } else { this.caseId = ''; this.showInfo('The linked case could not be verified for this player. Open the correct case before recording findings.'); }
    } catch { if (generation === this.generation && caseId === this.caseId) this.caseTitle = 'Case unavailable — open to retry'; }
  }
  async copyReceiptSummary(): Promise<void> {
    const receipt = this.receipt, generation = this.generation;
    if (!receipt) return;
    try {
      const result = await this.api.audit({ operationId: receipt.operationId, source: receipt.source ?? 'All' }, null, 10);
      if (generation !== this.generation) return;
      const entry = result.data?.entries.find(x => x.operationId === receipt.operationId);
      if (!result.isSuccess || !entry) throw new Error('Receipt is unavailable. Open the activity log before copying a summary.');
      await navigator.clipboard.writeText(operationSummary(entry, this.operator.session?.environment ?? 'Unknown'));
      this.showSuccess('Internal operation summary copied. It includes the recorded reason; review it before sharing.');
    } catch (error) { if (generation === this.generation) this.showError(this.errorMessage(error)); }
  }

  async applyBan(): Promise<void> {
    const player = this.selectedPlayer?.player;
    if (!player || !this.requireReason(this.banReason)) return;
    const body = { reason: this.banReason.trim(), internalNotes: this.cleanOptional(this.banNotes), expiresAt: null as string | null, durationMinutes: this.durationMinutes(this.banDuration) };
    await this.openActionPreview(
      'ban',
      (operationId) => this.bindExpiry(this.api.previewBan(player.accountId, { operationId, ...body }), body),
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
      expiresAt: null as string | null, durationMinutes: this.durationMinutes(this.multiplayerRestrictionDuration),
    };
    await this.openActionPreview(
      'multiplayer-restriction',
      (operationId) => this.bindExpiry(this.api.previewMultiplayerRestriction(player.accountId, { operationId, ...body }), body),
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
    const body = { reason: this.muteReason.trim(), expiresAt: null as string | null, durationMinutes: this.durationMinutes(this.muteDuration) };
    await this.openActionPreview(
      'mute',
      (operationId) => this.bindExpiry(this.api.previewMute(player.characterId, { operationId, ...body }), body),
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
    const generation = this.generation;
    const query = this.itemQuery.trim();
    if (query.length < 2) { this.showError('Enter at least two characters of an item name or ID.'); return; }
    this.loadingItems = true;
    try {
      const response = await this.api.searchItems(query);
      if (generation !== this.generation || query !== this.itemQuery.trim()) return;
      if (!response.isSuccess) { this.showError(response.errorMessage); return; }
      this.itemResults = response.data ?? [];
      if (!this.itemResults.length) this.showInfo('No grantable items matched that search.');
    } catch (error) {
      if (generation === this.generation) this.showError(this.errorMessage(error));
    } finally {
      if (generation === this.generation) this.loadingItems = false;
    }
  }

  async chooseItem(item: ItemCatalogEntry): Promise<void> {
    const generation = this.generation;
    this.selectedItem = item;
    this.itemResults = [];
    this.itemQuery = item.name;
    this.equipmentOptions = null;
    this.equipmentDefinitionId = '';
    this.loadingEquipmentOptions = false;
    this.equipmentRank = 0;
    const characterId = this.selectedPlayer?.player.characterId;
    if (!characterId || item.itemType !== 'Equipment') return;
    this.loadingEquipmentOptions = true;
    try {
      const result = await this.api.compensationEquipmentOptions(characterId, item.id);
      if (generation !== this.generation || this.selectedPlayer?.player.characterId !== characterId || this.selectedItem !== item) return;
      if (!result.isSuccess || !result.data) { this.showError(result.errorMessage || 'Equipment options could not be loaded.'); return; }
      this.equipmentOptions = result.data;
      this.equipmentDefinitionId = result.data.options[0]?.definitionId ?? '';
      this.chooseEquipmentDefinition();
    } catch (error) {
      if (generation === this.generation && this.selectedPlayer?.player.characterId === characterId && this.selectedItem === item) this.showError(this.errorMessage(error));
    } finally {
      if (generation === this.generation && this.selectedItem === item) this.loadingEquipmentOptions = false;
    }
  }

  async grantItems(): Promise<void> {
    const player = this.selectedPlayer?.player;
    const item = this.selectedItem;
    if (!player || !item || !this.requireReason(this.grantReason)) return;
    if (!Number.isInteger(this.grantQuantity) || this.grantQuantity < 1) { this.showError('Quantity must be a positive whole number.'); return; }
    if (item.itemType === 'Equipment' && !this.equipmentOptions) { this.showError('Load the equipment options before previewing this grant.'); return; }
    if (this.equipmentOptions?.usesEquipmentProgression && (!this.selectedEquipmentDefinition || this.grantQuantity > this.equipmentOptions.maximumQuantity)) {
      this.showError('Select a supported equipment definition and stay within the equipment grant limit.'); return;
    }
    const equipment = this.equipmentOptions?.usesEquipmentProgression ? {
      definitionId: this.equipmentDefinitionId, tier: this.equipmentTier, rank: this.equipmentRank, activeStyleId: this.equipmentStyleId || null,
    } : null;
    const body = { itemBaseId: item.id, quantity: this.grantQuantity, reason: this.grantReason.trim(), internalNotes: this.cleanOptional(this.grantNotes), equipment };
    await this.openActionPreview(
      'grant',
      (operationId) => this.api.previewGrantItems(player.characterId, { operationId, ...body }),
      (previewToken, operationId) => this.api.grantItems(player.characterId, { previewToken, operationId, ...body }),
    );
  }

  readonly packageItemNames: Record<string, string> = {};
  async resolvePackageItems(lines: CompensationPackageLine[]): Promise<void> {
    const generation = this.generation;
    await Promise.all([...new Set(lines.map(x => x.itemBaseId))].filter(id => !this.packageItemNames[id]).map(async id => {
      try { const result = await this.api.searchItems(id); const item = result.data?.find(x => x.id === id); if (generation === this.generation && item) this.packageItemNames[id] = item.name; } catch { /* The final server review remains authoritative. */ }
    }));
  }
  compensationMode = 'use'; packageSearch = ''; usePackageId = '';
  get filteredPackages() { const q = this.packageSearch.trim().toLowerCase(); return this.packages.filter(x => !x.archived && (!q || `${x.name} ${x.purpose}`.toLowerCase().includes(q))); }
  get usePackage() { return this.packages.find(x => x.packageId === this.usePackageId && !x.archived); }
  packages: CompensationPackage[] = []; packagesLoading = false; packageError = ''; packageLoaded = false;
  packageId = ''; packageVersion = 0; packageName = ''; packagePurpose = ''; packageArchived = false;
  packageLines: CompensationPackageLine[] = [];
  private packageSaveOperation: { key: string; id: string } | null = null;
  async loadPackages(): Promise<void> {
    if (this.packagesLoading) return;
    const generation = this.generation; this.packagesLoading = true; this.packageError = '';
    try {
      const result = await this.api.compensationPackages();
      if (generation !== this.generation) return;
      if (!result.isSuccess || !result.data) throw new Error(result.errorMessage);
      this.packages = result.data; this.packageLoaded = true; if (this.usePackage) void this.resolvePackageItems(this.usePackage.items);
    } catch (error) { if (generation === this.generation) this.packageError = this.errorMessage(error); }
    finally { if (generation === this.generation) this.packagesLoading = false; }
  }
  selectPackage(id: string): void {
    const value = this.packages.find(x => x.packageId === id);
    this.packageId = value?.packageId ?? ''; this.packageVersion = value?.version ?? 0;
    this.packageName = value?.name ?? ''; this.packagePurpose = value?.purpose ?? ''; this.packageArchived = value?.archived ?? false;
    this.packageLines = structuredClone(value?.items ?? []); this.packageSaveOperation = null;
    if (value) void this.resolvePackageItems(value.items);
  }
  get packageHasEdits(): boolean {
    const saved = this.packages.find(x => x.packageId === this.packageId && x.version === this.packageVersion);
    return saved ? saved.name !== this.packageName || saved.purpose !== this.packagePurpose || saved.archived !== this.packageArchived || JSON.stringify(saved.items) !== JSON.stringify(this.packageLines) : !!(this.packageName || this.packagePurpose || this.packageLines.length);
  }
  editPackage(id: string): void {
    if (this.packageHasEdits) { this.showInfo('Save the package draft or discard its edits before selecting a different package.'); return; }
    this.selectPackage(id); this.preparation.save();
  }
  discardPackageEdits(): void { this.selectPackage(this.packageId); this.preparation.save(); }
  async grantSavedPackage(): Promise<void> {
    const player = this.selectedPlayer?.player, saved = this.usePackage;
    if (!player || !saved || !this.requireReason(this.grantReason)) return;
    const body = { characterId: player.characterId, packageId: saved.packageId, version: saved.version, reason: this.grantReason.trim(), internalNotes: this.cleanOptional(this.grantNotes) };
    await this.openActionPreview('package', id => this.api.previewCompensationPackage({ operationId: id, ...body }),
      (previewToken, operationId) => this.api.grantCompensationPackage({ previewToken, operationId, ...body }));
  }
  addPackageLine(): void {
    if (!this.selectedItem || !Number.isInteger(this.grantQuantity) || this.grantQuantity < 1 || this.packageLines.length >= 10) { this.showError('Select an item and whole positive quantity; a package supports at most 10 lines.'); return; }
    if (this.selectedItem.itemType === 'Equipment' && !this.selectedEquipmentDefinition) { this.showError('Choose supported equipment options first.'); return; }
    this.packageLines = [...this.packageLines, { itemBaseId: this.selectedItem.id, quantity: this.grantQuantity,
      equipment: this.selectedItem.itemType === 'Equipment' ? { definitionId: this.equipmentDefinitionId, tier: this.equipmentTier, rank: this.equipmentRank, activeStyleId: this.equipmentStyleId || null } : null }];
  }
  removePackageLine(index: number): void { this.packageLines = this.packageLines.filter((_, i) => i !== index); }
  async savePackage(): Promise<void> {
    const player = this.selectedPlayer?.player;
    if (!player || this.preparation.blocked || this.busyAction || this.operator.sessionExpired) return;
    if (this.journal.recoveryIncomplete) { this.showError(this.journal.recoveryMessage); return; }
    if (!this.packageId) this.packageId = crypto.randomUUID();
    const payload = { packageId: this.packageId, expectedVersion: this.packageVersion, characterId: player.characterId,
      name: this.packageName.trim(), purpose: this.packagePurpose.trim(), archived: this.packageArchived, items: this.packageLines };
    const key = JSON.stringify(payload);
    if (this.packageSaveOperation?.key !== key) this.packageSaveOperation = { key, id: this.journal.unresolved.find(x => x.targetId === this.packageId && x.kind === 'save-package')?.operationId ?? crypto.randomUUID() };
    const operationId = this.packageSaveOperation.id; const generation = this.generation;
    this.busyAction = 'save-package'; this.journal.record(operationId, this.packageId, 'Save compensation package', 'Submitting', 'save-package', 'Game');
    try {
      const result = await this.api.saveCompensationPackage({ operationId, ...payload });
      this.journal.record(operationId, payload.packageId, 'Save compensation package', result.isSuccess ? 'Completed' : 'Rejected');
      if (generation !== this.generation) return;
      if (!result.isSuccess || !result.data) { this.showError(result.errorMessage); return; }
      this.packages = [result.data, ...this.packages.filter(x => x.packageId !== result.data!.packageId)];
      this.selectPackage(result.data.packageId); this.showSuccess('Package version saved. Review it against the player before granting.');
    } catch (error) {
      this.journal.record(operationId, payload.packageId, 'Save compensation package', error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500 ? 'Rejected' : 'Unknown');
      if (generation === this.generation) this.showError(`${this.errorMessage(error)} Operation: ${operationId}. Retry unchanged after checking the activity log.`);
    } finally { if (generation === this.generation) this.busyAction = ''; }
  }
  async grantPackage(): Promise<void> {
    const player = this.selectedPlayer?.player;
    const saved = this.packages.find(x => x.packageId === this.packageId && x.version === this.packageVersion);
    if (!player || !saved || saved.archived || !this.requireReason(this.grantReason)) { this.showError('Select a saved active package and enter a reason.'); return; }
    if (saved.name !== this.packageName || saved.purpose !== this.packagePurpose || saved.archived !== this.packageArchived || JSON.stringify(saved.items) !== JSON.stringify(this.packageLines)) { this.showError('Save or discard package edits before reviewing a grant.'); return; }
    const body = { characterId: player.characterId, packageId: saved.packageId, version: saved.version,
      reason: this.grantReason.trim(), internalNotes: this.cleanOptional(this.grantNotes) };
    await this.openActionPreview('package', operationId => this.api.previewCompensationPackage({ operationId, ...body }),
      (previewToken, operationId) => this.api.grantCompensationPackage({ previewToken, operationId, ...body }));
  }

  signetQuantity = 1;
  signetReason = '';
  signetConfirmation = false;
  private signetOperation: { operationId: string; characterId: string; quantity: number; reason: string } | null = null;

  async grantAlphaSignets(): Promise<void> {
    const characterId = this.selectedPlayer?.player.characterId;
    const reason = this.signetReason.trim();
    if (!characterId || this.busyAction || !this.requireReason(reason)) return;
    if (!Number.isInteger(this.signetQuantity) || this.signetQuantity < 1 || this.signetQuantity > 1200) {
      this.showError('Select between 1 and 1200 Signets.'); return;
    }
    const body = { quantity: this.signetQuantity, reason };
    await this.openActionPreview('signets',
      operationId => this.api.previewAlphaSignets(characterId, { operationId, ...body }),
      (previewToken, operationId) => this.api.grantAlphaSignets(characterId, { previewToken, operationId, ...body }));
  }

  async confirmActionPreview(): Promise<void> {
    if (!this.previewSubmit || this.previewSubmitting || this.busyAction || this.operator.sessionExpired) return;
    this.previewSubmitting = true;
    try {
      if (await this.previewSubmit()) this.closeActionPreview(false);
    } finally {
      this.previewSubmitting = false;
    }
  }

  closeActionPreview(cancelOperation = true): void {
    if ((this.previewSubmitting || this.busyAction) && cancelOperation) return;
    if (cancelOperation && this.previewKind && !this.journal.unresolved.some(x => x.operationId === this.actionPreview?.operationId)) this.pendingOperationIds.delete(this.previewKind);
    this.actionPreview = null;
    this.previewConfirmation = '';
    this.previewSubmit = null;
    this.previewKind = '';
    this.previewRefresh = null;
  }

  async refreshActionPreview(): Promise<void> { if (!this.previewSubmitting) await this.previewRefresh?.(); }

  savePreparationAfterClick(): void { queueMicrotask(() => this.preparation.save()); }
  setSection(section: WorkspaceSection): void {
    this.preparation.save();
    if (section === 'grant' && !this.packageLoaded && this.hasPermission(this.permissions.economy)) void this.loadPackages();
    this.activeSection = section;
    this.message = '';
    void this.router.navigate([], { relativeTo: this.route, queryParams: { section }, queryParamsHandling: 'merge', replaceUrl: true });
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
    this.preparation.detach();
    const generation = ++this.generation;
    this.targetId = characterId;
    this.resetDrafts();
    this.playerError = '';
    this.loadingPlayer = true;
    this.selectedPlayer = null;
    this.selectedItem = null;
    this.equipmentOptions = null;
    this.loadingEquipmentOptions = false;
    this.supportSnapshot = null;
    this.supportSnapshotError = '';
    this.transferHistoryError = '';
    this.transferHistoryLoading = false;
    this.resetTransferConversation();
    this.resetPlayerMessages();
    const section = this.route.snapshot?.queryParamMap?.get('section');
    this.activeSection = section && ['support', 'inventory', 'activity', 'account', 'chat', 'grant', 'signets', 'audit'].includes(section) ? section as WorkspaceSection : 'support';
    this.message = '';
    try {
      const response = await this.api.playerDetails(characterId);
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) { this.playerError = response.errorMessage || 'The player could not be loaded.'; return; }
      this.selectedPlayer = response.data;
      void this.restorePreparation(characterId);
      void this.loadCaseContext();
      void this.loadSupportSnapshot(characterId);
      if (this.activeSection === 'chat') void this.loadPlayerMessages();
      if (this.activeSection === 'grant' && this.hasPermission(this.permissions.economy)) void this.loadPackages();
    } catch (error) {
      if (generation === this.generation) this.playerError = this.errorMessage(error);
    } finally {
      if (generation === this.generation) this.loadingPlayer = false;
    }
  }

  private clearPlayer(): void {
    this.preparation.detach();
    this.generation++;
    this.targetId = null;
    this.playerError = '';
    this.resetDrafts();
    this.selectedPlayer = null;
    this.selectedItem = null;
    this.equipmentOptions = null;
    this.loadingEquipmentOptions = false;
    this.supportSnapshot = null;
    this.supportSnapshotError = '';
    this.transferHistoryError = '';
    this.transferHistoryLoading = false;
    this.resetTransferConversation();
    this.resetPlayerMessages();
    this.loadingPlayer = false;
    this.activeSection = 'support';
  }

  private resetDrafts(): void {
    this.banReason = this.banNotes = this.unbanReason = this.multiplayerRestrictionReason = this.multiplayerRestrictionNotes = this.multiplayerRestrictionRevokeReason = '';
    this.muteReason = this.unmuteReason = this.grantReason = this.grantNotes = this.signetReason = this.itemQuery = '';
    this.banDuration = this.multiplayerRestrictionDuration = '24h'; this.muteDuration = '1h';
    this.compensationMode = 'use'; this.usePackageId = ''; this.selectPackage(''); this.packages = []; this.packageLoaded = false; this.packagesLoading = false; this.packageError = '';
    this.grantQuantity = this.signetQuantity = 1; this.signetConfirmation = false; this.signetOperation = null;
    this.loadingItems = false; this.itemResults = []; this.pendingOperationIds.clear(); this.receipt = null;
    this.actionPreview = null; this.previewSubmit = null; this.previewRefresh = null; this.previewKind = ''; this.previewConfirmation = '';
    this.busyAction = ''; this.previewSubmitting = false; this.issue = '';
    this.supportSnapshotLoading = false;
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
    if (this.preparation.blocked || this.busyAction || this.previewSubmitting || this.operator.sessionExpired) return;
    if (this.journal.recoveryIncomplete) { this.showError(this.journal.recoveryMessage); return; }
    const generation = this.generation;
    this.busyAction = kind;
    this.message = '';
    const operationId = this.operationId(kind);
    try {
      const response = await previewRequest(operationId);
      if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) { this.showError(response.errorMessage || 'The action preview could not be created.'); return; }
      this.actionPreview = response.data;
      this.previewKind = kind;
      this.previewConfirmation = '';
      this.previewSubmit = () => this.runMutation(kind, operationId, () => submitRequest(response.data!.previewToken, operationId));
      this.previewRefresh = () => this.openActionPreview(kind, previewRequest, submitRequest);
    } catch (error) {
      if (generation === this.generation) this.showError(this.errorMessage(error));
    } finally {
      if (generation === this.generation) this.busyAction = '';
    }
  }

  private async runMutation(kind: string, operationId: string, request: () => Promise<ApiResponse<unknown>>): Promise<boolean> {
    const generation = this.generation;
    const targetId = this.selectedPlayer?.player.characterId ?? '';
    const title = this.actionPreview?.title ?? actionLabel(kind);
    this.journal.record(operationId, targetId, title, 'Submitting', kind, kind === 'mute' || kind === 'unmute' ? 'Chat' : 'Game');
    this.busyAction = kind;
    this.message = '';
    try {
      const response = await request();
      if (!response.isSuccess) {
        this.journal.record(operationId, targetId, title, 'Rejected');
        if (generation === this.generation) this.showError(response.errorMessage);
        return false;
      }
      this.journal.record(operationId, targetId, title, 'Completed');
      if (generation !== this.generation) return true;
      const completedFields: Record<string, string[]> = { ban: ['banReason', 'banNotes'], unban: ['unbanReason'], 'multiplayer-restriction': ['multiplayerRestrictionReason', 'multiplayerRestrictionNotes'], 'multiplayer-restriction-revoke': ['multiplayerRestrictionRevokeReason'], mute: ['muteReason'], unmute: ['unmuteReason'], grant: ['grantReason', 'grantNotes'], package: ['grantReason', 'grantNotes'], signets: ['signetReason'] };
      for (const field of completedFields[kind] ?? []) Reflect.set(this, field, '');
      this.preparation.save();
      this.receipt = this.journal.receipts[0];
      this.pendingOperationIds.delete(kind);
      this.showSuccess(`Operation completed. Reference: ${operationId}`);
      try { await this.refreshSelected(); }
      catch { this.showInfo('Operation completed. The player refresh failed; refresh the record before preparing another action.'); }
      return true;
    } catch (error) {
      const rejected = error instanceof HttpErrorResponse && error.status >= 400 && error.status < 500;
      this.journal.record(operationId, targetId, title, rejected ? 'Rejected' : 'Unknown');
      if (generation === this.generation) this.showError(`${this.errorMessage(error)} ${rejected ? 'Review the request.' : 'Outcome unknown. Check the activity log or retry this same operation.'} Reference: ${operationId}.`);
      return false;
    } finally {
      if (generation === this.generation) this.busyAction = '';
    }
  }

  private async refreshSelected(): Promise<void> {
    const characterId = this.selectedPlayer?.player.characterId;
    if (!characterId) return;
    const generation = this.generation;
    const response = await this.api.playerDetails(characterId);
    if (generation !== this.generation) return;
    if (response.isSuccess && response.data) {
      this.selectedPlayer = response.data;
      this.searchResults = this.searchResults.map((player) => player.characterId === characterId ? response.data!.player : player);
      void this.loadSupportSnapshot(characterId);
    } else throw new Error(response.errorMessage || 'The player refresh failed.');
  }

  private operationId(kind: string): string {
    const player = this.selectedPlayer?.player;
    const targets = [player?.characterId, player?.accountId, player?.activeBanId, player?.activeMultiplayerRestrictionId];
    const existing = this.pendingOperationIds.get(kind) ?? this.journal.unresolved.find(x => targets.includes(x.targetId) && x.kind === kind)?.operationId;
    if (existing) return existing;
    const created = crypto.randomUUID();
    this.pendingOperationIds.set(kind, created);
    return created;
  }

  private requireReason(reason: string): boolean {
    if (!reason.trim()) { this.showError('A reason or support reference is required.'); return false; }
    return true;
  }

  private durationMinutes(duration: DurationOption): number | null {
    return duration === '1h' ? 60 : duration === '24h' ? 1440 : duration === '7d' ? 10080 : null;
  }
  private async bindExpiry(request: Promise<ApiResponse<ActionPreview>>, body: { expiresAt: string | null; durationMinutes: number | null }): Promise<ApiResponse<ActionPreview>> {
    const result = await request;
    if (result.isSuccess && result.data) {
      if (body.durationMinutes && !result.data.effectExpiresAt) throw new Error('The server did not return the reviewed expiry. Refresh before submitting.');
      body.expiresAt = result.data.effectExpiresAt ?? null; body.durationMinutes = null;
    }
    return result;
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
