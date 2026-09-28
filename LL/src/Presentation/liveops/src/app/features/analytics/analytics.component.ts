import { RouterLink } from '@angular/router';
import { readableName } from '../../shared/admin-presentation';
import { CommonModule } from '@angular/common';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LiveOpsApiService } from '../../liveops-api.service';
import { TelemetrySnapshot, ItemizationReport } from '../../liveops.models';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './analytics.component.html',
  styles: [`
    .analytics { max-width: 1440px; margin: 0 auto; padding: 2rem; }
    .heading { display: flex; justify-content: space-between; gap: 1rem; align-items: start; }
    .eyebrow { color: #c9a153; text-transform: uppercase; letter-spacing: .12em; font-size: .75rem; }
    h1 { margin: .3rem 0; } h2 { margin-top: 2rem; }
    .controls { display: flex; gap: .75rem; align-items: center; flex-wrap: wrap; }
    select, button { background: #1c2632; color: #e9e4d8; border: 1px solid #425063; border-radius: .4rem; padding: .55rem .8rem; }
    .cards { display: grid; grid-template-columns: repeat(auto-fit,minmax(145px,1fr)); gap: .75rem; margin: 1.5rem 0; }
    .card, .panel { background: #111923; border: 1px solid #2c3948; border-radius: .6rem; padding: 1rem; }
    .card span { display: block; color: #aab5c2; font-size: .8rem; } .card strong { font-size: 1.6rem; }
    .panel { overflow-x: auto; margin-top: .75rem; } table { width: 100%; border-collapse: collapse; text-align: left; }
    th, td { padding: .55rem .7rem; border-bottom: 1px solid #293644; white-space: nowrap; }
    th { color: #aab5c2; font-weight: 500; } .note { color: #aab5c2; }
    .error { color: #ffc2ba; }
    .trend-bars { overflow-x: auto; }
    .trend-bars button { padding: 0; min-width: 8px; }
    @media (max-width: 640px) { .analytics { padding: 1rem; } .heading { flex-direction: column; } }
  `],
})
export class AnalyticsComponent implements OnInit, OnDestroy {
  reports: TelemetrySnapshot[] = [];
  itemization: ItemizationReport[] = []; itemizationError = ''; itemizationDate = '';
  contentKind = ''; levelBand = ''; cohortDays = ''; definition = ''; itemContext = ''; itemSlot = ''; itemTier: number | null = null; itemRules: number | null = null;
  private generation = 0;
  ngOnDestroy(): void { this.generation++; }
  label(value: string): string { return readableName(value.replace(/[-_]/g, ' ')); }
  get itemReport(): ItemizationReport | undefined { return this.itemization.find(x => x.day === this.itemizationDate); }
  get itemCohorts() { return (this.itemReport?.cohorts ?? []).filter(x => (!this.itemContext || x.context === this.itemContext) && (!this.itemSlot || x.slot === this.itemSlot) && (this.itemTier === null || x.tier === this.itemTier) && (this.itemRules === null || x.rulesVersion === this.itemRules)); }
  get itemContexts(): string[] { return [...new Set(this.itemReport?.cohorts.map(x => x.context) ?? [])]; }
  get itemSlots(): string[] { return [...new Set(this.itemReport?.cohorts.map(x => x.slot) ?? [])]; }
  get outcomes() { return (this.selected?.outcomes ?? []).filter(x => (!this.contentKind || x.kind === this.contentKind) && this.matches(x.key)); }
  get adoption() { return (this.selected?.adoption ?? []).filter(x => (!this.levelBand || x.levelBand === this.levelBand) && (!this.cohortDays || x.cohortDays === +this.cohortDays) && this.matches(x.key)); }
  get economy() { return (this.selected?.economy ?? []).filter(x => (!this.levelBand || x.levelBand === this.levelBand) && (!this.cohortDays || x.cohortDays === +this.cohortDays)); }
  get contentKinds(): string[] { return [...new Set(this.selected?.outcomes.map(x => x.kind) ?? [])]; }
  get levelBands(): string[] { return [...new Set([...(this.selected?.adoption ?? []), ...(this.selected?.economy ?? [])].map(x => x.levelBand))]; }
  private matches(value: string): boolean { return this.label(value).toLowerCase().includes(this.definition.trim().toLowerCase()) || value.toLowerCase().includes(this.definition.trim().toLowerCase()); }
  get trend(): { date: string; dau: number | null; height: number }[] {
    if (!this.reports.length) return [];
    const end = Date.parse(this.reports[0].reportDateUtc + 'T00:00:00Z');
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
    const end = Date.parse(this.reports[0].reportDateUtc + 'T00:00:00Z');
    const current = this.reports.filter(x => Date.parse(x.reportDateUtc + 'T00:00:00Z') > end - this.days * 86400000);
    const previous = this.reports.filter(x => { const t = Date.parse(x.reportDateUtc + 'T00:00:00Z'); return t <= end - this.days * 86400000 && t > end - this.days * 2 * 86400000; });
    const mean = (rows: TelemetrySnapshot[]) => rows.length ? rows.reduce((sum, x) => sum + x.population.dau, 0) / rows.length : null;
    const a = mean(current), b = mean(previous);
    return { current: a, previous: b, delta: current.length === this.days && previous.length === this.days && b ? (a! - b) / b * 100 : null, currentCount: current.length, previousCount: previous.length };
  }
  selectedDate = '';
  days = 30;
  loading = false;
  error = '';

  constructor(private readonly api: LiveOpsApiService) {}
  ngOnInit(): void { void this.load(); }
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
  async load(): Promise<void> {
    const generation = ++this.generation;
    this.loading = true; this.error = ''; this.itemizationError = '';
    const results = await Promise.allSettled([this.api.analyticsOverview(Math.min(90, this.days * 2)), this.api.itemizationReports(this.days)]);
    if (generation !== this.generation) return;
    const overview = results[0], itemization = results[1];
    if (overview.status === 'fulfilled' && overview.value.isSuccess && overview.value.data) {
      this.reports = overview.value.data.slice().sort((a, b) => b.reportDateUtc.localeCompare(a.reportDateUtc));
      if (!this.reports.some(x => x.reportDateUtc === this.selectedDate)) this.selectedDate = this.reports[0]?.reportDateUtc ?? '';
    } else this.error = overview.status === 'rejected' ? 'Daily reports could not be refreshed. Previously loaded reports remain visible.' : overview.value.errorMessage;
    if (itemization.status === 'fulfilled' && itemization.value.isSuccess && itemization.value.data) {
      this.itemization = itemization.value.data.slice().sort((a, b) => b.day.localeCompare(a.day));
      if (!this.itemization.some(x => x.day === this.itemizationDate)) this.itemizationDate = this.itemization[0]?.day ?? '';
    } else this.itemizationError = itemization.status === 'rejected' ? 'Itemization reports could not be refreshed; previous data may be stale.' : itemization.value.errorMessage;
    this.loading = false;
  }
}
