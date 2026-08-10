import { TestBed } from '@angular/core/testing';
import { AppUpdateService } from './core/services/client-side/app-update/app-update.service';
import { ToastService } from './core/services/client-side/components/toast/toast.service';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  const toastService = jasmine.createSpyObj<ToastService>('ToastService', [
    'register',
  ]);
  const appUpdate = jasmine.createSpyObj<AppUpdateService>('AppUpdateService', [
    'start',
  ]);

  beforeEach(async () => {
    toastService.register.calls.reset();
    appUpdate.start.calls.reset();

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        { provide: ToastService, useValue: toastService },
        { provide: AppUpdateService, useValue: appUpdate },
      ],
    })
      .overrideComponent(AppComponent, {
        set: {
          imports: [],
          template: '<div data-testid="app-shell"></div>',
        },
      })
      .compileComponents();
  });

  it('creates the application shell with its canonical title', () => {
    const fixture = TestBed.createComponent(AppComponent);

    expect(fixture.componentInstance.title).toBe('ll');
    expect(
      fixture.nativeElement.querySelector('[data-testid="app-shell"]'),
    ).toBeTruthy();
  });

  it('starts update checks during initialization', () => {
    const fixture = TestBed.createComponent(AppComponent);

    fixture.componentInstance.ngOnInit();

    expect(appUpdate.start).toHaveBeenCalledTimes(1);
  });
});
