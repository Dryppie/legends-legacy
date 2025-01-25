import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/professions/professions.service';
import { CharacterActionsService } from '../../../../core/services/character-actions/character-actions.service';
import { Subscription } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { ProfessionCardComponent } from '../../../../shared/components/professions/profession-card/profession-card.component';
import { NgFor } from '@angular/common';

@Component({
  selector: 'app-gathering',
  standalone: true,
  imports: [ProfessionHeaderComponent, ProfessionCardComponent, NgFor],
  templateUrl: './gathering.component.html',
  styleUrl: './gathering.component.css',
})
export class GatheringComponent {
  professionId!: string;
  gatheringNodes!: GatheringNode[];
  combatStarted = false;
  private subscription: Subscription = new Subscription();
  currentAction: CharacterActionDto | null = null;

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
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  getProfessionDetails(id: string) {
    this.professionService.getGatheringNodesById(id).subscribe((data: any) => {
      this.gatheringNodes = data as GatheringNode[];
    });
  }
}
