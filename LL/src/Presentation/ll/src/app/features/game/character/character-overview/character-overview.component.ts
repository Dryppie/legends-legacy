import { NgIf } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { catchError, EMPTY, take } from 'rxjs';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterAttributesComponent } from '../../../../shared/components/character/character-attributes/character-attributes.component';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { CharacterOverviewDto } from '../../../../shared/models/Dtos/characterDto';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-character-overview',
  standalone: true,
  imports: [
    DefaultHeaderComponent,
    CharacterAttributesComponent,
    NgIf,
    FormsModule,
    RegularButtonComponent,
  ],
  templateUrl: './character-overview.component.html',
})
export class CharacterOverviewComponent {
  searchValue = signal('');
  character = signal<CharacterOverviewDto | null>(null);

  constructor(private characterService: CharacterService) {
    this.characterService.characterOverview$
      .pipe(take(1))
      .subscribe((c) => this.character.set(c));
  }

  onSearch() {
    const trimmed = this.searchValue().trim();
    if (!trimmed) return;

    this.characterService
      .searchCharacter(trimmed)
      .pipe(
        catchError((err) => {
          console.error(err.message);
          return EMPTY;
        }),
      )
      .subscribe((character) => {
        this.character.set(character);
      });
  }

  onEnter(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.onSearch();
    }
  }
}
