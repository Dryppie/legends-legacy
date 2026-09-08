import { Injectable, OnDestroy, signal } from '@angular/core';
import { catchError, EMPTY, of, Subject, switchMap, timer } from 'rxjs';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { DraftMention } from './chat-mentions';

/** Scoped to one chat composer so drawers and mobile chat have independent state. */
@Injectable()
export class ChatMentionSuggestionsService implements OnDestroy {
  readonly active = signal<DraftMention | null>(null);
  readonly names = signal<string[]>([]);
  readonly selectedIndex = signal(-1);
  readonly loading = signal(false);
  readonly error = signal(false);
  private readonly searches = new Subject<DraftMention | null>();
  private readonly subscription;

  constructor(characterService: CharacterService) {
    this.subscription = this.searches
      .pipe(
        switchMap((mention) => {
          this.names.set([]);
          this.selectedIndex.set(-1);
          this.error.set(false);
          this.loading.set(!!mention && mention.query.trim().length >= 2);
          if (!this.loading() || !mention) return EMPTY;

          return timer(200).pipe(
            switchMap(() =>
              characterService.suggestCharacterNames(mention.query.trim()),
            ),
            catchError(() => {
              this.error.set(true);
              return of([] as string[]);
            }),
          );
        }),
      )
      .subscribe((names) => {
        this.loading.set(false);
        this.names.set(names);
        this.selectedIndex.set(names.length ? 0 : -1);
      });
  }

  update(mention: DraftMention | null): void {
    const previous = this.active();
    this.active.set(mention);
    if (
      previous?.query === mention?.query &&
      previous?.start === mention?.start
    )
      return;
    this.searches.next(mention);
  }

  close(): void {
    this.active.set(null);
    this.searches.next(null);
  }

  moveSelection(direction: number): void {
    const count = this.names().length;
    if (count)
      this.selectedIndex.update((index) => (index + direction + count) % count);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.searches.complete();
  }
}
