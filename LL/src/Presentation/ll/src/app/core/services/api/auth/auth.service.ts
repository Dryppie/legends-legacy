import { computed, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  Subscription,
  catchError,
  map,
  of,
  shareReplay,
  switchMap,
  take,
  tap,
  throwError,
  timer,
} from 'rxjs';

import { ApiService } from '../api.service';

import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { ToastService } from '../../client-side/toast/toast.service';
import { UserInfoDto } from '../../../../shared/models/Dtos/userInfoDto';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private refreshSub?: Subscription;

  /* writable signals */
  private readonly _currentCharacter = signal<CharacterDto | null>(null);
  private readonly _isAuthenticated = signal(false);

  /* public read-only selectors */
  readonly currentCharacter = computed(() => this._currentCharacter());
  readonly isAuthenticated = computed(() => this._isAuthenticated());
  public returnUrl = '/';

  readonly identity = computed(() => {
    const ch = this._currentCharacter();
    return ch ? `${ch.id}:${ch.name}` : null;
  });

  constructor(
    private router: Router,
    private api: ApiService,
    private toast: ToastService,
    private event: EventBusService,
  ) {}

  initAuth(): Observable<void> {
    return this.tryRefresh().pipe(
      tap((exp) => {
        if (exp) this.afterSuccessfulAuth(exp);
      }),
      map(() => void 0), // hide expiry to callers
      catchError(() => of(void 0)),
    );
  }

  private markAuthenticated() {
    this._isAuthenticated.set(true);
    this.event.emitFetchCurrentAction();
  }

  private markUnauthenticated() {
    this._currentCharacter.set(null);
    this._isAuthenticated.set(false);
    this.event.emitLogout();
  }

  updateCharacter(updatedCharacter: CharacterDto): void {
    this._currentCharacter.set(updatedCharacter);
  }

  login(email: string, password: string): Observable<void> {
    return this.api
      .post('auth/login', { Email: email, Password: password })
      .pipe(
        tap(({ accessExpiresAt }) => {
          this.toast.showToast('Login successful', '', true);
          this.afterSuccessfulAuth(accessExpiresAt);
        }),
        map(() => void 0),
        catchError((e) => {
          this.toast.showToast('Login failed', e.message, false);
          return throwError(() => e);
        }),
      );
  }

  register(
    username: string,
    email: string,
    password: string,
  ): Observable<void> {
    return this.api
      .post('auth/register', {
        Username: username,
        Email: email,
        Password: password,
      })
      .pipe(
        tap(({ accessExpiresAt }) => {
          this.toast.showToast('Registration success', '', true);
          this.afterSuccessfulAuth(accessExpiresAt);
        }),
        catchError((e) => {
          this.toast.showToast('Registration failed', e.errorMessage, false);
          return throwError(() => e);
        }),
      );
  }

  loginAsGuest(): void {
    this.api
      .post('auth/loginAsGuest')
      .pipe(
        tap(({ accessExpiresAt }) => {
          this.toast.showToast('Guest session started', '', true);
          this.afterSuccessfulAuth(accessExpiresAt);
        }),
        catchError((e) => {
          this.toast.showToast('Guest login error', e.message, false);
          return throwError(() => e);
        }),
      )
      .subscribe();
  }

  googleLogin(idToken: string): void {
    this.api
      .post('auth/google', idToken)
      .pipe(
        tap(({ accessExpiresAt }) => {
          this.toast.showToast('Google sign-in success', '', true);
          this.afterSuccessfulAuth(accessExpiresAt);
        }),
        catchError((e) => {
          this.toast.showToast('Google sign-in error', e.message, false);
          return throwError(() => e);
        }),
      )
      .subscribe();
  }

  // bind Google to existing account (new)
  bindGoogle(idToken: string): void {
    this.api
      .post('auth/bind-google', idToken)
      .pipe(
        tap(() => {
          this.toast.showToast('Google binding success', '', true);
        }),
        catchError((e) => {
          this.toast.showToast('Google binding error', e.message, false);
          return throwError(() => e);
        }),
      )
      .subscribe();
  }

  convertGuestToUser(
    username: string,
    email: string,
    password: string,
  ): Observable<void> {
    return this.api
      .post('auth/convertGuestToUser', {
        Username: username,
        Email: email,
        Password: password,
      })
      .pipe(
        tap(({ accessExpiresAt }) => {
          this.toast.showToast('Account converted', '', true);
          // cookies already updated server‑side; just restart auth flow
          this.afterSuccessfulAuth(accessExpiresAt);
        }),
        catchError((e) => {
          this.toast.showToast('Guest to real account error', e.message, false);
          return throwError(() => e);
        }),
      );
  }

  renameCharacter(newName: string) {
    return this.api.post('auth/rename', newName).pipe(
      tap(async ({ accessExpiresAt }) => {
        this.toast.showToast('Edited name', '', true);
        this.afterSuccessfulAuth(accessExpiresAt);

        // reconnect chat after rename
        // await this.chat.reconnect('global'); // or current channel if tracked
      }),
      catchError((e) => {
        this.toast.showToast('Failed to edit name', e.message, false);
        return throwError(() => e);
      }),
    );
  }

  logout(): void {
    this.api.post('auth/logout').subscribe({
      next: () => this.finishLogout(),
      error: () => this.finishLogout(),
    });
  }

  private finishLogout(): void {
    this.refreshSub?.unsubscribe(); // stop future refresh attempts
    this.markUnauthenticated();
    this.router.navigateByUrl('/');
  }

  checkAuth(): Observable<CharacterDto | null> {
    return this.fetchCharacter().pipe(
      catchError(() => {
        return this.tryRefresh().pipe(
          switchMap((ok) => (ok ? this.fetchCharacter() : of(null))),
        );
      }),
      tap((ch) => {
        if (ch) this.markAuthenticated();
      }),
    );
  }

  private fetchCharacter(): Observable<CharacterDto> {
    return this.api
      .get('character')
      .pipe(tap((character) => this._currentCharacter.set(character)));
  }

  /** Returns `true` when refresh succeeded. */
  private tryRefresh(): Observable<number> {
    return this.api.post('auth/createNewTokens').pipe(
      map(({ accessExpiresAt }) => accessExpiresAt),
      catchError(() => of(0)),
      shareReplay(1), // so concurrent subscribers share the same call
    );
  }

  private armRefreshScheduler(accessExpiresAt: number): void {
    this.refreshSub?.unsubscribe();

    const delayMs = Math.max(
      (accessExpiresAt * 1000 - Date.now()) * 0.7,
      1_000,
    );
    this.refreshSub = timer(delayMs)
      .pipe(switchMap(() => this.tryRefresh()))
      .subscribe({
        next: (newExp) => {
          if (newExp) {
            this.armRefreshScheduler(newExp); // re-arm on success
          } else {
            this.logout(); // refresh failed
          }
        },
        error: () => this.logout(),
      });
  }

  private afterSuccessfulAuth(accessExpiresAt: number) {
    this.markAuthenticated();
    this.armRefreshScheduler(accessExpiresAt);

    // preload character, then redirect
    this.fetchCharacter().subscribe({
      next: () => this.router.navigateByUrl('/game'),
      error: () => this.router.navigateByUrl('/game'),
    });
  }

  getUserInfo(): Observable<UserInfoDto> {
    return this.api.get('auth/getUserInfo').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to register'));
      }),
    );
  }
}
