import { CompensationPackage, ItemizationReport, OperationalDetailPage, SupportCasePage, SupportCaseDetails, OperatorDraft, ServerOperationPage, ServerOperationStatus, JobScheduleHealth } from './liveops.models';
import { Injectable } from '@angular/core';
import { StateRefreshRecoveryResult } from './liveops.models';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import {
  ApiResponse,
  ActionPreview,
  CompensationEquipmentOptions,
  AccountRiskDetails,
  AccountRiskFilters,
  AccountRiskOperation,
  AccountRiskPage,
  AccountTemporalCorrelationReport,
  AdministrationAuditFilters,
  AdministrationAuditPage,
  ItemCatalogEntry,
  OperationalStatus,
  TelemetrySnapshot,
  OperatorSession,
  PlayerDetails,
  PlayerMessageHistoryPage,
  PlayerSupportSnapshot,
  PlayerSummary,
  SupportSection,
  TransferHistorySupportSnapshot,
  TransferConversationPage,
  TransferConversationCorrelationReport,
} from './liveops.models';

@Injectable({ providedIn: 'root' })
export class LiveOpsApiService {
  private antiforgeryToken = '';

  constructor(private readonly http: HttpClient) {}
  previewStateRefresh(deliveryId: string, body: object): Promise<ApiResponse<ActionPreview>> { return this.post(`/api/liveops/deliveries/${deliveryId}/state-refresh/preview`, body); }
  retryStateRefresh(deliveryId: string, body: object): Promise<ApiResponse<StateRefreshRecoveryResult>> { return this.post(`/api/liveops/deliveries/${deliveryId}/state-refresh`, body); }
  operations(page = 1, unresolvedOnly = true): Promise<ApiResponse<ServerOperationPage>> {
    return firstValueFrom(this.http.get<ApiResponse<ServerOperationPage>>('/api/liveops/operations', { params: new HttpParams().set('page', page).set('unresolvedOnly', unresolvedOnly) }));
  }
  operationStatus(id: string): Promise<ApiResponse<ServerOperationStatus>> {
    return firstValueFrom(this.http.get<ApiResponse<ServerOperationStatus>>('/api/liveops/operations/' + id));
  }
  jobSchedules(): Promise<ApiResponse<JobScheduleHealth[]>> {
    return firstValueFrom(this.http.get<ApiResponse<JobScheduleHealth[]>>('/api/liveops/status/job-schedules'));
  }

  itemizationReports(days: number): Promise<ApiResponse<ItemizationReport[]>> {
    return firstValueFrom(this.http.get<ApiResponse<ItemizationReport[]>>('/api/liveops/analytics/itemization', { params: new HttpParams().set('days', days) }));
  }

  analyticsOverview(days: number): Promise<ApiResponse<TelemetrySnapshot[]>> {
    return firstValueFrom(this.http.get<ApiResponse<TelemetrySnapshot[]>>(
      '/api/liveops/analytics/overview', { params: new HttpParams().set('days', days) }));
  }

  session(): Promise<OperatorSession> {
    return firstValueFrom(this.http.get<OperatorSession>('/auth/session'));
  }

  operatorDraft(key: string): Promise<ApiResponse<OperatorDraft>> {
    return firstValueFrom(this.http.get<ApiResponse<OperatorDraft>>('/api/liveops/drafts/' + key.replace(':', '/')));
  }
  saveOperatorDraft(key: string, expectedVersion: string, content: string): Promise<ApiResponse<OperatorDraft>> {
    return this.post('/api/liveops/drafts/' + key.replace(':', '/'), { expectedVersion, content });
  }

  async initializeAntiforgery(): Promise<void> {
    const result = await firstValueFrom(
      this.http.get<{ requestToken: string }>('/auth/antiforgery'),
    );
    this.antiforgeryToken = result.requestToken;
  }

  searchPlayers(query: string): Promise<ApiResponse<PlayerSummary[]>> {
    const params = new HttpParams().set('query', query).set('limit', 20);
    return firstValueFrom(
      this.http.get<ApiResponse<PlayerSummary[]>>('/api/liveops/players', {
        params,
      }),
    );
  }

  playerDetails(characterId: string): Promise<ApiResponse<PlayerDetails>> {
    return firstValueFrom(
      this.http.get<ApiResponse<PlayerDetails>>(
        `/api/liveops/players/${characterId}`,
      ),
    );
  }

  playerSupportSnapshot(characterId: string): Promise<ApiResponse<PlayerSupportSnapshot>> {
    return firstValueFrom(
      this.http.get<ApiResponse<PlayerSupportSnapshot>>(
        `/api/liveops/players/${characterId}/support-snapshot`,
      ),
    );
  }

  playerTransferHistory(
    characterId: string,
    cursor: string,
    take = 25,
  ): Promise<ApiResponse<SupportSection<TransferHistorySupportSnapshot>>> {
    const params = new HttpParams().set('cursor', cursor).set('take', take);
    return firstValueFrom(
      this.http.get<ApiResponse<SupportSection<TransferHistorySupportSnapshot>>>(
        `/api/liveops/players/${characterId}/transfers`,
        { params },
      ),
    );
  }

  playerTransferConversation(
    characterId: string,
    transferId: string,
    cursor: string | null = null,
    take = 25,
  ): Promise<ApiResponse<TransferConversationPage>> {
    let params = new HttpParams().set('take', take);
    if (cursor) params = params.set('cursor', cursor);
    return firstValueFrom(
      this.http.get<ApiResponse<TransferConversationPage>>(
        `/api/liveops/players/${characterId}/transfers/${transferId}/conversation`,
        { params },
      ),
    );
  }

  playerMessageHistory(
    characterId: string,
    cursor: string | null = null,
    take = 25,
  ): Promise<ApiResponse<PlayerMessageHistoryPage>> {
    let params = new HttpParams().set('take', take);
    if (cursor) params = params.set('cursor', cursor);
    return firstValueFrom(
      this.http.get<ApiResponse<PlayerMessageHistoryPage>>(
        `/api/liveops/players/${characterId}/messages`,
        { params },
      ),
    );
  }

  operationalDetails(view: string, page = 1, status = ''): Promise<ApiResponse<OperationalDetailPage>> {
    return firstValueFrom(this.http.get<ApiResponse<OperationalDetailPage>>('/api/liveops/status/details', { params: new HttpParams().set('view', view).set('page', page).set('status', status) }));
  }

  operationalStatus(): Promise<ApiResponse<OperationalStatus>> {
    return firstValueFrom(
      this.http.get<ApiResponse<OperationalStatus>>('/api/liveops/status'),
    );
  }

  accountRisks(filters: AccountRiskFilters, page = 1, pageSize = 50): Promise<ApiResponse<AccountRiskPage>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    for (const [name, value] of Object.entries(filters)) {
      if (value?.trim()) params = params.set(name, value.trim());
    }
    return firstValueFrom(this.http.get<ApiResponse<AccountRiskPage>>('/api/liveops/account-risk', { params }));
  }

  accountRiskDetails(accountId: string, transferLimit = 200): Promise<ApiResponse<AccountRiskDetails>> {
    const params = new HttpParams().set('transferLimit', transferLimit);
    return firstValueFrom(this.http.get<ApiResponse<AccountRiskDetails>>(`/api/liveops/account-risk/${accountId}`, { params }));
  }

  accountTemporalCorrelations(accountId: string, windowDays = 90): Promise<ApiResponse<AccountTemporalCorrelationReport>> {
    const params = new HttpParams().set('windowDays', windowDays);
    return firstValueFrom(this.http.get<ApiResponse<AccountTemporalCorrelationReport>>(
      `/api/liveops/account-risk/${accountId}/temporal-correlations`,
      { params },
    ));
  }

  accountTransferConversationCorrelations(accountId: string): Promise<ApiResponse<TransferConversationCorrelationReport>> {
    return firstValueFrom(this.http.get<ApiResponse<TransferConversationCorrelationReport>>(
      `/api/liveops/account-risk/${accountId}/transfer-conversation-correlations`,
    ));
  }

  updateAccountRiskStatus(accountId: string, body: object): Promise<ApiResponse<AccountRiskOperation>> {
    return this.post(`/api/liveops/account-risk/${accountId}/status`, body);
  }

  addAccountRiskNote(accountId: string, body: object): Promise<ApiResponse<AccountRiskOperation>> {
    return this.post(`/api/liveops/account-risk/${accountId}/notes`, body);
  }

  audit(
    filters: AdministrationAuditFilters,
    cursor: string | null = null,
    take = 25,
  ): Promise<ApiResponse<AdministrationAuditPage>> {
    let params = new HttpParams().set('take', take);
    for (const [name, value] of Object.entries(filters)) {
      if (value?.trim()) params = params.set(name, value.trim());
    }
    if (cursor) params = params.set('cursor', cursor);

    return firstValueFrom(
      this.http.get<ApiResponse<AdministrationAuditPage>>(
        '/api/liveops/audit',
        { params },
      ),
    );
  }

  async exportAudit(
    filters: AdministrationAuditFilters,
    from: string,
    to: string,
    operationId: string,
  ): Promise<{ blob: Blob; fileName: string }> {
    const response = await firstValueFrom(this.http.post(
      '/api/liveops/audit/exports',
      {
        operationId,
        from,
        to,
        source: filters.source || null,
        actionType: filters.actionType || null,
        actor: filters.actor || null,
        permission: filters.permission || null,
        reference: filters.reference || null,
        riskLevel: filters.riskLevel || null,
        target: filters.target || null,
        targetOperationId: filters.operationId || null,
      },
      {
        headers: this.mutationHeaders(),
        observe: 'response',
        responseType: 'blob',
      },
    ));
    const disposition = response.headers.get('Content-Disposition') ?? '';
    const fileName = /filename="?([^";]+)"?/i.exec(disposition)?.[1]
      ?? 'liveops-audit.csv';
    return { blob: response.body ?? new Blob(), fileName };
  }

  searchItems(query: string): Promise<ApiResponse<ItemCatalogEntry[]>> {
    const params = new HttpParams().set('query', query).set('limit', 20);
    return firstValueFrom(
      this.http.get<ApiResponse<ItemCatalogEntry[]>>('/api/liveops/items', {
        params,
      }),
    );
  }

  ban(accountId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(`/api/liveops/accounts/${accountId}/bans`, body);
  }

  previewBan(accountId: string, body: object): Promise<ApiResponse<ActionPreview>> {
    return this.post(`/api/liveops/accounts/${accountId}/bans/preview`, body);
  }

  unban(restrictionId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/accounts/bans/${restrictionId}/revoke`,
      body,
    );
  }

  previewUnban(restrictionId: string, body: object): Promise<ApiResponse<ActionPreview>> {
    return this.post(
      `/api/liveops/accounts/bans/${restrictionId}/revoke/preview`,
      body,
    );
  }

  restrictMultiplayer(accountId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(`/api/liveops/accounts/${accountId}/multiplayer-restrictions`, body);
  }

  previewMultiplayerRestriction(accountId: string, body: object): Promise<ApiResponse<ActionPreview>> {
    return this.post(`/api/liveops/accounts/${accountId}/multiplayer-restrictions/preview`, body);
  }

  revokeMultiplayerRestriction(restrictionId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/accounts/multiplayer-restrictions/${restrictionId}/revoke`,
      body,
    );
  }

  previewRevokeMultiplayerRestriction(restrictionId: string, body: object): Promise<ApiResponse<ActionPreview>> {
    return this.post(
      `/api/liveops/accounts/multiplayer-restrictions/${restrictionId}/revoke/preview`,
      body,
    );
  }

  mute(characterId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/chat/characters/${characterId}/mutes`,
      body,
    );
  }

  previewMute(characterId: string, body: object): Promise<ApiResponse<ActionPreview>> {
    return this.post(
      `/api/liveops/chat/characters/${characterId}/mutes/preview`,
      body,
    );
  }

  unmute(restrictionId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/chat/mutes/${restrictionId}/revoke`,
      body,
    );
  }

  previewUnmute(restrictionId: string, body: object): Promise<ApiResponse<ActionPreview>> {
    return this.post(
      `/api/liveops/chat/mutes/${restrictionId}/revoke/preview`,
      body,
    );
  }

  compensationEquipmentOptions(characterId: string, itemBaseId: string): Promise<ApiResponse<CompensationEquipmentOptions>> {
    return firstValueFrom(this.http.get<ApiResponse<CompensationEquipmentOptions>>(`/api/liveops/characters/${characterId}/item-grants/equipment-options`, { params: new HttpParams().set('itemBaseId', itemBaseId) }));
  }

  grantItems(characterId: string, body: object): Promise<ApiResponse<unknown>> {
    return this.post(
      `/api/liveops/characters/${characterId}/item-grants`,
      body,
    );
  }

  previewAlphaSignets(characterId: string, body: object): Promise<ApiResponse<ActionPreview>> {
    return this.post(`/api/liveops/characters/${characterId}/signets/preview`, body);
  }

  grantAlphaSignets(characterId: string, body: { previewToken: string; operationId: string; quantity: number; reason: string }): Promise<ApiResponse<unknown>> {
    return this.post(`/api/liveops/characters/${characterId}/signets`, body);
  }

  previewGrantItems(characterId: string, body: object): Promise<ApiResponse<ActionPreview>> {
    return this.post(
      `/api/liveops/characters/${characterId}/item-grants/preview`,
      body,
    );
  }

  cases(filters: Record<string, string>): Promise<ApiResponse<SupportCasePage>> {
    return firstValueFrom(this.http.get<ApiResponse<SupportCasePage>>('/api/liveops/cases', { params: new HttpParams({ fromObject: filters }) }));
  }
  supportCase(caseId: string, beforeSequence?: number): Promise<ApiResponse<SupportCaseDetails>> {
    return firstValueFrom(this.http.get<ApiResponse<SupportCaseDetails>>(`/api/liveops/cases/${caseId}`, {
      params: beforeSequence ? new HttpParams().set('beforeSequence', beforeSequence) : new HttpParams() }));
  }
  createCase(body: object): Promise<ApiResponse<SupportCaseDetails>> { return this.post('/api/liveops/cases', body); }
  changeCase(caseId: string, action: 'notes' | 'status' | 'operations' | 'follow-up', body: object): Promise<ApiResponse<SupportCaseDetails>> {
    return this.post(`/api/liveops/cases/${caseId}/${action}`, body);
  }

  compensationPackages(): Promise<ApiResponse<CompensationPackage[]>> { return firstValueFrom(this.http.get<ApiResponse<CompensationPackage[]>>('/api/liveops/compensation-packages')); }
  saveCompensationPackage(body: object): Promise<ApiResponse<CompensationPackage>> { return this.post('/api/liveops/compensation-packages', body); }
  previewCompensationPackage(body: object): Promise<ApiResponse<ActionPreview>> { return this.post('/api/liveops/compensation-packages/preview', body); }
  grantCompensationPackage(body: object): Promise<ApiResponse<unknown>> { return this.post('/api/liveops/compensation-packages/grant', body); }

  logout(): Promise<unknown> {
    return firstValueFrom(
      this.http.post('/auth/logout', {}, { headers: this.mutationHeaders(), responseType: 'text' }),
    );
  }

  private post<T>(path: string, body: object): Promise<ApiResponse<T>> {
    return firstValueFrom(
      this.http.post<ApiResponse<T>>(path, body, {
        headers: this.mutationHeaders(),
      }),
    );
  }

  private mutationHeaders(): HttpHeaders {
    if (!this.antiforgeryToken) {
      throw new Error('The operator session is not ready. Refresh and try again.');
    }
    return new HttpHeaders({ 'X-XSRF-TOKEN': this.antiforgeryToken });
  }
}
