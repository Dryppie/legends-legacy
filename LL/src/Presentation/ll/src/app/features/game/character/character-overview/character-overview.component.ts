import { Component } from '@angular/core';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { EquippedEssencesComponent } from '../../../../shared/components/essences/equipped-essences/equipped-essences.component';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterOverviewDto } from '../../../../shared/models/Dtos/characterDto';
import { Observable } from 'rxjs';
import { CharacterAttributesComponent } from '../../../../shared/components/character/character-attributes/character-attributes.component';
import { AsyncPipe, NgIf } from '@angular/common';

@Component({
  selector: 'app-character-overview',
  standalone: true,
  imports: [
    DefaultHeaderComponent,
    EquippedEssencesComponent,
    CharacterAttributesComponent,
    AsyncPipe,
    NgIf,
  ],
  templateUrl: './character-overview.component.html',
  styleUrl: './character-overview.component.css',
})
export class CharacterOverviewComponent {
  showItemInfo = false;
  itemName = '';
  itemDescription = '';
  itemImage = '';

  character$!: Observable<CharacterOverviewDto | null>;

  constructor(private characterService: CharacterService) {}

  ngOnInit() {
    this.character$ = this.characterService.characterOverview$;

    this.characterService.getCharacterOverview();
  }
}
