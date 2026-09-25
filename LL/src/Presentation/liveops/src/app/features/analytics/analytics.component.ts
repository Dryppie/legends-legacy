import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LiveOpsApiService } from '../../liveops-api.service';
import { TelemetrySnapshot } from '../../liveops.models';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule],
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
  `],
})
export class AnalyticsComponent implements OnInit {
  reports: TelemetrySnapshot[] = [];
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
    this.loading = true;
    this.error = '';
    try {
      const result = await this.api.analyticsOverview(this.days);
      if (!result.isSuccess || !result.data) throw new Error(result.errorMessage || 'Could not load analytics.');
      this.reports = result.data;
      if (!this.reports.some(x => x.reportDateUtc === this.selectedDate))
        this.selectedDate = this.reports[0]?.reportDateUtc ?? '';
    } catch (error) {
      this.error = error instanceof Error ? error.message : 'Could not load analytics.';
    } finally { this.loading = false; }
  }
}
