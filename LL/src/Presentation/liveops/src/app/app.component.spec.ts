import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { LiveOpsApiService } from './liveops-api.service';

describe('AppComponent', () => {
  it('loads the authenticated operator session', async () => {
    const api = {
      session: jasmine.createSpy().and.resolveTo({
        subject: 'operator-1',
        displayName: 'Test Operator',
        permissions: ['liveops.read'],
        environment: 'Development',
        isDevelopmentOperator: true,
      }),
      initializeAntiforgery: jasmine.createSpy().and.resolveTo(),
    };

    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [{ provide: LiveOpsApiService, useValue: api }],
    }).compileComponents();

    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(api.session).toHaveBeenCalled();
    expect(api.initializeAntiforgery).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Test Operator');
  });
});
