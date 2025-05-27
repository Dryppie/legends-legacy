import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';
import { Subscription } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { NgFor, NgIf } from '@angular/common';
import { ProfessionCardComponent } from '../../../../shared/components/professions/profession-card/profession-card.component';
import { GatheringProfession } from '../../../../shared/models/profession';
import { CharacterProfession } from '../../../../shared/models/Dtos/characterProfession';

@Component({
  selector: 'app-gathering',
  standalone: true,
  imports: [ProfessionHeaderComponent, ProfessionCardComponent, NgFor, NgIf],
  templateUrl: './gathering.component.html',
  styleUrl: './gathering.component.css',
})
export class GatheringComponent {
  professionId!: string;
  profession!: GatheringProfession;
  gatheringNodes!: GatheringNode[];
  combatStarted = false;
  private subscription: Subscription = new Subscription();
  currentAction: CharacterActionDto | null = null;
  characterProfessions: CharacterProfession[] = [];
  characterProfession!: CharacterProfession;

  constructor(
    private route: ActivatedRoute,
    private professionService: ProfessionsService,
    private characterActionService: CharacterActionsService,
  ) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionService.currentAction$.subscribe((action) => {
        this.currentAction = action;
      }),
    );
    this.route.paramMap.subscribe((params) => {
      this.professionId = params.get('id') ?? '';
      this.getProfessionDetails(this.professionId);
      this.professionService.characterProfessions$.subscribe(
        (characterProfessions) => {
          this.characterProfessions = characterProfessions;
          this.getCharacterProfession();
        },
      );
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  getCharacterProfession() {
    this.characterProfession = this.characterProfessions.find(
      (p) => p.professionType.toLowerCase() === this.professionId, // or p.id, p.type, etc.
    )!;
  }

  getProfessionDetails(id: string) {
    this.profession = this.professionService.getProfessionById(
      id,
    ) as GatheringProfession;
    this.gatheringNodes = this.profession.gatheringNodes;
  }
}
