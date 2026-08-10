import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { environment } from '../../../../../environments/environment';

const GIS_SCRIPT_URL = 'https://accounts.google.com/gsi/client';
const GIS_SCRIPT_TIMEOUT_MS = 10_000;
const GIS_BUTTON_TIMEOUT_MS = 3_000;

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
    if (this.initialization) {
      return this.initialization;
    }

    if (!environment.googleClientId) {
      return Promise.reject(new Error('Google Sign-In is not configured.'));
    }

    const attempt = this.loadScript().then(() => {
      this.identityServices.initialize({
        client_id: environment.googleClientId,
        callback: ({ credential }: GoogleCredentialResponse) =>
          this.handleIdToken(credential),
        use_fedcm_for_button: true,
        button_auto_select: true,
      });
    });

    const retryableAttempt = attempt.catch((error: unknown) => {
      if (this.initialization === retryableAttempt) {
        this.initialization = undefined;
      }

      throw error;
    });

    this.initialization = retryableAttempt;
    return retryableAttempt;
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
    await this.waitForRenderedButton(parent);
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

      const cleanup = () => {
        window.clearTimeout(timeoutId);
        script.removeEventListener('load', handleLoad);
        script.removeEventListener('error', handleError);
      };
      const fail = () => {
        cleanup();
        if (!window.google?.accounts?.id) {
          script.remove();
        }
        reject(new Error('Unable to load Google Identity Services.'));
      };
      const handleLoad = () => {
        if (!window.google?.accounts?.id) {
          fail();
          return;
        }

        cleanup();
        resolve();
      };
      const handleError = () => fail();
      const timeoutId = window.setTimeout(fail, GIS_SCRIPT_TIMEOUT_MS);

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

  private waitForRenderedButton(parent: HTMLElement): Promise<void> {
    if (parent.childElementCount > 0) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const observer = new MutationObserver(() => {
        if (parent.childElementCount === 0) return;

        cleanup();
        resolve();
      });
      const timeoutId = window.setTimeout(() => {
        cleanup();
        reject(new Error('Google Sign-In button did not render.'));
      }, GIS_BUTTON_TIMEOUT_MS);
      const cleanup = () => {
        window.clearTimeout(timeoutId);
        observer.disconnect();
      };

      observer.observe(parent, { childList: true });
    });
  }
}
