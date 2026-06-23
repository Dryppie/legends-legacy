import { computed, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  Observable,
  Subscription,
  catchError,
  finalize,
  map,
  of,
  shareReplay,
  switchMap,
  tap,
  throwError,
  timer,
} from 'rxjs';

import { ApiService } from '../api.service';

import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { UserInfoDto } from '../../../../shared/models/Dtos/userInfoDto';
import { ToastService } from '../../client-side/components/toast/toast.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private refreshSub?: Subscription;
  /** UNIX seconds when the current access token expires */
  private _accessExpiresAt = 0;

  /** shared refresh observable while a refresh is in-flight */
  private _refreshInFlight$?: Observable<number>;
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

  checkAuth(): Observable<CharacterDto | null> {
    return this.tryRefresh().pipe(
      switchMap((expiresAt) => (expiresAt ? this.fetchCharacter() : of(null))),
      tap((ch) => {
        if (ch) {
          this.markAuthenticated();
        } else {
          this.markUnauthenticated();
        }
      }),
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
    const current = this._currentCharacter();
    if (current && this.isSameCharacter(current, updatedCharacter)) {
      return;
    }

    this._currentCharacter.set(updatedCharacter);
  }

  refreshCurrentCharacter(): void {
    this.fetchCharacter().subscribe();
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

  private fetchCharacter(): Observable<CharacterDto> {
    return this.api
      .get('character')
      .pipe(
        tap((character) => {
          const current = this._currentCharacter();
          if (!current || !this.isSameCharacter(current, character)) {
            this._currentCharacter.set(character);
          }
        }),
      );
  }

  /** Returns `true` when refresh succeeded. */
  private tryRefresh(): Observable<number> {
    return this.api.post('auth/createNewTokens').pipe(
      map(({ accessExpiresAt }) => {
        this.setAccessExpiry(accessExpiresAt);
        return accessExpiresAt;
      }),
      catchError(() => of(0)),
      shareReplay(1), // so concurrent subscribers share the same call
    );
  }

  private afterSuccessfulAuth(accessExpiresAt: number) {
    this.markAuthenticated();

    this.setAccessExpiry(accessExpiresAt);
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

  ensureValidToken(): Observable<number> {
    if (!this._isAuthenticated() || !this._accessExpiresAt) {
      return throwError(() => new Error('Not authenticated'));
    }

    const now = Date.now();
    const expiryMs = this._accessExpiresAt * 1000;

    const REFRESH_BUFFER_MS = 10_000;
    if (now < expiryMs - REFRESH_BUFFER_MS) {
      return of(this._accessExpiresAt);
    }

    if (!this._refreshInFlight$) {
      this._refreshInFlight$ = this.refreshOrLogout();
    }

    return this._refreshInFlight$;
  }

  refreshSession(): Observable<number> {
    if (!this._refreshInFlight$) {
      this._refreshInFlight$ = this.refreshOrLogout();
    }

    return this._refreshInFlight$;
  }

  private refreshOrLogout(): Observable<number> {
    return this.tryRefresh().pipe(
      tap((exp) => {
        if (!exp) {
          this.logout();
        }
      }),
      switchMap((exp) =>
        exp ? of(exp) : throwError(() => new Error('Unable to refresh session')),
      ),
      finalize(() => (this._refreshInFlight$ = undefined)),
      shareReplay(1),
    );
  }

  private setAccessExpiry(exp: number) {
    this._accessExpiresAt = exp;
  }

  private isSameCharacter(a: CharacterDto, b: CharacterDto): boolean {
    return (
      a.id === b.id &&
      a.name === b.name &&
      a.level === b.level &&
      a.experience === b.experience &&
      a.experienceUntilNextLevel === b.experienceUntilNextLevel &&
      a.cinders === b.cinders &&
      a.soulstones === b.soulstones &&
      a.arenaRating === b.arenaRating
    );
  }
}
