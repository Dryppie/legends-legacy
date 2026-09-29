import { downloadCsv } from '../../shared/csv-export';
import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TelemetrySnapshot } from '../../liveops.models';
import { readableName } from '../../shared/admin-presentation';
import { ColumnHelpComponent } from '../../shared/column-help.component';
import { adoptionHelp } from './analytics-column-help';
import { AdoptionColumn as Column, AdoptionSort as Sort, AdoptionPreferences, compareLevelBands, createAdoptionPreferences } from './adoption-preferences';

type Metric = TelemetrySnapshot['adoption'][number];
interface AdoptionRow extends Metric {
  name: string;
  rate: number | null;
  gap: number | null;
  change: number | null;
  baseline: Metric | undefined;
}

@Component({
  selector: 'app-adoption-table',
  standalone: true,
  imports: [CommonModule, FormsModule, ColumnHelpComponent],
  templateUrl: './adoption-table.component.html',
  styleUrl: './adoption-table.component.css',
})
export class AdoptionTableComponent implements OnChanges {
  readonly columnHelp = adoptionHelp;
  @Input({ required: true }) report!: TelemetrySnapshot;
  @Input() reports: TelemetrySnapshot[] = [];
  @Input() definitionFilter = '';
  @Input() levelBand = '';
  @Input() cohortDays = '7';
  @Input() preferences: AdoptionPreferences = createAdoptionPreferences();
  get measure() { return this.preferences.measure; }
  set measure(value: string) { this.preferences.measure = value; }
  get sort() { return this.preferences.sort; }
  set sort(value: Sort) { this.preferences.sort = value; }
  get grouped() { return this.preferences.grouped; }
  set grouped(value: boolean) { this.preferences.grouped = value; }
  get minimumCohort() { return this.preferences.minimumCohort; }
  set minimumCohort(value: number | null) { this.preferences.minimumCohort = value; }
  get comparisonDate() { return this.preferences.comparisonDate; }
  set comparisonDate(value: string) { this.preferences.comparisonDate = value; }

  readonly sortOptions: { value: Sort; label: string }[] = [
    { value: 'rate-desc', label: 'Most adopted' },
    { value: 'rate-asc', label: 'Least adopted' },
    { value: 'observed-desc', label: 'Most characters' },
    { value: 'observed-asc', label: 'Fewest characters' },
    { value: 'gap-desc', label: 'Biggest ownership → loadout gap' },
    { value: 'gap-asc', label: 'Smallest ownership → loadout gap' },
    { value: 'change-desc', label: 'Largest increase' },
    { value: 'change-asc', label: 'Largest decrease' },
    { value: 'name-asc', label: 'Name A–Z' },
    { value: 'name-desc', label: 'Name Z–A' },
    { value: 'level-asc', label: 'Level low to high' },
    { value: 'level-desc', label: 'Level high to low' },
    { value: 'cohort-asc', label: 'Cohort shortest first' },
    { value: 'cohort-desc', label: 'Cohort longest first' },
    { value: 'population-desc', label: 'Largest cohort population' },
    { value: 'population-asc', label: 'Smallest cohort population' },
  ];
  get columns(): { key: Column; label: string }[] {
    return [
      { key: 'name', label: 'Definition' }, { key: 'cohort', label: 'Cohort' },
      { key: 'level', label: 'Level' }, { key: 'rate', label: 'Adoption' },
      { key: 'observed', label: 'Observed' }, { key: 'population', label: 'Cohort characters' },
      ...(this.measure !== 'style-selected' ? [{ key: 'gap' as const, label: 'Ownership → loadout gap' }] : []),
      { key: 'change', label: 'Change (pp)' },
    ];
  }
  get earlierReports(): TelemetrySnapshot[] {
    return this.reports.filter(x => x.reportDateUtc < this.report.reportDateUtc)
      .slice().sort((a, b) => b.reportDateUtc.localeCompare(a.reportDateUtc));
  }
  get baseline(): TelemetrySnapshot | undefined {
    return this.earlierReports.find(x => x.reportDateUtc === this.comparisonDate);
  }
  ngOnChanges(): void {
    if (this.report.reportDateUtc !== this.preferences.selectedReportDate) {
      this.preferences.selectedReportDate = this.report.reportDateUtc;
      const weekEarlier = new Date(Date.parse(this.preferences.selectedReportDate + 'T00:00:00Z') - 7 * 86400000).toISOString().slice(0, 10);
      this.comparisonDate = this.earlierReports.find(x => x.reportDateUtc === weekEarlier)?.reportDateUtc
        ?? this.earlierReports[0]?.reportDateUtc ?? '';
    } else if (this.comparisonDate && !this.baseline) this.comparisonDate = '';
  }
  measureChanged(): void {
    if (this.measure === 'style-selected' && this.sort.startsWith('gap-')) this.sort = 'rate-desc';
  }
  sortBy(column: Column): void {
    const defaultDirection = ['name', 'level', 'cohort'].includes(column) ? 'asc' : 'desc';
    const direction = this.sort === `${column}-asc` ? 'desc' : this.sort === `${column}-desc` ? 'asc' : defaultDirection;
    this.sort = `${column}-${direction}`;
  }
  ariaSort(column: Column): 'ascending' | 'descending' | 'none' {
    return this.sort === `${column}-asc` ? 'ascending' : this.sort === `${column}-desc` ? 'descending' : 'none';
  }
  arrow(column: Column): string {
    return this.ariaSort(column) === 'ascending' ? ' ↑' : this.ariaSort(column) === 'descending' ? ' ↓' : '';
  }
  exportRows(): void {
    downloadCsv(`liveops-adoption-${this.report.reportDateUtc}.csv`, this.groups.flatMap(group => group.rows).map(({ baseline, ...row }) => ({ ...row, previousObserved: baseline?.observedCharacters, previousCohort: baseline?.cohortCharacters })), {
      reportDay: this.report.reportDateUtc, snapshotAt: this.report.snapshotAtUtc, generatedAt: this.report.generatedAtUtc,
      comparisonDay: this.baseline?.reportDateUtc ?? 'None', preferences: this.preferences,
      filters: { definition: this.definitionFilter, levelBand: this.levelBand, cohortDays: this.cohortDays },
      includesCatalogZeros: this.report.adoptionIncludesZeroObservations === true,
      interpretation: 'Rates use cohort characters; change and gap are percentage points. Selections are not combat usage. Membership may change between dates; blank comparisons mean unavailable.'
    });
  }
  private identity(row: Metric, kind = row.kind): string {
    return JSON.stringify([row.cohortDays, row.levelBand, kind, row.key]);
  }
  private rate(row: Metric | undefined): number | null {
    return row && row.cohortCharacters > 0 ? 100 * row.observedCharacters / row.cohortCharacters : null;
  }
  get rows(): AdoptionRow[] {
    const previous = new Map((this.baseline?.adoption ?? []).map(x => [this.identity(x), x]));
    const current = new Map(this.report.adoption.map(x => [this.identity(x), x]));
    const query = this.definitionFilter.trim().toLowerCase();
    return this.report.adoption.filter(x => x.kind === this.measure
      && (!this.cohortDays || x.cohortDays === +this.cohortDays)
      && (!this.levelBand || x.levelBand === this.levelBand)
      && x.cohortCharacters >= Math.max(0, this.minimumCohort ?? 0)
      && (!query || x.key.toLowerCase().includes(query) || this.name(x.key).toLowerCase().includes(query)))
      .map(row => {
        const baseline = previous.get(this.identity(row));
        const rate = this.rate(row), previousRate = this.rate(baseline);
        const owned = current.get(this.identity(row, 'essence-owned'));
        const saved = current.get(this.identity(row, 'essence-saved'));
        const ownedRate = this.rate(owned), savedRate = this.rate(saved);
        return { ...row, name: this.name(row.key), rate, baseline,
          change: rate !== null && previousRate !== null ? rate - previousRate : null,
          gap: ownedRate !== null && savedRate !== null && owned!.cohortCharacters === saved!.cohortCharacters
            ? ownedRate - savedRate : null };
      }).sort((a, b) => this.compare(a, b));
  }
  get groups(): { key: string; label: string; rows: AdoptionRow[] }[] {
    const rows = this.rows;
    if (!this.grouped) return [{ key: 'all', label: '', rows }];
    const groups = new Map<string, { key: string; label: string; rows: AdoptionRow[] }>();
    for (const row of rows) {
      const key = `${row.cohortDays}:${row.levelBand}`;
      if (!groups.has(key)) groups.set(key, { key, label: `${row.cohortDays}-day cohort · Level ${row.levelBand}`, rows: [] });
      groups.get(key)!.rows.push(row);
    }
    return [...groups.values()].sort((a, b) =>
      (a.rows[0].cohortDays - b.rows[0].cohortDays) * (this.sort === 'cohort-desc' ? -1 : 1)
      || compareLevelBands(a.rows[0].levelBand, b.rows[0].levelBand) * (this.sort === 'level-desc' ? -1 : 1));
  }
  private compare(a: AdoptionRow, b: AdoptionRow): number {
    const [column, direction] = this.sort.split('-') as [Column, string];
    let result: number;
    if (column === 'name') result = a.name.localeCompare(b.name);
    else if (column === 'level') result = compareLevelBands(a.levelBand, b.levelBand);
    else {
      const value = (row: AdoptionRow): number | null => column === 'cohort' ? row.cohortDays
        : column === 'observed' ? row.observedCharacters : column === 'population' ? row.cohortCharacters : row[column];
      const av = value(a), bv = value(b);
      // Missing evidence always follows measured values, in either direction.
      if (av === null || bv === null) return av === bv ? this.tie(a, b) : av === null ? 1 : -1;
      result = av - bv;
    }
    return result * (direction === 'desc' ? -1 : 1) || this.tie(a, b);
  }
  private tie(a: AdoptionRow, b: AdoptionRow): number {
    return a.name.localeCompare(b.name) || a.key.localeCompare(b.key)
      || a.cohortDays - b.cohortDays || compareLevelBands(a.levelBand, b.levelBand);
  }
  private name(key: string): string { return readableName(key.replace(/[-_]/g, ' ')); }
}
