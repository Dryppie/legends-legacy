import { Injectable } from '@angular/core';
import { LiveOpsApiService } from './liveops-api.service';

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

  initialize(subject: string, environment: string): void {
    this.key = `liveops.operations.${environment}.${subject}`;
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
        const response = await api.audit({ operationId: entry.operationId, source: entry.source ?? 'All' }, null, 10);
        if (scope !== this.key) break;
        if (!response.isSuccess || !response.data) { unavailable = true; continue; }
        const receipt = response.data.entries.find(x => x.operationId === entry.operationId && x.actorSubject === subject && (!entry.source || entry.source === x.source));
        if (receipt && this.receipts.find(x => x.operationId === entry.operationId)?.outcome === 'Unknown') {
          this.record(entry.operationId, entry.targetId, entry.action, 'Completed', entry.kind, receipt.source); resolved++;
        }
        unavailable ||= response.data.unavailableSources.length > 0;
      } catch { unavailable = true; }
    }
    if (scope === this.key) this.checkMessage = `${resolved} completed receipt(s) recovered. ${this.unresolved.length} operation(s) still need checking.${unavailable ? ' Some activity sources were unavailable.' : ''}`;
    this.checking = false;
  }

  get unresolved(): OperationReceipt[] { return this.receipts.filter(x => x.outcome === 'Unknown' || x.outcome === 'Submitting'); }
}
