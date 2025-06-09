import { computed, Injectable, Signal } from '@angular/core';
import { ApiService } from '../api.service';
import {
  BehaviorSubject,
  Observable,
  shareReplay,
  startWith,
  Subject,
  switchMap,
} from 'rxjs';
import {
  CharacterDto,
  CharacterOverviewDto,
} from '../../../../shared/models/Dtos/characterDto';
import { AuthService } from '../auth/auth.service';
import { toSignal } from '@angular/core/rxjs-interop';

@Injectable({
  providedIn: 'root',
})
export class CharacterService {
  private readonly refresh$ = new Subject<void>();

  readonly currentCharacter: Signal<CharacterDto | null>;
  // readonly currentCharacterId: Signal<string | null>;
  readonly currentCharacterId = computed(
    () => this.currentCharacter()?.id ?? null,
  );
  /** cached, shared stream of professions */
  private readonly characterOverviewObservable$ = this.refresh$.pipe(
    // make the first request immediately
    startWith(void 0),
    // hit the API whenever refresh$ emits
    switchMap(() => this.getCharacterOverview().pipe()),
    // keep the latest value for all current & future subscribers
    shareReplay(1),
  );

  /** Public readonly stream.  Subscribe or use it with the async-pipe. */
  get characterOverview$(): Observable<CharacterOverviewDto> {
    return this.characterOverviewObservable$;
  }

  private characterOverviewSubject =
    new BehaviorSubject<CharacterOverviewDto | null>(null);

  // public characterOverview$ = this.characterOverviewSubject.asObservable();

  constructor(
    private api: ApiService,
    private authService: AuthService,
  ) {
    this.currentCharacter = toSignal(this.authService.currentCharacter$, {
      initialValue: null,
    });

    // this.currentCharacterId = computed(
    //   () => this.currentCharacter()?.id ?? null,
    // );
  }

  updateCharacter(updatedCharacter: CharacterDto): void {
    this.authService.updateCharacter(updatedCharacter);
  }

  getCurrentCharacter(): Observable<CharacterDto | null> {
    return this.authService.currentCharacter$;
  }

  public getCharacterOverview(): Observable<CharacterOverviewDto> {
    return this.api.get('Character/Overview');
  }
}
