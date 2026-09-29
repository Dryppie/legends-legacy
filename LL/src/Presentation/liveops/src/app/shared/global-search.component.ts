import { WorkspaceStateService } from '../workspace-state.service';
import { CommonModule } from '@angular/common';
import { Component, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LiveOpsApiService } from '../liveops-api.service';
import { OperatorContextService } from '../operator-context.service';
import { actionLabel, actionEffect } from './admin-presentation';

interface SearchResult { kind: 'Player' | 'Case' | 'Operation'; id: string; label: string; context: string; route: string[]; params?: Record<string, string>; }

@Component({ selector: 'app-global-search', standalone: true, imports: [CommonModule, FormsModule, RouterLink], template: `
  <section class="global-search" aria-label="Global search">
    <form (ngSubmit)="search()"><label for="global-search">Find player, case or operation</label><div class="search-row"><input id="global-search" name="query" [(ngModel)]="query" (ngModelChange)="invalidate()" maxlength="160" placeholder="Player name, email, case title or exact reference" /><button class="primary" [disabled]="loading">{{ loading ? 'Searching' : 'Find' }}</button></div></form>
    @if (searched) { <div class="global-search-results"><div class="workspace-tools"><span role="status">{{ loading ? 'Searching available records…' : results.length + ' result(s)' }}</span><button class="quiet" type="button" (click)="close()">Close search results</button></div>
      @for (warning of warnings; track warning) { <p class="message error" role="status">{{ warning }}</p> }
      <ul class="work-list">@for (result of results; track result.kind + result.id) { <li><a [routerLink]="result.route" [queryParams]="result.params" (click)="close()"><span class="source-pill">{{ result.kind }}</span><strong>{{ result.label }}</strong><span>{{ result.context }}</span></a></li> }</ul>
      @if (!loading && !results.length && !warnings.length) { <p>No matching player or case. Operations require their complete reference.</p> }
      <div class="workspace-tools"><a routerLink="/players" [queryParams]="{ lookup: lookup }" (click)="viewAll('players')">Refine player search</a><a routerLink="/cases" [queryParams]="{ lookup: lookup }" (click)="viewAll('cases')">View all matching cases</a></div>
      <p class="support-note">Up to 20 players and 10 cases are shown. Opening a result never performs an action.</p>
    </div> }
  </section>
` })
export class GlobalSearchComponent implements OnDestroy {
  lookup = '';
  query = ''; searched = false; loading = false; results: SearchResult[] = []; warnings: string[] = []; private generation = 0;
  constructor(private api: LiveOpsApiService, private operator: OperatorContextService, private workspace: WorkspaceStateService = new WorkspaceStateService()) {}
  viewAll(kind: string): void {
    if (kind === 'cases') this.workspace.caseFilters = { search: this.query.trim(), status: '' };
    else this.workspace.playerSearch = { query: this.query.trim(), results: [], message: 'Select Search to load this query.', searched: false };
    this.close();
  }
  ngOnDestroy(): void { this.generation++; }
  invalidate(): void { this.generation++; this.loading = false; this.searched = false; this.results = []; this.warnings = []; }
  close(): void { this.invalidate(); }
  async search(): Promise<void> {
    const query = this.query.trim(), generation = ++this.generation;
    this.lookup = crypto.randomUUID();
    this.results = []; this.warnings = []; this.searched = true;
    if (query.length < 2) { this.warnings = ['Enter at least two characters, or paste a complete reference.']; return; }
    this.loading = true;
    const run = async (label: string, work: () => Promise<SearchResult[]>) => {
      try { const results = await work(); if (generation === this.generation) this.results.push(...results); }
      catch { if (generation === this.generation) this.warnings.push(`${label} search is unavailable. Other results may still be useful.`); }
    };
    const jobs = [run('Player', async () => {
      const response = await this.api.searchPlayers(query); if (!response.isSuccess || !response.data) throw new Error();
      return response.data.map(x => ({ kind: 'Player', id: x.characterId, label: x.characterName, context: `Level ${x.characterLevel} · ${x.accountLabel}`, route: ['/players', x.characterId] }));
    })];
    if (this.operator.hasPermission(this.operator.permissions.account)) jobs.push(run('Case', async () => {
      const response = await this.api.cases({ search: query, page: '1' }); if (!response.isSuccess || !response.data) throw new Error();
      return response.data.cases.slice(0, 10).map(x => ({ kind: 'Case', id: x.id, label: x.title, context: `${x.characterName} · ${x.status}`, route: ['/cases', x.id] }));
    }));
    if (/^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(query)) jobs.push(run('Operation', async () => {
      const response = await this.api.audit({ operationId: query }, null, 10); if (!response.isSuccess || !response.data) throw new Error();
      if (response.data.unavailableSources.length && generation === this.generation) this.warnings.push(`Operation coverage unavailable: ${response.data.unavailableSources.join(', ')}.`);
      return response.data.entries.filter(x => x.operationId.toLowerCase() === query.toLowerCase()).map(x => ({ kind: 'Operation', id: x.source + x.operationId, label: actionLabel(x.actionType), context: `${x.source} · ${actionEffect(x.detailsJson)} · ${new Date(x.occurredAt).toLocaleString()}`, route: ['/audit'], params: { operationId: x.operationId, source: x.source } }));
    }));
    await Promise.all(jobs);
    if (generation === this.generation) { this.results.sort((a, b) => Number(b.id.toLowerCase().endsWith(query.toLowerCase()) || b.label.toLowerCase() === query.toLowerCase()) - Number(a.id.toLowerCase().endsWith(query.toLowerCase()) || a.label.toLowerCase() === query.toLowerCase()) || a.kind.localeCompare(b.kind) || a.label.localeCompare(b.label)); this.loading = false; }
  }
}
