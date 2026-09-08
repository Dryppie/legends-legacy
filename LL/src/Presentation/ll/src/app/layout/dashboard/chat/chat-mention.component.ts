import { AsyncPipe, NgIf } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { startWith, switchMap } from 'rxjs';
import { CharacterTagComponent } from '../../../shared/components/character/character-tag/character-tag.component';
import { ChatMentionLookupService } from './chat-mention-lookup.service';

@Component({
  selector: 'app-chat-mention',
  imports: [AsyncPipe, NgIf, CharacterTagComponent],
  template: `<app-character-tag
      *ngIf="characterId$ | async as id; else plainText"
      [id]="id"
      [name]="name()"
      [mention]="true"
    ></app-character-tag
    ><ng-template #plainText>{{ rawText() }}</ng-template>`,
  host: { '(keydown)': '$event.stopPropagation()' },
})
export class ChatMentionComponent {
  readonly name = input.required<string>();
  readonly rawText = input.required<string>();
  private readonly lookup = inject(ChatMentionLookupService);
  readonly characterId$ = toObservable(this.name).pipe(
    switchMap((name) => this.lookup.resolve(name).pipe(startWith(null))),
  );
}
