import { TestBed } from '@angular/core/testing';
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
    identityServices.initialize.calls.reset();
    identityServices.renderButton.calls.reset();

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

    expect(parent.childElementCount).toBe(0);
    expect(identityServices.renderButton).toHaveBeenCalledWith(
      parent,
      jasmine.objectContaining({
        text: 'continue_with',
        size: 'large',
      }),
    );
    expect(identityServices.initialize).toHaveBeenCalledTimes(1);
  });
});
