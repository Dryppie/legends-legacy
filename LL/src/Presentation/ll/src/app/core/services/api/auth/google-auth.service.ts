import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { environment } from '../../../../../environments/environment';
import { take } from 'rxjs';
declare const google: any; // GIS is loaded globally

@Injectable({ providedIn: 'root' })
@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private scriptLoaded?: Promise<void>;
  private gisReady = false;

  constructor(private readonly auth: AuthService) {}

  /** Call once, e.g. from app.component.ts → ngOnInit() */
  init(): void {
    if (this.gisReady) return; // already initialised

    (this.scriptLoaded ??= this.injectScript()).then(() => {
      google.accounts.id.initialize({
        client_id: environment.googleClientId,
        callback: ({ credential }: { credential: string }) =>
          this.handleIdToken(credential),
        use_fedcm_for_prompt: true,
        use_fedcm_for_button: true,
        auto_select: true,
      });
      this.gisReady = true;
    });
  }

  prompt(): void {
    this.init(); // guard: ensure GIS loaded
    google.accounts.id.prompt(() => {}); // noop callback
  }

  // ─────────────────────────────────────────────────────────
  private handleIdToken(idToken: string): void {
    /*  ✅ Synchronous, signal-driven read – no take(1), no subscribe */
    const loggedIn = this.auth.isAuthenticated(); // signal<boolean>
    if (loggedIn) {
      this.auth.bindGoogle(idToken); // bind to existing user
    } else {
      this.auth.googleLogin(idToken); // fresh sign-in / sign-up
    }
  }

  private injectScript(): Promise<void> {
    return new Promise((resolve, reject) => {
      if ((window as any).google?.accounts?.id) {
        // already present
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = script.defer = true;
      script.onload = () => resolve();
      script.onerror = (err) => reject(err);
      document.head.appendChild(script);
    });
  }
}
