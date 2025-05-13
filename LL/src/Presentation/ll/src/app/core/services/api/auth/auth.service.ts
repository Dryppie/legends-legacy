import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  catchError,
  map,
  of,
  switchMap,
  take,
  tap,
  throwError,
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
  private currentCharacterSubject = new BehaviorSubject<CharacterDto | null>(
    null,
  );

  public currentCharacter$ = this.currentCharacterSubject.asObservable();

  private isAuthenticatedSubject = new BehaviorSubject<boolean | null>(false); // Start with false
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  public returnUrl = '/';

  constructor(
    private router: Router,
    private api: ApiService,
    private toast: ToastService,
    private event: EventBusService,
  ) {}

  private markAuthenticated() {
    this.isAuthenticatedSubject.next(true);
    this.event.emitFetchCurrentAction();
  }

  private markUnauthenticated() {
    this.currentCharacterSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    this.event.emitLogout();
  }

  updateCharacter(updatedCharacter: CharacterDto): void {
    this.currentCharacterSubject.next(updatedCharacter);
  }

  login(email: string, password: string): Observable<void> {
    return this.api
      .post('auth/login', { Email: email, Password: password })
      .pipe(
        tap(() => {
          this.toast.showToast('Login successful', '', true);
          this.afterSuccessfulAuth();
        }),
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
        tap(() => {
          this.toast.showToast('Registration success', '', true);
          this.afterSuccessfulAuth();
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
        tap(() => {
          this.toast.showToast('Guest session started', '', true);
          this.afterSuccessfulAuth();
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
        tap(() => {
          this.toast.showToast('Google sign-in success', '', true);
          this.afterSuccessfulAuth();
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
        tap(() => {
          this.toast.showToast('Account converted', '', true);
          // cookies already updated server‑side; just restart auth flow
          this.afterSuccessfulAuth();
        }),
        catchError((e) => {
          this.toast.showToast('Guest to real account error', e.message, false);
          return throwError(() => e);
        }),
      );
  }

  logout(): void {
    this.api
      .post('auth/logout')
      .pipe(take(1))
      .subscribe({
        next: () => this.finishLogout(),
        error: () => this.finishLogout(),
      });
  }

  private finishLogout() {
    this.markUnauthenticated();
    this.router.navigateByUrl('/');
  }

  checkAuth(): Observable<CharacterDto | null> {
    return this.fetchCharacter().pipe(
      catchError(() => {
        console.log('check auth error)');
        return this.tryRefresh().pipe(
          switchMap((ok) => (ok ? this.fetchCharacter() : of(null))),
        );
      }),
      tap((ch) => {
        console.log('tapped into check auth');
        if (ch) this.markAuthenticated();
      }),
    );
  }

  private fetchCharacter(): Observable<CharacterDto> {
    return this.api.get('character').pipe(
      tap((character) => this.currentCharacterSubject.next(character)),
      map((character) => character as CharacterDto),
    );
  }

  /** Returns `true` when refresh succeeded. */
  private tryRefresh(): Observable<boolean> {
    return this.api.post('auth/createNewTokens').pipe(
      map(() => true), // tokens set in HttpOnly cookie
      catchError(() => of(false)),
    );
  }

  private afterSuccessfulAuth() {
    this.markAuthenticated();
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
