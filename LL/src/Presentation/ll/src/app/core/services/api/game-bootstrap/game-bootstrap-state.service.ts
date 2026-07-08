import { computed, effect, Injectable, signal } from '@angular/core';
import { catchError, finalize, Observable, of, shareReplay, tap, throwError } from 'rxjs';
import { CharacterActionsStateService } from '../character-actions/character-actions.state.service';
import { AuthService } from '../auth/auth.service';
import {
  GameBootstrapDto,
  GameBootstrapService,
} from './game-bootstrap.service';
import { TutorialStateService } from '../tutorial/tutorial-state.service';
import { GameEventService } from '../../real-time/game-event.service';

@Injectable({ providedIn: 'root' })
export class GameBootstrapStateService {
  private readonly _bootstrap = signal<GameBootstrapDto | null>(null);
  private readonly _loading = signal(false);
  private readonly _loaded = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _serverTimeUtc = signal<string | null>(null);
  private inFlight$?: Observable<GameBootstrapDto | null>;

  readonly bootstrap = computed(() => this._bootstrap());
  readonly loading = computed(() => this._loading());
  readonly loaded = computed(() => this._loaded());
  readonly error = computed(() => this._error());
  readonly serverTimeUtc = computed(() => this._serverTimeUtc());

  constructor(
    private readonly bootstrapService: GameBootstrapService,
    private readonly auth: AuthService,
    private readonly tutorialState: TutorialStateService,
    private readonly characterActionsState: CharacterActionsStateService,
    private readonly gameEvents: GameEventService,
  ) {
    effect(
      () => {
        if (!this.auth.isAuthenticated()) {
          this.reset();
          return;
        }

        const reconnectCount = this.gameEvents.reconnectCount();
        if (reconnectCount <= 0 || !this._loaded()) {
          return;
        }

        queueMicrotask(() => {
          this.reload().subscribe({
            error: () => undefined,
          });
        });
      },
      { allowSignalWrites: true },
    );
  }

  load(): Observable<GameBootstrapDto | null> {
    return this.fetch(false);
  }

  reload(): Observable<GameBootstrapDto | null> {
    return this.fetch(true);
  }

  reset(): void {
    this.inFlight$ = undefined;
    this._bootstrap.set(null);
    this._loading.set(false);
    this._loaded.set(false);
    this._error.set(null);
    this._serverTimeUtc.set(null);
  }

  private fetch(force: boolean): Observable<GameBootstrapDto | null> {
    if (!this.auth.isAuthenticated()) {
      this.reset();
      return of(null);
    }

    if (!force && this.canUseCachedBootstrap()) {
      return of(this._bootstrap());
    }

    if (this.inFlight$) {
      return this.inFlight$;
    }

    this._loading.set(true);
    this._error.set(null);

    this.inFlight$ = this.bootstrapService.get().pipe(
      tap((bootstrap) => this.hydrate(bootstrap)),
      catchError((err) => {
        this._error.set(err?.message ?? 'Failed to load game state');
        return throwError(() => err);
      }),
      finalize(() => {
        this._loading.set(false);
        this.inFlight$ = undefined;
      }),
      shareReplay(1),
    );

    return this.inFlight$;
  }

  private hydrate(bootstrap: GameBootstrapDto): void {
    this._bootstrap.set(bootstrap);
    this._serverTimeUtc.set(bootstrap.serverTimeUtc);
    this.auth.updateCharacter(bootstrap.character);
    this.tutorialState.initialize(bootstrap.tutorial, {
      resumeCurrentStep: true,
    });
    this.characterActionsState.initializeFromBootstrap(bootstrap.currentAction);
    this._loaded.set(true);
  }

  private canUseCachedBootstrap(): boolean {
    const bootstrap = this._bootstrap();
    const currentCharacter = this.auth.currentCharacter();

    return (
      this._loaded() &&
      !!bootstrap &&
      !!currentCharacter &&
      bootstrap.character.id === currentCharacter.id
    );
  }
}
