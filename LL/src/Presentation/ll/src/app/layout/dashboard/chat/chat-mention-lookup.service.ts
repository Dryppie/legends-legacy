import { Injectable } from '@angular/core';
import { catchError, defer, map, Observable, of, shareReplay } from 'rxjs';
import { CharacterService } from '../../../core/services/api/character/character.service';

@Injectable({ providedIn: 'root' })
export class ChatMentionLookupService {
  private readonly cache = new Map<
    string,
    { expiresAt: number; result: Observable<string | null> }
  >();

  constructor(private readonly characters: CharacterService) {}

  resolve(name: string): Observable<string | null> {
    const trimmedName = name.trim();
    if (!trimmedName || trimmedName.length > 26) return of(null);
    const key = trimmedName.toLowerCase();
    const cached = this.cache.get(key);
    if (cached && cached.expiresAt > Date.now()) return cached.result;

    // Share exact lookups across repeated mentions and chat layouts. Expire
    // results so renames, new characters and temporary failures can recover.
    const result = defer(() =>
      this.characters.resolveCharacterIdByName(trimmedName),
    ).pipe(
      map((id) =>
        id && id !== '00000000-0000-0000-0000-000000000000' ? id : null,
      ),
      catchError(() => of(null)),
      shareReplay({ bufferSize: 1, refCount: true }),
    );
    if (this.cache.size >= 256)
      this.cache.delete(this.cache.keys().next().value!);
    this.cache.set(key, { expiresAt: Date.now() + 60_000, result });
    return result;
  }
}
