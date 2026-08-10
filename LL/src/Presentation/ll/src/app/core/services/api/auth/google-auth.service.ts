import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { environment } from '../../../../../environments/environment';

const GIS_SCRIPT_URL = 'https://accounts.google.com/gsi/client';

interface GoogleCredentialResponse {
  credential: string;
}

interface GoogleIdentityServices {
  initialize(config: Record<string, unknown>): void;
  renderButton(parent: HTMLElement, options: Record<string, unknown>): void;
}

declare global {
  interface Window {
    google?: {
      accounts: {
        id: GoogleIdentityServices;
      };
    };
  }
}

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private initialization?: Promise<void>;

  constructor(private readonly auth: AuthService) {}

  init(): Promise<void> {
    return (this.initialization ??= this.loadScript().then(() => {
      this.identityServices.initialize({
        client_id: environment.googleClientId,
        callback: ({ credential }: GoogleCredentialResponse) =>
          this.handleIdToken(credential),
        use_fedcm_for_button: true,
        button_auto_select: true,
      });
    }));
  }

  async renderButton(parent: HTMLElement): Promise<void> {
    await this.init();
    parent.replaceChildren();
    this.identityServices.renderButton(parent, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      text: 'continue_with',
      shape: 'rectangular',
      logo_alignment: 'left',
    });
  }

  // ─────────────────────────────────────────────────────────
  private handleIdToken(idToken: string): void {
    /*  ✅ Synchronous, signal-driven read – no take(1), no subscribe */
    const loggedIn = this.auth.isAuthenticated(); // signal<boolean>
    if (loggedIn) {
      this.auth.bindGoogle(idToken).subscribe({ error: () => undefined }); // bind to existing user
    } else {
      this.auth.googleLogin(idToken); // fresh sign-in / sign-up
    }
  }

  private get identityServices(): GoogleIdentityServices {
    const identityServices = window.google?.accounts?.id;
    if (!identityServices) {
      throw new Error('Google Identity Services failed to initialize.');
    }

    return identityServices;
  }

  private loadScript(): Promise<void> {
    if (window.google?.accounts?.id) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const existingScript = document.querySelector<HTMLScriptElement>(
        `script[src="${GIS_SCRIPT_URL}"]`,
      );
      const script = existingScript ?? document.createElement('script');

      const handleLoad = () => resolve();
      const handleError = () =>
        reject(new Error('Unable to load Google Identity Services.'));

      script.addEventListener('load', handleLoad, { once: true });
      script.addEventListener('error', handleError, { once: true });

      if (existingScript) {
        return;
      }

      script.src = GIS_SCRIPT_URL;
      script.async = script.defer = true;
      document.head.appendChild(script);
    });
  }
}
