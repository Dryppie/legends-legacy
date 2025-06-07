import { Component, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';
import { map } from 'rxjs';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { NgFor, NgIf } from '@angular/common';
import { ProfessionCardComponent } from '../../../../shared/components/professions/profession-card/profession-card.component';
import { GatheringProfession } from '../../../../shared/models/profession';
import { toSignal } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-gathering',
  standalone: true,
  imports: [ProfessionHeaderComponent, ProfessionCardComponent, NgFor, NgIf],
  templateUrl: './gathering.component.html',
})
export class GatheringComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly professionService = inject(ProfessionsService);
  private readonly characterActionService = inject(CharacterActionsService);

  readonly professionId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: '' },
  );

  readonly currentAction = toSignal(
    this.characterActionService.currentAction$,
    { initialValue: null },
  );
  readonly characterProfessions = this.professionService.characterProfessions;
  readonly characterProfession = computed(() =>
    this.characterProfessions().find(
      (p) => p.professionType.toLocaleLowerCase() === this.professionId(),
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

  ngOnDestroy(): void {}

  getProfessionDetails(id: string) {
    const prof = this.professionService.getProfessionById(
      id,
    ) as GatheringProfession;
    this.profession.set(prof);
    this.gatheringNodes.set(prof.gatheringNodes);
  }
}
