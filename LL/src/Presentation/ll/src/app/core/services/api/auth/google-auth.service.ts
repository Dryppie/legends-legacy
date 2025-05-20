import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { environment } from '../../../../../environments/environment';
import { take } from 'rxjs';
declare const google: any; // GIS is loaded globally

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private scriptLoaded?: Promise<void>;
  private gisReady = false;

  constructor(private auth: AuthService) {}

  /** Call once, e.g. from app.component.ts → ngOnInit() */
  init() {
    if (this.gisReady) return;

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

  prompt() {
    this.init();
    google.accounts.id.prompt((notification: any) => {});
  }

  // ────────────────────────────────────────────────────────────────
  private handleIdToken(idToken: string) {
    this.auth.isAuthenticated$.pipe(take(1)).subscribe((isLoggedIn) => {
      if (isLoggedIn) {
        this.auth.bindGoogle(idToken); // bind to existing user
      } else {
        this.auth.googleLogin(idToken); // fresh sign‑in / sign‑up
      }
    });
  }

  private injectScript(): Promise<void> {
    return new Promise((resolve, reject) => {
      if ((window as any).google?.accounts?.id) {
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
