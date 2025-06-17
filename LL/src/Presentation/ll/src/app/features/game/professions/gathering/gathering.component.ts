import {
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { map } from 'rxjs';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { NgFor, NgIf } from '@angular/common';
import { ProfessionCardComponent } from '../../../../shared/components/professions/profession-card/profession-card.component';
import { GatheringProfession } from '../../../../shared/models/profession';
import { toSignal } from '@angular/core/rxjs-interop';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';

@Component({
  selector: 'app-gathering',
  standalone: true,
  imports: [ProfessionHeaderComponent, ProfessionCardComponent, NgFor, NgIf],
  templateUrl: './gathering.component.html',
})
export class GatheringComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly professionService = inject(ProfessionsService);
  private readonly characterActionService = inject(
    CharacterActionsStateService,
  );

  readonly professionId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: '' },
  );

  readonly currentAction = this.characterActionService.currentAction;
  readonly characterProfessions = this.professionService.characterProfessions;

  readonly characterProfession = computed(() =>
    this.characterProfessions().find(
      (p) => p.professionType.toLowerCase() === this.professionId(),
    ),
  );

  readonly profession = signal<GatheringProfession | null>(null);
  readonly gatheringNodes = signal<GatheringNode[]>([]);

  combatStarted = false;

  constructor() {
    effect(
      () => {
        const id = this.professionId();
        if (id) {
          this.getProfessionDetails(id);
        }
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.professionService.refresh();
  }

  getProfessionDetails(id: string): void {
    const prof = this.professionService.getProfessionById(
      id,
    ) as GatheringProfession;
    this.profession.set(prof);
    this.gatheringNodes.set(prof.gatheringNodes);
  }
}
