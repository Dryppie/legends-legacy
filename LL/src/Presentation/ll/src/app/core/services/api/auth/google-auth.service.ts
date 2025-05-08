import { Injectable } from '@angular/core';
import { AuthService } from './auth.service';
import { environment } from '../../../../../environments/environment';
import { take } from 'rxjs';
declare const google: any; // GIS is loaded globally

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private gisReady = false;

  constructor(private auth: AuthService) {}

  /** Call once, e.g. from app.component.ts → ngOnInit() */
  init() {
    if (this.gisReady) {
      return;
    } // already done

    google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: ({ credential }: { credential: string }) =>
        this.handleIdToken(credential),

      // --- FedCM switches ---
      use_fedcm_for_prompt: true, // One‑Tap / auto‑sign‑in
      use_fedcm_for_button: true, // Button flow (Chrome 125+ / Android 128+)
      auto_select: true,
    });

    this.gisReady = true;
  }

  prompt() {
    this.init();
    google.accounts.id.prompt((notification: any) => {});
  }

  // ────────────────────────────────────────────────────────────────
  private handleIdToken(idToken: string) {
    this.auth.isAuthenticated$.pipe(take(1)).subscribe((isLoggedIn) => {
      if (isLoggedIn) {
        this.auth.bindGoogle(idToken).subscribe(); // bind to existing user
      } else {
        this.auth.googleLogin(idToken); // fresh sign‑in / sign‑up
      }
    });
  }
}
