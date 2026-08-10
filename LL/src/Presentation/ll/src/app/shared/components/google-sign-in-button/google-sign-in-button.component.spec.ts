import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GoogleAuthService } from '../../../core/services/api/auth/google-auth.service';
import { GoogleSignInButtonComponent } from './google-sign-in-button.component';

describe('GoogleSignInButtonComponent', () => {
  let fixture: ComponentFixture<GoogleSignInButtonComponent>;
  const googleAuth = jasmine.createSpyObj<GoogleAuthService>(
    'GoogleAuthService',
    ['renderButton'],
  );

  beforeEach(async () => {
    spyOn(console, 'error');
    googleAuth.renderButton.calls.reset();
    googleAuth.renderButton.and.resolveTo();

    await TestBed.configureTestingModule({
      imports: [GoogleSignInButtonComponent],
      providers: [{ provide: GoogleAuthService, useValue: googleAuth }],
    }).compileComponents();

    fixture = TestBed.createComponent(GoogleSignInButtonComponent);
  });

  it('loads the Google button only when the component is displayed', async () => {
    expect(googleAuth.renderButton).not.toHaveBeenCalled();

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(googleAuth.renderButton).toHaveBeenCalledTimes(1);
    expect(fixture.componentInstance.isLoading).toBeFalse();
    expect(fixture.componentInstance.loadFailed).toBeFalse();
  });

  it('shows a recoverable error and retries after browser blocking', async () => {
    googleAuth.renderButton.and.rejectWith(new Error('blocked'));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeTruthy();

    googleAuth.renderButton.and.resolveTo();
    const retryButton = fixture.nativeElement.querySelector(
      'button',
    ) as HTMLButtonElement;
    retryButton.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(googleAuth.renderButton).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });
});
