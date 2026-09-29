import { Injectable } from '@angular/core';
import { LiveOpsApiService } from './liveops-api.service';
import { ServerOperation } from './liveops.models';

export interface OperationReceipt {
  operationId: string;
  targetId: string;
  action: string;
  outcome: 'Submitting' | 'Unknown' | 'Completed' | 'Rejected';
  recordedAt: string;
  kind?: string;
  source?: 'Game' | 'Chat';
  uncertain?: boolean;
}

@Injectable({ providedIn: 'root' })
export class OperationJournalService {
  private key = '';
  receipts: OperationReceipt[] = [];
  checking = false;
  checkMessage = '';
  recoveryIncomplete = false;
  recoveryMessage = '';

  async restoreServer(api: LiveOpsApiService): Promise<void> {
    const scope = this.key; this.recoveryIncomplete = true; this.recoveryMessage = 'Loading received operation references…';
    try {
      for (let page = 1; page <= 40; page++) {
        const response = await api.operations(page, true);
        if (scope !== this.key) return;
        if (!response.isSuccess || !response.data) throw new Error('The server operation register could not be loaded.');
        for (const row of response.data.entries) {
          if (this.receipts.find(x => x.operationId === row.operationId)?.outcome === 'Completed') continue;
          this.record(row.operationId, row.targetId, row.kind, 'Unknown', row.kind, row.source);
        }
        if (page * response.data.pageSize >= response.data.total) {
          this.recoveryIncomplete = false; this.recoveryMessage = ''; return;
        }
      }
      this.recoveryMessage = 'More than 1,000 received operations need review. Use Operations & recovery before preparing another action.';
    } catch { if (scope === this.key) this.recoveryMessage = 'Server operation recovery is unavailable. Refresh recovery before preparing another action; the existing references are retained.'; }
  }

  initialize(subject: string, environment: string): void {
    this.key = `liveops.operations.${environment}.${subject}`;
    this.recoveryIncomplete = false; this.recoveryMessage = '';
    try {
      const stored: unknown = JSON.parse(localStorage.getItem(this.key) || sessionStorage.getItem(this.key) || '[]');
      this.receipts = Array.isArray(stored) ? stored.filter((x): x is OperationReceipt =>
        x && typeof x.operationId === 'string' && typeof x.targetId === 'string' &&
        typeof x.action === 'string' && typeof x.recordedAt === 'string' &&
        ['Submitting', 'Unknown', 'Completed', 'Rejected'].includes(x.outcome))
        .map(x => ({ ...x, outcome: x.outcome === 'Submitting' ? 'Unknown' : x.outcome })) : [];
    } catch { this.receipts = []; }
  }

  record(operationId: string, targetId: string, action: string, outcome: OperationReceipt['outcome'], kind?: string, source?: 'Game' | 'Chat'): void {
    const previous = this.receipts.find(x => x.operationId === operationId);
    // Rejecting a retry does not establish what happened to the original timed-out request.
    const uncertain = previous?.outcome === 'Unknown' || previous?.uncertain === true;
    if (outcome === 'Rejected' && uncertain) outcome = 'Unknown';
    let resolvedCount = 0;
    this.receipts = [{ operationId, targetId, action, outcome, kind: kind ?? previous?.kind, source: source ?? previous?.source, recordedAt: new Date().toISOString() },
      ...this.receipts.filter(x => x.operationId !== operationId)]
      .filter(x => x.outcome === 'Unknown' || x.outcome === 'Submitting' || ++resolvedCount <= 20);
    this.receipts[0].uncertain = outcome === 'Unknown' || (outcome === 'Submitting' && uncertain);
    // Only identifiers and outcomes survive reload: no reason, evidence, payload, or token.
    try { if (this.key) localStorage.setItem(this.key, JSON.stringify(this.receipts)); } catch { /* Storage is optional. */ }
  }

  applyServerOutcome(row: ServerOperation): boolean {
    if (row.outcome === 'Unknown') return false;
    const previous = this.receipts.find(x => x.operationId === row.operationId);
    if (previous?.outcome === 'Completed') return true;
    // Only the server register can establish a definite rejection of the original
    // attempt. An HTTP rejection of a retry still follows record()'s uncertainty guard.
    this.receipts = this.receipts.filter(x => x.operationId !== row.operationId);
    this.record(row.operationId, row.targetId, previous?.action ?? row.kind,
      row.outcome === 'Committed' ? 'Completed' : 'Rejected', row.kind, row.source);
    return true;
  }

  async reconcile(api: LiveOpsApiService, subject: string): Promise<void> {
    if (this.checking) return;
    this.checking = true; this.checkMessage = ''; const scope = this.key;
    let resolved = 0, unavailable = false;
    const pending = this.receipts.filter(x => x.outcome === 'Unknown').slice(0, 20);
    if (!pending.length) { this.checking = false; return; }
    // Bound concurrency and never retry a mutation while checking its receipt.
    for (const entry of pending) {
      if (scope !== this.key) break;
      try {
        try {
          const status = await api.operationStatus(entry.operationId);
          if (scope !== this.key) break;
          if (status.isSuccess && status.data && this.applyServerOutcome(status.data.operation)) { resolved++; continue; }
        } catch { /* Older operations have no register row; retain exact audit lookup. */ }
        const response = await api.audit({ operationId: entry.operationId, source: entry.source ?? 'All' }, null, 10);
        if (scope !== this.key) break;
        if (!response.isSuccess || !response.data) { unavailable = true; continue; }
        const receipt = response.data.entries.find(x => x.operationId === entry.operationId && x.actorSubject === subject && (!entry.source || entry.source === x.source));
        if (receipt && this.receipts.find(x => x.operationId === entry.operationId)?.outcome === 'Unknown') {
          if (receipt.source === 'Chat') {
            try { await api.operationStatus(entry.operationId); } catch { /* Older local-only operations have no register row. */ }
            if (scope !== this.key) break;
          }
          this.record(entry.operationId, entry.targetId, entry.action, 'Completed', entry.kind, receipt.source); resolved++;
        }
        unavailable ||= response.data.unavailableSources.length > 0;
      } catch { unavailable = true; }
    }
    if (scope === this.key) this.checkMessage = `${resolved} confirmed outcome(s) recovered. ${this.unresolved.length} operation(s) still need checking.${unavailable ? ' Some activity sources were unavailable.' : ''}`;
    this.checking = false;
  }

  get unresolved(): OperationReceipt[] { return this.receipts.filter(x => x.outcome === 'Unknown' || x.outcome === 'Submitting'); }
}
