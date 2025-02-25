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

import { CharacterDto } from '../../../shared/models/Dtos/characterDto';
import { NamedStorageKeys } from '../../common/enums/named-storage-keys';
import { ToastService } from '../toast/toast.service';
import { EventBusService } from '../event-bus/event-bus.service';

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
    private apiService: ApiService,
    private toastService: ToastService,
    private eventBusService: EventBusService, // inject event bus
  ) {}

  updateCharacter(updatedCharacter: CharacterDto): void {
    this.currentCharacterSubject.next(updatedCharacter);
  }

  setAuth(): void {
    this.isAuthenticatedSubject.next(true);
    this.getLoggedInCharacter().subscribe({
      error: () => {
        this.isAuthenticatedSubject.next(false); // Set as not authenticated in case of an error
      },
    });
    this.eventBusService.emitFetchCurrentAction();
  }

  purgeAuth(): Observable<void> {
    this.currentCharacterSubject.next(null);
    this.isAuthenticatedSubject.next(false);
    localStorage.clear();
    // Emit an event that'll trigger through all services using this
    this.eventBusService.emitLogout();

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
      tap((newTokens) => {
        this.setAuth();
        this.setToken(newTokens);
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
      tap((newTokens) => {
        this.setToken(newTokens);
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
      .subscribe({
        next: () => {
          this.purgeAuth();
          this.router.navigateByUrl('/');
        },
        error: (error) => {
          this.purgeAuth();
          console.error('logout failed', error);
        },
      });
  }

  getLoggedInCharacter(): Observable<CharacterDto> {
    return this.apiService.get('character').pipe(
      tap((character) => {
        this.updateCharacter(character);
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to register'));
      }),
    );
  }

  // Verify the user has a valid token pair, by requesting details about the user
  // This runs on application startup in app.component.ts and on login
  checkAuth(): Observable<CharacterDto | null> {
    const jsonString = this.getToken();

    if (!jsonString) {
      return this.purgeAuth().pipe(map(() => null));
    }

    let tokens;
    try {
      tokens = JSON.parse(jsonString);
    } catch (error) {
      console.error('Error parsing JSON:', error);
      return this.purgeAuth().pipe(map(() => null));
    }

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
        error: () => {
          // In case of error, attempt to refresh tokens
          this.refreshTokens(refreshToken).subscribe();
        },
      }),
      map((res) => res as CharacterDto),
      catchError(() => {
        // On failure to validate or refresh tokens, update authentication status
        this.isAuthenticatedSubject.next(false);
        return of(null);
      }),
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

  loginAsGuest() {
    this.apiService.post('auth/loginAsGuest').subscribe({
      next: (newTokens) => {
        this.setToken(newTokens);
        this.setAuth();
        this.router.navigateByUrl('/game');
      },
      error: (error) => {
        throw new Error('Failed to login as guest');
      },
    });
  }

  convertGuestToUser(
    username: string,
    email: string,
    password: string,
  ): Observable<any> {
    const userCredentials = {
      Username: username,
      Email: email,
      Password: password,
    };
    return this.apiService
      .post('auth/convertGuestToUser', userCredentials)
      .pipe(
        tap((newTokens) => {
          this.setToken(newTokens);
          // Update local state if necessary
          this.toastService.showToast(
            'Account created successfully!',
            'success',
          );
        }),
        catchError(() => {
          return throwError(() => new Error('Failed to convert guest to user'));
        }),
      );
  }

  private handleAuthSuccess(res: any): Observable<CharacterDto> {
    this.setAuth();
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

  setToken(newTokens: any): void {
    localStorage.setItem(NamedStorageKeys.Session, JSON.stringify(newTokens));
  }

  getToken(): string | null {
    return localStorage.getItem(NamedStorageKeys.Session);
  }
}
