import { AsyncPipe, NgIf } from '@angular/common';
import { Component, EventEmitter, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterAttributesComponent } from '../../../../shared/components/character/character-attributes/character-attributes.component';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { EquippedEssencesComponent } from '../../../../shared/components/essences/equipped-essences/equipped-essences.component';
import { CharacterOverviewDto } from '../../../../shared/models/Dtos/characterDto';

@Component({
  selector: 'app-character-overview',
  standalone: true,
  imports: [
    DefaultHeaderComponent,
    EquippedEssencesComponent,
    CharacterAttributesComponent,
    AsyncPipe,
    NgIf,
    FormsModule,
  ],
  templateUrl: './character-overview.component.html',
})
export class CharacterOverviewComponent {
  readonly character$!: Observable<CharacterOverviewDto>;

  searchValue = '';

  @Output() search = new EventEmitter<string>();

  constructor(private characterService: CharacterService) {
    // `characterService` is already injected → safe to use here
    this.character$ = this.characterService.characterOverview$;
  }

  onSearch() {
    const trimmed = this.searchValue.trim();
    if (trimmed) {
      this.search.emit(trimmed);
    }
  }

  onEnter(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.onSearch();
    }
  }
}
