import { ComponentFixture, TestBed } from '@angular/core/testing';
import { OverlayRef } from '@angular/cdk/overlay';
import { Subject } from 'rxjs';
import { HelpDrawerComponent } from './help-drawer.component';
import { HelpService } from './help.service';
import { HELP_PAGE_ID } from './help.tokens';
import { Guide } from './help.models';

describe('HelpDrawerComponent', () => {
  let fixture: ComponentFixture<HelpDrawerComponent>;
  let request: Subject<Guide>;
  let overlay: { dispose: jasmine.Spy };

  beforeEach(() => {
    request = new Subject<Guide>();
    overlay = { dispose: jasmine.createSpy('dispose') };
    TestBed.configureTestingModule({
      imports: [HelpDrawerComponent],
      providers: [
        { provide: HELP_PAGE_ID, useValue: 'inventory' },
        { provide: HelpService, useValue: { loadGuide: () => request } },
        { provide: OverlayRef, useValue: overlay },
      ],
    });
    fixture = TestBed.createComponent(HelpDrawerComponent);
    fixture.detectChanges();
  });

  it('shows loading then the resolved guide', () => {
    expect(fixture.nativeElement.textContent).toContain('Loading guide');
    request.next({
      title: 'Inventory Guide',
      lastReviewed: '2026-09-03',
      sections: [
        { heading: 'Rank and Style', body: 'Review the Forge preview.' },
      ],
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Rank and Style');
    expect(fixture.nativeElement.textContent).not.toContain('Loading guide');
  });

  it('shows a recovery message when cohort or guide loading fails', () => {
    request.error(new Error('offline'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Guide unavailable');
    expect(fixture.nativeElement.textContent).toContain(
      'close the guide and try again',
    );
    fixture.nativeElement.querySelector('[aria-label="Close guide"]').click();
    expect(overlay.dispose).toHaveBeenCalled();
  });

  it('releases the cohort subscription when the drawer is destroyed', () => {
    expect(request.observed).toBeTrue();
    fixture.destroy();
    expect(request.observed).toBeFalse();
  });
});
