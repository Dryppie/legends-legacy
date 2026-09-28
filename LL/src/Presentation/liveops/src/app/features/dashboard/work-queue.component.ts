import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LiveOpsApiService } from '../../liveops-api.service';
import { OperatorContextService } from '../../operator-context.service';

interface WorkItem { id: string; title: string; context: string; updatedAt: string | null; route: string[]; }
interface WorkGroup { title: string; status: string; route: string; total: number | null; items: WorkItem[]; error: string; asOf: string; }

@Component({ selector: 'app-work-queue', standalone: true, imports: [CommonModule, RouterLink], template: `
  <section class="status-section work-queue" aria-labelledby="work-queue-title">
    <div class="status-section-heading"><div><p class="eyebrow">Resume work</p><h2 id="work-queue-title">Your administration queue</h2></div><button class="secondary" [disabled]="loading" (click)="load()">{{ loading ? 'Refreshing work' : 'Refresh work' }}</button></div>
    <p>Open cases need a decision. Waiting cases need a follow-up. Investigation scores are review signals, not a finding of abuse.</p>
    <div class="work-queue-grid">
      @for (group of groups; track group.title) {
        <section class="risk-panel"><header><h3>{{ group.title }}</h3><span>{{ group.total === null ? 'Unknown' : group.total }} total</span></header>
          @if (group.error) { <p class="message error" role="status">{{ group.error }} {{ group.asOf ? 'Showing the last successful results.' : '' }}</p> }
          <ul class="work-list">@for (item of group.items; track item.id) { <li><a [routerLink]="item.route"><strong>{{ item.title }}</strong><span>{{ item.context }}</span></a>@if (item.updatedAt) { <small>Updated {{ item.updatedAt | date:'medium' }}</small> }</li> }
          @empty { @if (group.total === 0 && !group.error) { <li>No work in this queue.</li> } }</ul>
          <footer><a [routerLink]="group.route" [queryParams]="{ status: group.status }">Open full queue</a>@if (group.asOf) { <small>Checked {{ group.asOf | date:'shortTime' }}</small> }</footer>
        </section>
      }
    </div>
    <p class="support-note">Up to five records per queue: cases most recently updated, investigations by risk. Full queues provide pagination. Times: {{ timeZone }}.</p>
  </section>
` })
export class WorkQueueComponent implements OnInit, OnDestroy {
  groups: WorkGroup[] = []; loading = false; private generation = 0;
  readonly timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  constructor(private api: LiveOpsApiService, private operator: OperatorContextService) {}
  ngOnInit(): void {
    if (this.operator.hasPermission(this.operator.permissions.account)) {
      this.groups.push(this.group('Open cases', 'Open', '/cases'), this.group('Waiting for follow-up', 'Waiting', '/cases'));
    }
    if (this.operator.hasPermission(this.operator.permissions.read)) {
      this.groups.push(this.group('Unreviewed investigations', 'Unreviewed', '/account-risk'), this.group('Investigations in progress', 'Investigating', '/account-risk'));
    }
    void this.load();
  }
  ngOnDestroy(): void { this.generation++; }
  private group(title: string, status: string, route: string): WorkGroup { return { title, status, route, items: [], total: null, error: '', asOf: '' }; }
  async load(): Promise<void> {
    if (this.loading) return;
    this.loading = true; const generation = ++this.generation;
    await Promise.all(this.groups.map(async group => {
      try {
        let items: WorkItem[], total: number;
        if (group.route === '/cases') {
          const result = await this.api.cases({ status: group.status, page: '1' });
          if (!result.isSuccess || !result.data) throw new Error(result.errorMessage || 'Cases are unavailable.');
          total = result.data.total; items = result.data.cases.slice(0, 5).map(x => ({ id: x.id, title: x.title, context: `${x.characterName} · ${x.category}`, updatedAt: x.updatedAt, route: ['/cases', x.id] }));
        } else {
          const result = await this.api.accountRisks({ status: group.status, sort: 'risk' }, 1, 5);
          if (!result.isSuccess || !result.data) throw new Error(result.errorMessage || 'Investigations are unavailable.');
          total = result.data.total; items = result.data.entries.map(x => ({ id: x.accountId, title: x.characterName, context: `${x.severity} · Score ${x.score} · ${x.primaryReason}`, updatedAt: x.evaluatedAt, route: ['/account-risk', x.accountId] }));
        }
        if (generation !== this.generation) return;
        Object.assign(group, { items, total, error: '', asOf: new Date().toISOString() });
      } catch (error) { if (generation === this.generation) group.error = error instanceof Error ? error.message : 'Queue unavailable. Retry with Refresh work.'; }
    }));
    if (generation === this.generation) this.loading = false;
  }
}
