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

interface ApiResponse<T> {
  isSuccess: boolean;
  data?: T;
  errorMessage?: string;
}

interface AuthTokens {
  accessToken: string;
  accessExpiresAt: number;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private refreshSub?: Subscription;
  /** UNIX seconds when the current access token expires */
  private _accessExpiresAt = 0;
  private _accessToken = '';

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
      tap((expiresAt) => {
        if (expiresAt) {
          this.markAuthenticated();
        } else {
          this.markUnauthenticated();
        }
      }),
      map(() => null),
    );
  }

  private markAuthenticated() {
    this._isAuthenticated.set(true);
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
        tap((response) => {
          const tokens = this.unwrapTokens(response);
          this.toast.showToast('Login successful', '', true);
          this.afterSuccessfulAuth(tokens.accessToken, tokens.accessExpiresAt);
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
        tap((response) => {
          const tokens = this.unwrapTokens(response);
          this.toast.showToast('Registration success', '', true);
          this.afterSuccessfulAuth(tokens.accessToken, tokens.accessExpiresAt);
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
        tap((response) => {
          const tokens = this.unwrapTokens(response);
          this.toast.showToast('Guest session started', '', true);
          this.afterSuccessfulAuth(tokens.accessToken, tokens.accessExpiresAt);
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
        tap((response) => {
          const tokens = this.unwrapTokens(response);
          this.toast.showToast('Google sign-in success', '', true);
          this.afterSuccessfulAuth(tokens.accessToken, tokens.accessExpiresAt);
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
        tap((response) => {
          const tokens = this.unwrapTokens(response);
          this.toast.showToast('Account converted', '', true);
          // cookies already updated server‑side; just restart auth flow
          this.afterSuccessfulAuth(tokens.accessToken, tokens.accessExpiresAt);
        }),
        catchError((e) => {
          this.toast.showToast('Guest to real account error', e.message, false);
          return throwError(() => e);
        }),
      );
  }

  renameCharacter(newName: string) {
    return this.api.post('auth/rename', newName).pipe(
      tap((response) => {
        const tokens = this.unwrapTokens(response);
        this.toast.showToast('Edited name', '', true);
        this.afterSuccessfulAuth(tokens.accessToken, tokens.accessExpiresAt);

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
    this._accessToken = '';
    this._accessExpiresAt = 0;
    this.markUnauthenticated();
    this.router.navigateByUrl('/');
  }

  private fetchCharacter(): Observable<CharacterDto> {
    return this.api
      .get('character')
      .pipe(
        map((response) => this.unwrapResponse<CharacterDto>(response)),
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
      map((response) => {
        const tokens = this.unwrapTokens(response);
        this.setAccessToken(tokens.accessToken, tokens.accessExpiresAt);
        return tokens.accessExpiresAt;
      }),
      catchError(() => of(0)),
      shareReplay(1), // so concurrent subscribers share the same call
    );
  }

  private afterSuccessfulAuth(accessToken: string, accessExpiresAt: number) {
    this.setAccessToken(accessToken, accessExpiresAt);
    this.markAuthenticated();
    this.router.navigateByUrl('/game');
  }

  getUserInfo(): Observable<UserInfoDto> {
    return this.api.get('auth/getUserInfo').pipe(
      map((response) => this.unwrapResponse<UserInfoDto>(response)),
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

  getAccessToken(): string {
    return this._accessToken;
  }

  private setAccessToken(token: string, exp: number) {
    this._accessToken = token;
    this._accessExpiresAt = exp;
  }

  private unwrapTokens(response: AuthTokens | ApiResponse<AuthTokens>): AuthTokens {
    const tokens = this.unwrapResponse<AuthTokens>(response);

    if (!tokens?.accessToken || !tokens.accessExpiresAt) {
      throw new Error('Authentication response did not include access token data.');
    }

    return tokens;
  }

  private unwrapResponse<T>(response: T | ApiResponse<T>): T {
    if (this.isApiResponse<T>(response)) {
      if (!response.isSuccess || response.data == null) {
        throw new Error(response.errorMessage || 'Request failed.');
      }

      return response.data;
    }

    return response;
  }

  private isApiResponse<T>(response: T | ApiResponse<T>): response is ApiResponse<T> {
    return (
      response != null &&
      typeof response === 'object' &&
      'isSuccess' in response &&
      'data' in response
    );
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
      a.arenaRating === b.arenaRating &&
      this.isSameEquippedTitle(a.equippedTitle, b.equippedTitle)
    );
  }

  private isSameEquippedTitle(
    a: CharacterDto['equippedTitle'],
    b: CharacterDto['equippedTitle'],
  ): boolean {
    if (!a && !b) return true;
    if (!a || !b) return false;

    return (
      a.key === b.key &&
      a.name === b.name &&
      a.displayPosition === b.displayPosition &&
      a.displayName === b.displayName
    );
  }
}
