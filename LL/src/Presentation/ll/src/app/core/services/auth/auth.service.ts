import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  catchError,
  distinctUntilChanged,
  from,
  map,
  of,
  switchMap,
  take,
  tap,
  throwError,
} from 'rxjs';

import { ApiService } from '../api/api.service';

import { CharacterDto } from '../../../shared/models/characterDto';
import { NamedStorageKeys } from '../../common/enums/named-storage-keys';
import { ToastService } from '../toast/toast.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private currentCharacterSubject = new BehaviorSubject<CharacterDto | null>(
    null,
  );

  public currentCharacter$ = this.currentCharacterSubject
    .asObservable()
    .pipe(distinctUntilChanged());

  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false); // Start with false
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  public returnUrl = '/';

  constructor(
    private router: Router,
    private apiService: ApiService,
    private toastService: ToastService,
  ) {}

  setAuth(currentCharacter: CharacterDto): void {
    this.currentCharacterSubject.next(currentCharacter);
    this.isAuthenticatedSubject.next(true);
  }

  purgeAuth(): Observable<void> {
    this.currentCharacterSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    localStorage.removeItem(NamedStorageKeys.Session);

    if (!location.href.includes('login')) {
      return from(this.router.navigateByUrl('/')).pipe(
        map(() => {
          this.returnUrl = '/';
          return undefined;
        }),
      );
    }

    return of(undefined);
  }

  login(email: string, password: string): Observable<any> {
    const userCredentials = {
      Email: email,
      Password: password,
    };
    return this.apiService.post('auth/login', userCredentials).pipe(
      tap((user) => {
        this.setAuth(user);
        this.setToken(user);
        this.toastService.showToast(
          'Action completed successfully!',
          'success',
        );

        this.router.navigateByUrl(`/game`);
      }),

      catchError(() => {
        this.toastService.showToast(
          'Login Failed',
          'Wrong email or password',
          'error',
          't',
        );
        return throwError(() => new Error('Failed to login'));
      }),
    );
  }

  register(username: string, email: string, password: string): Observable<any> {
    const userCredentials = {
      Username: username,
      Email: email,
      Password: password,
    };
    return this.apiService.post('auth/register', userCredentials).pipe(
      tap((user) => {
        this.setToken(user);
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to register'));
      }),
    );
  }

  logout() {
    this.apiService
      .post('auth/logout')
      .pipe(take(1))
      .subscribe(() => {
        this.purgeAuth();
        this.router.navigateByUrl(`/`);
      });
  }

  getCurrentCharacter(): CharacterDto | null {
    return this.currentCharacterSubject.value;
  }

  // Verify the user has a valid token pair, by requesting details about the user
  // This runs on application startup in app.component.ts and on login
  checkAuth(): Observable<CharacterDto | null> {
    const jsonString = this.getToken();

    if (!jsonString) {
      return this.purgeAuth().pipe(map(() => null));
    }

    const tokens = JSON.parse(jsonString);
    const accessToken = tokens.find(
      (token: { key: string; value: string }) => token.key === 'AccessToken',
    )?.value;
    const refreshToken = tokens.find(
      (token: { key: string; value: string }) => token.key === 'RefreshToken',
    )?.value;

    if (!accessToken || !refreshToken) {
      return this.purgeAuth().pipe(map(() => null));
    }

    return this.apiService.post('auth/validateToken', accessToken).pipe(
      tap({
        next: (res) => this.handleAuthSuccess(res),
        error: (err) => console.error('Error validating token', err),
      }),
      map((res) => res as CharacterDto),
      catchError(() =>
        this.refreshTokens(refreshToken).pipe(
          map((res) => res || null), // Ensure null is returned if refreshTokens fails
        ),
      ),
    );
  }

  private refreshTokens(refreshToken: string): Observable<CharacterDto> {
    return this.apiService.post('auth/createNewTokens', refreshToken).pipe(
      switchMap((newTokens) => {
        this.setToken(newTokens);
        return this.handleAuthSuccess(newTokens.accessToken);
      }),
      catchError(() => this.handleAuthFailure()),
    );
  }

  private handleAuthSuccess(res: any): Observable<CharacterDto> {
    this.setAuth(res as CharacterDto);
    return of(res as CharacterDto); // Ensure the return type is Observable<CharacterDto>
  }

  private handleAuthFailure(): Observable<CharacterDto> {
    return from(this.purgeAuth()).pipe(
      // Use `mapTo` or `tap` with `map` to handle the type correctly
      map(() => {
        // Handle any necessary logic here, if needed
        // Return a default instance or an empty CharacterDto as appropriate
        return {} as CharacterDto; // Return an empty CharacterDto instance
      }),
    );
  }

  setToken(user: any): void {
    localStorage.setItem(NamedStorageKeys.Session, JSON.stringify(user));
  }

  getToken(): string | null {
    return localStorage.getItem(NamedStorageKeys.Session);
  }
}
