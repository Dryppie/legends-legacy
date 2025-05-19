import { Component, Input } from '@angular/core';
import { MiniButtonComponent } from '../../mini-button/mini-button.component';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';
import {
  CharacterActionDto,
  StartGatheringActionRequest,
} from '../../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { NgIf } from '@angular/common';
import { GatheringType } from '../../../models/enums/gatheringType';
import { GatheringNode } from '../../../models/Dtos/gatheringNode';
import { CharacterProfession } from '../../../models/Dtos/characterProfession';

@Component({
  selector: 'app-profession-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf],
  templateUrl: './profession-card.component.html',
  styleUrl: './profession-card.component.css',
})
export class ProfessionCardComponent {
  @Input() gatheringNode!: GatheringNode;
  @Input() characterProfession!: CharacterProfession | null;
  currentAction: CharacterActionDto | null = null;
  private subscription: Subscription = new Subscription();
  isLocked = true;

  constructor(private characterActionsService: CharacterActionsService) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionsService.currentAction$.subscribe((action) => {
        this.currentAction = action;
      }),
    );
    this.setIsLocked();
  }

  // TODO: Rework this. Html is calling this continuously
  canStartAction(): boolean {
    return (
      this.currentAction == null ||
      (new Date(this.currentAction.updatedAt).getTime() <= Date.now() &&
        this.currentAction.isDeleted)
    );
  }
  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  specificCard(): boolean {
    return (
      this.currentAction?.gatheringActionDetails?.name == this.gatheringNode.id
    );
  }

  startGatheringAction() {
    const startGatheringActionRequest: StartGatheringActionRequest = {
      gatheringNodeId: this.gatheringNode.id,
      gatheringType: GatheringType.Woodcutting,
    };
    this.characterActionsService.startGatheringAction(
      startGatheringActionRequest,
    );
  }

  cancelCharacterAction() {
    this.characterActionsService.stopCharacterAction();
  }
  setIsLocked() {
    this.isLocked =
      !this.characterProfession ||
      this.characterProfession.level < this.gatheringNode.levelRequirement;
  }
}
