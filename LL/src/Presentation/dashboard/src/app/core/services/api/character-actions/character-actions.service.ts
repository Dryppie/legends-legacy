import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { ApiService } from '../api.service';

@Injectable({ providedIn: 'root' })
export class CharacterActionsService {
  private readonly currentActionSubject =
    new BehaviorSubject<CharacterActionDto | null>(null);

  readonly currentAction$ = this.currentActionSubject.asObservable();

  constructor(private readonly apiService: ApiService) {}

  getCurrentAction(): Observable<CharacterActionDto | null> {
    return this.apiService.get('CharacterActions').pipe(
      tap((action) => this.currentActionSubject.next(action)),
    );
  }

  stopCharacterAction(): void {
    this.apiService.delete('CharacterActions').subscribe(() => {
      this.currentActionSubject.next(null);
    });
  }
}
