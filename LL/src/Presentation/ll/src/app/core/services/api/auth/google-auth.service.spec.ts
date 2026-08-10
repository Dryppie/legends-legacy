import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../../environments/environment';
import { AuthService } from './auth.service';
import { GoogleAuthService } from './google-auth.service';

describe('GoogleAuthService', () => {
  const auth = jasmine.createSpyObj<AuthService>('AuthService', [
    'bindGoogle',
    'googleLogin',
    'isAuthenticated',
  ]);
  const identityServices = jasmine.createSpyObj('GoogleIdentityServices', [
    'initialize',
    'renderButton',
  ]);

  beforeEach(() => {
    environment.googleClientId = 'test-client-id.apps.googleusercontent.com';
    identityServices.initialize.calls.reset();
    identityServices.renderButton.calls.reset();
    identityServices.renderButton.and.callFake((parent: HTMLElement) => {
      parent.appendChild(document.createElement('iframe'));
    });

    window.google = {
      accounts: {
        id: identityServices,
      },
    };

    TestBed.configureTestingModule({
      providers: [GoogleAuthService, { provide: AuthService, useValue: auth }],
    });
  });

  afterEach(() => {
    delete window.google;
    document
      .querySelectorAll('script[src="https://accounts.google.com/gsi/client"]')
      .forEach((script) => script.remove());
  });

  it('initializes GIS once with click-scoped account selection enabled', async () => {
    const service = TestBed.inject(GoogleAuthService);

    await Promise.all([service.init(), service.init()]);

    expect(identityServices.initialize).toHaveBeenCalledTimes(1);
    expect(identityServices.initialize).toHaveBeenCalledWith(
      jasmine.objectContaining({
        button_auto_select: true,
        use_fedcm_for_button: true,
      }),
    );
  });

  it('renders the supported Google button without prompting on page load', async () => {
    const service = TestBed.inject(GoogleAuthService);
    const parent = document.createElement('div');
    parent.appendChild(document.createElement('span'));

    await service.renderButton(parent);

    expect(parent.childElementCount).toBe(1);
    expect(identityServices.renderButton).toHaveBeenCalledWith(
      parent,
      jasmine.objectContaining({
        text: 'continue_with',
        size: 'large',
      }),
    );
    expect(identityServices.initialize).toHaveBeenCalledTimes(1);
  });

  it('allows initialization to be retried after the GIS script is blocked', async () => {
    delete window.google;
    const service = TestBed.inject(GoogleAuthService);
    const firstAttempt = service.init();
    const script = document.querySelector<HTMLScriptElement>(
      'script[src="https://accounts.google.com/gsi/client"]',
    );

    expect(script).not.toBeNull();
    script?.dispatchEvent(new Event('error'));
    await expectAsync(firstAttempt).toBeRejectedWithError(
      'Unable to load Google Identity Services.',
    );

    window.google = {
      accounts: {
        id: identityServices,
      },
    };

    await expectAsync(service.init()).toBeResolved();
    expect(identityServices.initialize).toHaveBeenCalledTimes(1);
  });
});
