import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PlayerSupportSnapshot } from '../liveops.models';
import { LiveOpsApiService } from '../liveops-api.service';
import { OperatorDraftService } from '../operator-draft.service';

interface Evidence { reference: string; text: string; source: string; at: string; }
@Component({ selector: 'app-support-evidence', standalone: true, imports: [CommonModule, FormsModule], template: `
  <details class="case-form"><summary>Collect evidence for this support case</summary>
    <p>Search the loaded records by item, run or reference. Select up to 12 facts to prepare an internal case note. A snapshot is bounded; absence does not establish missing entitlement.</p>
    <label>Find in loaded evidence<input [(ngModel)]="query" placeholder="Item name or reference" /></label>
    <div class="case-history">@for (row of visible; track row.reference) { <label><input type="checkbox" [checked]="selected.has(row.reference)" (change)="toggle(row.reference)" [disabled]="busy || (!selected.has(row.reference) && selected.size >= 12)" />{{ row.text }}<small>{{ row.source }} · {{ row.at | date:'medium' }} · {{ row.reference }}</small></label> }@empty { <p>No loaded evidence matches. Check section availability and retention below.</p> }</div>
    <p>{{ selected.size }} selected · {{ rows.length }} facts available in this snapshot.</p>
    <button class="secondary" (click)="copy()" [disabled]="!selected.size">Copy selected internal evidence</button>
    @if (caseId) { <button class="primary" (click)="prepareCase()" [disabled]="busy || !selected.size">Prepare note in active case</button> } @else { <p>Open a support case, then use its Investigate link to attach these findings.</p> }
    <p role="status">{{ message }}</p>
  </details>
` })
export class SupportEvidenceComponent implements OnChanges {
  @Input() snapshot: PlayerSupportSnapshot | null = null;
  @Input() caseId = '';
  query = ''; selected = new Set<string>(); message = ''; busy = false;
  private target = '';
  constructor(private api: LiveOpsApiService, private drafts: OperatorDraftService, private router: Router) {}
  ngOnChanges(): void { if (this.target !== this.snapshot?.characterId) { this.selected.clear(); this.query = ''; this.target = this.snapshot?.characterId ?? ''; } }
  get rows(): Evidence[] {
    const s = this.snapshot; if (!s) return [];
    const rows: Evidence[] = [], add = (reference: string, text: string, source: string, at: string) => rows.push({ reference, text, source, at });
    const gear = s.equipment?.data;
    for (const item of gear?.items ?? []) add('item:' + item.instanceId, `${item.name} — ${item.locations.join(', ')}`, s.equipment!.source, s.equipment!.fetchedAtUtc);
    const run = gear?.dungeonRun;
    if (run) {
      add('run:' + run.runId, `${run.name} — ${run.status}; claimed ${run.rewardsClaimedAtUtc ?? 'not recorded'}; ${run.rewardRows.length}/${run.rewardRowCount} reward rows loaded`, s.equipment!.source, s.equipment!.fetchedAtUtc);
      for (const reward of run.rewardRows) add('reward:' + reward.rewardRowId, `${reward.quantity} × ${reward.name}; saved reward in run ${run.runId}`, s.equipment!.source, s.equipment!.fetchedAtUtc);
    }
    for (const item of s.economy.data?.recentAcquisitions ?? []) add('acquisition:' + item.itemInstanceId, `${item.quantity} × ${item.itemName}; acquisition ${item.acquisitionSource}`, s.economy.source, item.acquiredAtUtc);
    for (const [index, grant] of (s.economy.data?.recentCompensationGrants ?? []).entries()) add(`operation:${grant.operationId}:item:${grant.itemBaseId}:row:${index + 1}`, `${grant.quantity} × ${grant.itemName}; compensation recorded`, s.economy.source, grant.occurredAtUtc);
    for (const transfer of s.transfers.data?.entries ?? []) add('transfer:' + transfer.transferId, `${transfer.direction}: ${transfer.quantity} × ${transfer.assetName}`, s.transfers.source, transfer.occurredAtUtc);
    if (s.synchronization.data) add('delivery-snapshot:' + s.synchronization.fetchedAtUtc, `${s.synchronization.data.pendingDeliveries} pending / ${s.synchronization.data.failedDeliveries} failed player deliveries; not correlated to a reward or grant`, s.synchronization.source, s.synchronization.fetchedAtUtc);
    return rows;
  }
  get visible(): Evidence[] { const q = this.query.trim().toLowerCase(); return this.rows.filter(x => !q || `${x.text} ${x.reference}`.toLowerCase().includes(q)); }
  toggle(reference: string): void { if (this.selected.has(reference)) this.selected.delete(reference); else if (this.selected.size < 12) this.selected.add(reference); }
  private text(): string {
    return [`Evidence for character ${this.target}; snapshot ${this.snapshot?.generatedAtUtc}.`, ...this.rows.filter(x => this.selected.has(x.reference)).map(x => `${x.reference}: ${x.text} [${x.source}; ${x.at}]`),
      'Coverage: loaded retained records only; missing facts are not proof of missing entitlement.'].join('\n');
  }
  async copy(): Promise<void> { try { await navigator.clipboard.writeText(this.text()); this.message = 'Internal evidence copied with sources, references and coverage.'; } catch { this.message = 'Clipboard unavailable.'; } }
  async prepareCase(): Promise<void> {
    const caseId = this.caseId, target = this.target, text = this.text(); this.busy = true;
    try {
      const result = await this.api.supportCase(caseId);
      if (!result.isSuccess || result.data?.case.characterId !== target) throw new Error('The case could not be verified for this player.');
      const draft = await this.drafts.open('case:' + caseId);
      if (!draft.loaded || draft.conflict) throw new Error('Resolve the case draft conflict or storage error before attaching evidence.');
      const note = [draft.data['note'], text].filter(Boolean).join('\n\n');
      if (note.length > 4000) throw new Error('The combined note exceeds 4000 characters. Select fewer facts or save the existing case note first.');
      this.drafts.update(draft, { ...draft.data, note }); await this.drafts.save(draft);
      if (draft.error || draft.dirty) throw new Error(draft.error || 'The note draft has not finished saving.');
      if (caseId === this.caseId && target === this.target) await this.router.navigate(['/cases', caseId]);
    } catch (error) { this.message = error instanceof Error ? error.message : 'Evidence could not be prepared.'; }
    finally { this.busy = false; }
  }
}
