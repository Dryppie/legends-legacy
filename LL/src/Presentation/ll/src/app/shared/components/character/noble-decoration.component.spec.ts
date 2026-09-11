import {
  ComponentFixture,
  fakeAsync,
  TestBed,
  tick,
} from '@angular/core/testing';
import { Subject } from 'rxjs';
import {
  NobilityAppearance,
  NobilityService,
} from '../../../core/services/api/nobility/nobility.service';
import { TimeSyncService } from '../../../core/services/api/time-sync/time-sync.service';
import { NobleDecorationComponent } from './noble-decoration.component';

describe('NobleDecorationComponent display preference', () => {
  let fixture: ComponentFixture<NobleDecorationComponent>;
  let appearance: Subject<NobilityAppearance>;

  beforeEach(() => {
    appearance = new Subject<NobilityAppearance>();
    TestBed.configureTestingModule({
      imports: [NobleDecorationComponent],
      providers: [
        {
          provide: NobilityService,
          useValue: { publicAppearance: () => appearance },
        },
        { provide: TimeSyncService, useValue: { now: () => Date.now() } },
      ],
    });
    fixture = TestBed.createComponent(NobleDecorationComponent);
    fixture.componentRef.setInput('characterId', 'character-1');
  });

  afterEach(() => fixture.destroy());

  it('hides both the badge and overview display metadata when Display Nobility is off', fakeAsync(() => {
    fixture.detectChanges();
    tick(0);
    const value = {
      serverTime: new Date().toISOString(),
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      showBadge: true,
    };
    appearance.next(value);
    fixture.detectChanges();
    expect(
      fixture.nativeElement
        .querySelector('[aria-label="Noble"]')
        ?.textContent.trim(),
    ).toBe('◆');
    expect(fixture.componentInstance.visible()).toEqual(value);

    appearance.next({ ...value, showBadge: false });
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('[aria-label="Noble"]'),
    ).toBeNull();
    expect(fixture.componentInstance.visible()).toBeNull();
    expect(fixture.componentInstance.active()).not.toBeNull();

    appearance.next(value);
    fixture.detectChanges();
    expect(fixture.componentInstance.visible()).toEqual(value);
  }));

  it('hides enabled Nobility display when membership expires', fakeAsync(() => {
    fixture.detectChanges();
    tick(0);
    appearance.next({
      serverTime: new Date().toISOString(),
      expiresAt: new Date(Date.now() + 1000).toISOString(),
      showBadge: true,
    });
    fixture.detectChanges();
    expect(fixture.componentInstance.visible()).not.toBeNull();
    tick(1001);
    fixture.detectChanges();
    expect(fixture.componentInstance.visible()).toBeNull();
    expect(
      fixture.nativeElement.querySelector('[aria-label="Noble"]'),
    ).toBeNull();
  }));
});
