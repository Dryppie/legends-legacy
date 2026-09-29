import { TestBed } from '@angular/core/testing';
import { AdoptionTableComponent } from './adoption-table.component';
import { compareLevelBands } from './adoption-preferences';
import { TelemetrySnapshot } from '../../liveops.models';

type Metric = TelemetrySnapshot['adoption'][number];
const metric = (key: string, observed: number, count = 13, extra: Partial<Metric> = {}): Metric => ({
  key, kind: 'essence-owned', cohortDays: 7, levelBand: '50+', observedCharacters: observed, cohortCharacters: count, ...extra,
});
const snapshot = (date: string, adoption: Metric[], complete = true): TelemetrySnapshot => ({
  reportDateUtc: date, generatedAtUtc: date + 'T02:00:00Z', snapshotAtUtc: date + 'T02:00:00Z',
  population: { dau: 0, wau: 0, mau: 0, newActive: 0, returningActive: 0, d1Cohort: 0, d1Returned: 0, d7Cohort: 0, d7Returned: 0 },
  adoption, adoptionIncludesZeroObservations: complete, outcomes: [], economy: [],
});
function table(rows: Metric[], previous: Metric[] = []) {
  const result = new AdoptionTableComponent();
  result.report = snapshot('2026-09-28', rows);
  result.reports = [result.report, snapshot('2026-09-21', previous)];
  result.ngOnChanges(); return result;
}

describe('Adoption sorting and evidence', () => {
  it('ranks unrounded percentages rather than counts and preserves measured zeros', () => {
    const component = table([metric('many', 8), metric('small', 2, 3), metric('unused', 0)]);
    expect(component.rows.map(x => x.key)).toEqual(['small', 'many', 'unused']);
    component.sort = 'observed-desc'; expect(component.rows[0].key).toBe('many');
    component.sort = 'rate-asc'; expect(component.rows[0].rate).toBe(0);
    component.report.adoption = [metric('z', 1000, 10001), metric('a', 1, 10)];
    expect(component.rows.map(x => x.key)).toEqual(['z', 'a']);
  });
  it('uses stable alphabetical ties in both directions without changing the report', () => {
    const original = [metric('z', 1), metric('a', 1)]; const component = table(original);
    expect(component.rows.map(x => x.key)).toEqual(['a', 'z']);
    component.sort = 'rate-asc'; expect(component.rows.map(x => x.key)).toEqual(['a', 'z']);
    expect(original.map(x => x.key)).toEqual(['z', 'a']);
  });
  it('orders level bands numerically and keeps cohorts separate by default', () => {
    const component = table([metric('high', 12), metric('low', 1, 3, { levelBand: '1-9' }), metric('middle', 2, 3, { levelBand: '20-49' }), metric('month', 3, 3, { cohortDays: 30 })]);
    component.cohortDays = '';
    expect(component.groups.map(x => x.key)).toEqual(['7:1-9', '7:20-49', '7:50+', '30:50+']);
    component.sort = 'level-desc'; expect(component.groups[0].key).toBe('7:50+');
    component.grouped = false; component.sort = 'rate-desc'; expect(component.groups[0].rows[0].key).toBe('month');
    expect(['100+', '20-49', '1-9', '10-19'].sort(compareLevelBands)).toEqual(['1-9', '10-19', '20-49', '100+']);
  });
  it('applies measure, cohort, level, name and minimum population filters together', () => {
    const component = table([metric('essence-rat', 1, 3), metric('essence-cat', 2, 13), metric('essence-cat', 3, 13, { cohortDays: 30 }), metric('essence-cat', 1, 13, { kind: 'essence-saved' })]);
    component.minimumCohort = 10; component.definitionFilter = 'cat'; component.levelBand = '50+';
    expect(component.rows.length).toBe(1); expect(component.rows[0].observedCharacters).toBe(2);
    component.measure = 'essence-saved'; expect(component.rows[0].observedCharacters).toBe(1);
    component.levelBand = '20-49'; expect(component.rows).toEqual([]);
  });
  it('calculates gaps only from matching owned and saved evidence with identical denominators', () => {
    const component = table([metric('a', 8, 10), metric('a', 3, 10, { kind: 'essence-saved' }), metric('b', 5, 10), metric('b', 2, 9, { kind: 'essence-saved' }), metric('c', 0, 10)]);
    component.sort = 'gap-desc'; expect(component.rows.map(x => x.gap)).toEqual([50, null, null]);
    component.sort = 'gap-asc'; expect(component.rows[0].key).toBe('a');
    component.measure = 'style-selected'; component.measureChanged(); expect(component.sort).toBe('rate-desc');
  });
  it('compares rates across changing denominators only for the exact definition, measure, cohort and band', () => {
    const component = table([metric('a', 8, 10), metric('b', 1, 10), metric('c', 1, 10)], [metric('a', 2, 5), metric('b', 4, 5), metric('c', 1, 10, { cohortDays: 30 })]);
    component.sort = 'change-desc'; expect(component.rows.map(x => x.change)).toEqual([40, -70, null]);
    component.sort = 'change-asc'; expect(component.rows.map(x => x.key)).toEqual(['b', 'a', 'c']);
    expect(component.rows[0].baseline?.cohortCharacters).toBe(5);
    component.comparisonDate = ''; expect(component.rows.every(x => x.change === null)).toBeTrue();
  });
  it('never treats missing catalog definitions or zero denominators as historical zero adoption', () => {
    const component = table([metric('new', 5, 10), metric('empty', 0, 0), metric('real-zero', 0, 10)], [metric('empty', 0, 0), metric('real-zero', 5, 10)]);
    component.sort = 'change-asc'; expect(component.rows.map(x => x.change)).toEqual([-50, null, null]);
    component.sort = 'rate-asc'; expect(component.rows.at(-1)?.key).toBe('empty');
  });
  it('defaults comparison to a week earlier, then the nearest earlier report, never a later report', () => {
    const component = table([]); expect(component.comparisonDate).toBe('2026-09-21');
    component.report = snapshot('2026-09-27', []); component.ngOnChanges(); expect(component.comparisonDate).toBe('2026-09-21');
    component.report = component.reports[1]; component.ngOnChanges(); expect(component.comparisonDate).toBe('');
  });
  it('renders legacy coverage, accessible header toggles, small samples and the filtered empty state', async () => {
    await TestBed.configureTestingModule({ imports: [AdoptionTableComponent] }).compileComponents();
    const fixture = TestBed.createComponent(AdoptionTableComponent);
    fixture.componentRef.setInput('report', snapshot('2026-09-28', [metric('rat', 1, 3)], false));
    fixture.detectChanges(); await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('Completely unused definitions are absent');
    expect(root.textContent).toContain('33.3%'); expect(root.textContent).toContain('Small sample');
    const header = Array.from(root.querySelectorAll('thead th')).find(x => x.textContent?.includes('Adoption'))!;
    expect(header.getAttribute('aria-sort')).toBe('descending');
    const help = header.querySelector('app-column-help button') as HTMLButtonElement;
    help.scrollIntoView({ block: 'center', inline: 'center' });
    help.click(); fixture.detectChanges();
    expect(header.getAttribute('aria-sort')).toBe('descending');
    const tooltip = root.querySelector('#' + help.getAttribute('aria-describedby'))!;
    expect(tooltip.getAttribute('role')).toBe('tooltip');
    expect(tooltip.matches(':popover-open')).toBeTrue();
    expect(tooltip.textContent).toContain('Observed characters divided by cohort characters');
    help.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    expect(tooltip.matches(':popover-open')).toBeFalse();
    (header.querySelector('button') as HTMLButtonElement).click(); fixture.detectChanges();
    expect(header.getAttribute('aria-sort')).toBe('ascending');
    fixture.componentInstance.minimumCohort = 10; fixture.detectChanges();
    expect(root.textContent).toContain('No adoption rows match these filters');
    fixture.destroy();
  });
});
