import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  catchError,
  from,
  map,
  mergeMap,
  of,
  switchMap,
  take,
  tap,
  throwError,
} from 'rxjs';

import { ApiService } from '../api.service';

import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { NamedStorageKeys } from '../../../common/enums/named-storage-keys';
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
      tap((response) => {
        if (response.isSuccess) {
          this.setAuth();
          this.setToken(response.data);
          this.toastService.showToast(
            'Action completed successfully!',
            'Login succesful',
            response.isSuccess,
          );

          this.router.navigateByUrl(`/game`);
        } else {
          this.toastService.showToast(
            'Login failed!',
            response.errorMessage,
            response.isSuccess,
          );
        }
      }),

      catchError(() => {
        this.toastService.showToast(
          'Login Failed',
          'Wrong email or password',
          false,
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
      mergeMap((response) => {
        if (response.isSuccess) {
          this.setToken(response.data);
          this.toastService.showToast(
            'Registration Success',
            'Your account has been created.',
            response.isSuccess,
          );
          return of(response);
        } else {
          this.toastService.showToast(
            'Registration Failed',
            response.errorMessage,
            response.isSuccess,
          );
          return throwError(() => new Error('Failed to login'));
        }
      }),

      catchError((response) => {
        this.toastService.showToast(
          'Registration Failed',
          'Contact the developer',
          response.isSuccess,
        );
        return throwError(() => new Error('Failed to login'));
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
      tap((response) => {
        this.updateCharacter(response.data);
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

    let tokens: any;
    try {
      tokens = JSON.parse(jsonString);
    } catch (error) {
      console.error('Error parsing JSON:', error);
      return this.purgeAuth().pipe(map(() => null));
    }

    if (!tokens || typeof tokens !== 'object') {
      // Tokens is not a valid object
      return this.purgeAuth().pipe(map(() => null));
    }

    // Convert tokens to an array for easier lookup if tokens is stored as an object
    const tokensArray = Object.entries(tokens).map(([key, value]) => ({
      key,
      value,
    }));

    const accessToken =
      tokensArray.find((token) => token.key === 'accessToken')?.value ?? null;
    const refreshToken =
      tokensArray.find((token) => token.key === 'refreshToken')?.value ?? null;

    if (!accessToken || !refreshToken) {
      // Missing required tokens
      return this.purgeAuth().pipe(map(() => null));
    }

    return this.apiService.post('auth/validateToken', accessToken).pipe(
      tap({
        next: (res) => this.handleAuthSuccess(res),
        error: () => {
          // In case of error, attempt to refresh tokens
          this.refreshTokens(refreshToken as string).subscribe();
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
    this.apiService
      .post('auth/loginAsGuest')
      .pipe(
        mergeMap((response) => {
          if (response.isSuccess) {
            this.setToken(response.data);
            this.setAuth();
            this.router.navigateByUrl('/game');
            this.toastService.showToast(
              'Guest login success',
              'Your guest account has been created.',
              response.isSuccess,
            );
            return of(response);
          } else {
            this.toastService.showToast(
              'Registration Failed',
              response.errorMessage,
              response.isSuccess,
            );
            return throwError(() => new Error('Failed to login'));
          }
        }),
        catchError((response) => {
          this.toastService.showToast(
            'Registration Failed',
            'Contact the developer',
            response.isSuccess,
          );
          return throwError(() => new Error('Failed to login'));
        }),
      )
      .subscribe();
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
        tap((response) => {
          this.setToken(response.newTokens);
          // Update local state if necessary
          this.toastService.showToast(
            'Account created successfully!',
            'Success',
            response.isSuccess,
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

  getUserInfo(): Observable<UserInfoDto> {
    return this.apiService.get('auth/getUserInfo').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to register'));
      }),
    );
  }
}
