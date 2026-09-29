import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { LiveOpsApiService } from '../../liveops-api.service';
import { TelemetrySnapshot } from '../../liveops.models';
import { AdoptionTableComponent } from './adoption-table.component';
import { AnalyticsState } from './analytics-state.service';

const ok = <T>(data: T) => ({ isSuccess: true, data, errorMessage: '' });
const report = (date = '2026-09-27'): TelemetrySnapshot => ({
  reportDateUtc: date, generatedAtUtc: date + 'T02:00:00Z', snapshotAtUtc: date + 'T02:00:00Z',
  population: { dau: 3, wau: 3, mau: 3, newActive: 1, returningActive: 2, d1Cohort: 2, d1Returned: 1, d7Cohort: 1, d7Returned: 1 },
  outcomes: [{ kind: 'dungeon', key: 'test-dungeon', started: 1, completed: 1, failed: 0, uniqueCharacters: 1 }],
  adoption: [{ kind: 'essence-owned', key: 'rat', cohortDays: 7, levelBand: '50+', observedCharacters: 1, cohortCharacters: 3 }],
  economy: [{ cohortDays: 7, levelBand: '50+', resource: 'cinders', characterCount: 3, zeroCount: 0, p50Balance: 100, p90Balance: 300 }],
});
function api() {
  return {
    analyticsOverview: jasmine.createSpy().and.resolveTo(ok([report(), report('2026-09-20')])),
    itemizationReports: jasmine.createSpy().and.resolveTo(ok([{ day: '2026-09-25', interpretation: 'Equipment evidence', cohorts: [], choices: [], sevenDayOpportunities: [] }])),
  };
}
async function open(url: string, service = api()) {
  TestBed.configureTestingModule({ providers: [provideRouter(routes), { provide: LiveOpsApiService, useValue: service }] });
  const harness = await RouterTestingHarness.create(url);
  await harness.fixture.whenStable(); harness.detectChanges();
  return { harness, service, state: harness.routeDebugElement!.injector.get(AnalyticsState) };
}
async function navigate(harness: RouterTestingHarness, path: string) {
  await harness.navigateByUrl(path); await harness.fixture.whenStable(); harness.detectChanges();
}

describe('Analytics subpage navigation', () => {
  it('redirects the old link and renders one report page at a time with direct active links', async () => {
    const { harness, service } = await open('/analytics');
    expect(TestBed.inject(Router).url).toBe('/analytics/activity');
    const sections = [
      ['activity', 'Daily active account trend'], ['content', 'Major content outcomes'],
      ['adoption', 'Essence and Combat Style adoption'], ['economy', 'Economy health'], ['itemization', 'Itemization evidence'],
    ];
    for (const [page, heading] of sections) {
      await navigate(harness, '/analytics/' + page);
      const element = harness.routeNativeElement!;
      expect(element.textContent).toContain(heading);
      for (const [other, otherHeading] of sections) if (page !== other) expect(element.textContent).not.toContain(otherHeading);
      expect(element.querySelector('nav[aria-label="Analytics pages"] a[aria-current="page"]')?.getAttribute('href')).toBe('/analytics/' + page);
    }
    expect(service.analyticsOverview).toHaveBeenCalledOnceWith(60);
    expect(service.itemizationReports).toHaveBeenCalledOnceWith(30);
    await navigate(harness, '/analytics/not-a-page');
    expect(TestBed.inject(Router).url).toBe('/analytics/activity');
  });

  it('keeps the report day, cohort filters and adoption settings when returning to a page', async () => {
    const { harness, state } = await open('/analytics/adoption');
    const adoption = harness.routeDebugElement!.query(By.directive(AdoptionTableComponent)).componentInstance as AdoptionTableComponent;
    adoption.sort = 'rate-asc'; adoption.minimumCohort = 2; adoption.grouped = false; adoption.comparisonDate = '';
    state.cohortDays = '30'; state.levelBand = '50+'; state.adoptionDefinition = 'rat';
    await navigate(harness, '/analytics/economy');
    expect(harness.routeDebugElement!.injector.get(AnalyticsState)).toBe(state);
    expect(harness.routeNativeElement!.querySelectorAll('app-column-help').length).toBe(9);
    await navigate(harness, '/analytics/content'); state.definition = 'different content';
    await navigate(harness, '/analytics/adoption');
    const restored = harness.routeDebugElement!.query(By.directive(AdoptionTableComponent)).componentInstance as AdoptionTableComponent;
    expect(restored).not.toBe(adoption);
    expect(restored.sort).toBe('rate-asc'); expect(restored.minimumCohort).toBe(2);
    expect(restored.grouped).toBeFalse(); expect(restored.comparisonDate).toBe('');
    expect(state.selectedDate).toBe('2026-09-27'); expect(state.levelBand).toBe('50+'); expect(state.adoptionDefinition).toBe('rat');
  });

  it('opens Itemization directly without fetching daily reports and isolates errors and report dates', async () => {
    const service = api(); service.analyticsOverview.and.rejectWith(new Error('unavailable'));
    const { harness, state } = await open('/analytics/itemization', service);
    expect(service.analyticsOverview).not.toHaveBeenCalled();
    expect(state.itemizationDate).toBe('2026-09-25');
    expect(harness.routeNativeElement!.textContent).toContain('Equipment evidence');
    await navigate(harness, '/analytics/economy');
    expect(harness.routeNativeElement!.textContent).toContain('Daily reports could not be refreshed');
    await navigate(harness, '/analytics/itemization');
    expect(harness.routeNativeElement!.querySelector('[role="alert"]')).toBeNull();
    expect(state.itemizationDate).toBe('2026-09-25');
    state.days = 7;
    await state.loadItemization();
    expect(service.itemizationReports).toHaveBeenCalledWith(7);
    await navigate(harness, '/analytics/content');
    expect(service.analyticsOverview).toHaveBeenCalledWith(14);
  });
});

describe('Analytics report loading', () => {
  it('ignores an older range response and retains data when a later refresh fails', async () => {
    let resolveOld!: (value: ReturnType<typeof ok<TelemetrySnapshot[]>>) => void;
    const old = new Promise<ReturnType<typeof ok<TelemetrySnapshot[]>>>(resolve => resolveOld = resolve);
    const service = api(); service.analyticsOverview.and.returnValue(old);
    const state = new AnalyticsState(service as never);
    const loadingOld = state.ensureOverview();
    state.days = 7; service.analyticsOverview.and.resolveTo(ok([report()]));
    await state.ensureOverview();
    resolveOld(ok([report('2026-09-20')])); await loadingOld;
    expect(state.selectedDate).toBe('2026-09-27'); expect(state.loading).toBeFalse();
    service.analyticsOverview.and.rejectWith(new Error('failed refresh'));
    await state.loadOverview();
    expect(state.reports[0].reportDateUtc).toBe('2026-09-27'); expect(state.error).toContain('Previously loaded reports remain visible');
    state.ngOnDestroy();
  });
});
