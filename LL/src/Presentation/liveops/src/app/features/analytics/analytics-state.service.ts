import { downloadCsv } from '../../shared/csv-export';
import { WorkspaceStateService } from '../../workspace-state.service';
import { Injectable, OnDestroy } from '@angular/core';
import { LiveOpsApiService } from '../../liveops-api.service';
import { TelemetrySnapshot, ItemizationReport } from '../../liveops.models';
import { readableName } from '../../shared/admin-presentation';
import { compareLevelBands, createAdoptionPreferences } from './adoption-preferences';

@Injectable()
export class AnalyticsState implements OnDestroy {
  readonly economyColumns = [
    { label: 'Cohort', description: '7d or 30d means accounts with recorded game activity during that many UTC calendar days, ending on the report day. Their characters’ balances are measured at the snapshot time shown above, not added up over the period.' },
    { label: 'Resource', description: 'The currency being measured: Cinders or Soulstones. Zero balance, P50 and P90 all refer to this currency, and balances are shown in units of it.' },
    { label: 'Level', description: 'Character level at snapshot time. For example, 20–49 includes levels 20 through 49, and 50+ includes level 50 and above. Each row compares characters in the same level band.' },
    { label: 'Characters', description: 'The number of characters included in this cohort and level band, including those with zero balance. Activity is recorded per account, so this does not mean each character was individually played.' },
    { label: 'Zero balance', description: 'How many of these characters hold exactly zero of this currency at snapshot time. This is a character count, not a percentage. These characters are also included when calculating P50 and P90.' },
    { label: 'P50', description: 'The median balance: the middle value when characters’ balances are sorted from lowest to highest. For an even number of characters, this report uses the lower of the two middle values. This describes a typical balance, not the average.' },
    { label: 'P90', description: 'The 90th-percentile balance: at least 90% of these characters hold this amount or less. It shows the upper end of balances, not the average of the richest 10%. In small cohorts, it can be the highest balance.' },
  ];
  comparisonDate = ''; itemSort = 'sample';
  reports: TelemetrySnapshot[] = [];
  itemization: ItemizationReport[] = []; itemizationError = ''; itemizationDate = '';
  contentKind = ''; levelBand = ''; cohortDays = '7'; definition = ''; adoptionDefinition = ''; itemContext = ''; itemSlot = ''; itemTier: number | null = null; itemRules: number | null = null;
  private overviewGeneration = 0;
  private itemizationGeneration = 0;
  private overviewRange = 0;
  private itemizationRange = 0;
  readonly adoptionPreferences = createAdoptionPreferences();
  ngOnDestroy(): void { this.savePreferences(); this.overviewGeneration++; this.itemizationGeneration++; }
  label(value: string): string { return readableName(value.replace(/[-_]/g, ' ')); }
  get itemReport(): ItemizationReport | undefined { return this.itemization.find(x => x.day === this.itemizationDate); }
  get itemCohorts() { return (this.itemReport?.cohorts ?? []).filter(x => (!this.itemContext || x.context === this.itemContext) && (!this.itemSlot || x.slot === this.itemSlot) && (this.itemTier === null || x.tier === this.itemTier) && (this.itemRules === null || x.rulesVersion === this.itemRules)).slice().sort((a, b) => this.itemSort === 'sample' ? b.distinctCharacters - a.distinctCharacters : this.itemSort === 'battles' ? b.battles - a.battles : a.context.localeCompare(b.context)); }
  get itemContexts(): string[] { return [...new Set(this.itemReport?.cohorts.map(x => x.context) ?? [])]; }
  get itemSlots(): string[] { return [...new Set(this.itemReport?.cohorts.map(x => x.slot) ?? [])]; }
  get outcomes() { return (this.selected?.outcomes ?? []).filter(x => (!this.contentKind || x.kind === this.contentKind) && this.matches(x.key)); }
  get economy() { return (this.selected?.economy ?? []).filter(x => (!this.levelBand || x.levelBand === this.levelBand) && (!this.cohortDays || x.cohortDays === +this.cohortDays)); }
  get contentKinds(): string[] { return [...new Set(this.selected?.outcomes.map(x => x.kind) ?? [])]; }
  get levelBands(): string[] { return [...new Set([...(this.selected?.adoption ?? []), ...(this.selected?.economy ?? [])].map(x => x.levelBand))].sort(compareLevelBands); }
  private matches(value: string): boolean { return this.label(value).toLowerCase().includes(this.definition.trim().toLowerCase()) || value.toLowerCase().includes(this.definition.trim().toLowerCase()); }
  get trend(): { date: string; dau: number | null; height: number }[] {
    if (!this.reports.length) return [];
    const end = Date.parse((this.selectedDate || this.reports[0].reportDateUtc) + 'T00:00:00Z');
    const values = Array.from({ length: this.days }, (_, i) => {
      const date = new Date(end - (this.days - i - 1) * 86400000).toISOString().slice(0, 10);
      return { date, dau: this.reports.find(x => x.reportDateUtc === date)?.population.dau ?? null };
    });
    const max = Math.max(1, ...values.map(x => x.dau ?? 0));
    return values.map(x => ({ ...x, height: x.dau === null ? 0 : x.dau / max * 100 }));
  }
  get dailyReports(): TelemetrySnapshot[] { const dates = new Set(this.trend.map(x => x.date)); return this.reports.filter(x => dates.has(x.reportDateUtc)); }
  get comparison(): { current: number | null; previous: number | null; delta: number | null; currentCount: number; previousCount: number } {
    if (!this.reports.length) return { current: null, previous: null, delta: null, currentCount: 0, previousCount: 0 };
    const end = Date.parse((this.selectedDate || this.reports[0].reportDateUtc) + 'T00:00:00Z');
    const current = this.reports.filter(x => { const t = Date.parse(x.reportDateUtc + 'T00:00:00Z'); return t <= end && t > end - this.days * 86400000; });
    const previous = this.reports.filter(x => { const t = Date.parse(x.reportDateUtc + 'T00:00:00Z'); return t <= end - this.days * 86400000 && t > end - this.days * 2 * 86400000; });
    const mean = (rows: TelemetrySnapshot[]) => rows.length ? rows.reduce((sum, x) => sum + x.population.dau, 0) / rows.length : null;
    const a = mean(current), b = mean(previous);
    return { current: a, previous: b, delta: current.length === this.days && previous.length === this.days && b ? (a! - b) / b * 100 : null, currentCount: current.length, previousCount: previous.length };
  }
  selectedDate = '';
  days = 30;
  loading = false;
  itemizationLoading = false;
  error = '';

  private readonly preferenceKeys = ['comparisonDate', 'itemSort', 'days', 'selectedDate', 'itemizationDate', 'contentKind', 'levelBand', 'cohortDays', 'definition', 'adoptionDefinition', 'itemContext', 'itemSlot', 'itemTier', 'itemRules'] as const;
  constructor(private readonly api: LiveOpsApiService, private readonly workspace: WorkspaceStateService = new WorkspaceStateService()) {
    try {
      const saved = JSON.parse(workspace.analyticsPreferences);
      for (const key of this.preferenceKeys) if (key in saved && (typeof saved[key] === typeof this[key] || (this[key] === null && typeof saved[key] === 'number'))) Reflect.set(this, key, saved[key]);
      if (![7, 30, 90].includes(this.days)) this.days = 30;
      if (saved.adoptionPreferences) for (const key of Object.keys(this.adoptionPreferences)) {
        if (key in saved.adoptionPreferences) Reflect.set(this.adoptionPreferences, key, saved.adoptionPreferences[key]);
      }
    } catch { /* Older or invalid preferences use the defaults. */ }
  }
  savePreferences(): void {
    this.workspace.analyticsPreferences = JSON.stringify({ ...Object.fromEntries(this.preferenceKeys.map(key => [key, this[key]])), adoptionPreferences: this.adoptionPreferences });
  }
  get itemizationStale(): boolean {
    const expected = new Date(Date.now() - (new Date().getUTCHours() < 3 ? 2 : 1) * 86400000).toISOString().slice(0, 10);
    return !!this.itemization[0] && this.itemization[0].day < expected;
  }
  get earlierReports(): TelemetrySnapshot[] { return this.reports.filter(x => x.reportDateUtc < this.selectedDate); }
  get baseline(): TelemetrySnapshot | undefined { return this.earlierReports.find(x => x.reportDateUtc === this.comparisonDate); }
  economyChange(row: TelemetrySnapshot['economy'][number]): number | null {
    const before = this.baseline?.economy.find(x => x.cohortDays === row.cohortDays && x.levelBand === row.levelBand && x.resource === row.resource);
    return before ? row.p50Balance - before.p50Balance : null;
  }
  contentTrend(kind: string, key: string) {
    return this.trend.map(point => ({ date: point.date, row: this.reports.find(x => x.reportDateUtc === point.date)?.outcomes.find(x => x.kind === kind && x.key === key) }));
  }
  exportPage(page: string): void {
    const rows = page === 'activity' ? this.trend.map(point => ({ reportDate: point.date, missingReport: point.dau === null, ...this.reports.find(x => x.reportDateUtc === point.date)?.population }))
      : page === 'content' ? this.outcomes.map(row => ({ ...row, countingUnit: row.kind.toLowerCase() === 'colosseum' ? 'played games; failed is overlapping losses' : 'mode-specific starts and outcomes on their own dates' }))
      : page === 'economy' ? this.economy.map(row => ({ ...row, zeroBalancePercent: row.characterCount ? row.zeroCount / row.characterCount * 100 : null, p50Change: this.economyChange(row) }))
      : this.itemCohorts.map(row => ({ ...row }));
    downloadCsv(`liveops-${page}-${page === 'itemization' ? this.itemizationDate : this.selectedDate}.csv`, rows, {
      page, reportDay: page === 'itemization' ? this.itemizationDate : this.selectedDate,
      generatedAt: page === 'itemization' ? 'Not supplied by Itemization contract' : this.selected?.generatedAtUtc,
      snapshotAt: page === 'itemization' ? 'Not supplied by Itemization contract' : this.selected?.snapshotAtUtc,
      exportedAt: new Date().toISOString(), rangeDays: this.days, comparisonDay: this.baseline?.reportDateUtc ?? 'None',
      filters: Object.fromEntries(this.preferenceKeys.map(key => [key, this[key]])),
      coverage: page === 'activity' ? `${this.comparison.currentCount}/${this.days} current reports; ${this.comparison.previousCount}/${this.days} previous reports` : 'Filtered rows from the selected retained report',
      interpretation: page === 'itemization' ? this.itemReport?.interpretation : 'Counts keep their original populations. Selections are not combat usage; balances are not currency flows. Missing comparisons are blank; zero is a measured value.'
    });
  }
  get selected(): TelemetrySnapshot | undefined {
    return this.reports.find(x => x.reportDateUtc === this.selectedDate);
  }
  get stale(): boolean {
    const latest = this.reports[0];
    if (!latest) return false;
    const now = new Date();
    const expected = new Date(now.getTime() - (now.getUTCHours() < 3 ? 2 : 1) * 86_400_000)
      .toISOString().slice(0, 10);
    return latest.reportDateUtc < expected;
  }
  percent(numerator: number, denominator: number): string {
    return denominator ? `${Math.round(numerator / denominator * 100)}%` : '—';
  }
  ensureOverview(): Promise<void> {
    return this.overviewRange === this.days ? Promise.resolve() : this.loadOverview();
  }
  ensureItemization(): Promise<void> {
    return this.itemizationRange === this.days ? Promise.resolve() : this.loadItemization();
  }
  async loadOverview(): Promise<void> {
    const generation = ++this.overviewGeneration;
    this.overviewRange = this.days;
    this.loading = true; this.error = '';
    try {
      const response = await this.api.analyticsOverview(Math.min(90, this.days * 2));
      if (generation !== this.overviewGeneration) return;
      if (!response.isSuccess || !response.data) throw new Error(response.errorMessage);
      this.reports = response.data.slice().sort((a, b) => b.reportDateUtc.localeCompare(a.reportDateUtc));
      if (!this.reports.some(x => x.reportDateUtc === this.selectedDate)) this.selectedDate = this.reports[0]?.reportDateUtc ?? '';
    } catch {
      if (generation === this.overviewGeneration) this.error = 'Daily reports could not be refreshed. Previously loaded reports remain visible. Use Refresh to retry.';
    } finally {
      if (generation === this.overviewGeneration) this.loading = false;
    }
  }
  async loadItemization(): Promise<void> {
    const generation = ++this.itemizationGeneration;
    this.itemizationRange = this.days;
    this.itemizationLoading = true; this.itemizationError = '';
    try {
      const response = await this.api.itemizationReports(this.days);
      if (generation !== this.itemizationGeneration) return;
      if (!response.isSuccess || !response.data) throw new Error(response.errorMessage);
      this.itemization = response.data.slice().sort((a, b) => b.day.localeCompare(a.day));
      if (!this.itemization.some(x => x.day === this.itemizationDate)) this.itemizationDate = this.itemization[0]?.day ?? '';
    } catch {
      if (generation === this.itemizationGeneration) this.itemizationError = 'Itemization reports could not be refreshed. Previously loaded reports remain visible. Use Refresh to retry.';
    } finally {
      if (generation === this.itemizationGeneration) this.itemizationLoading = false;
    }
  }
}
