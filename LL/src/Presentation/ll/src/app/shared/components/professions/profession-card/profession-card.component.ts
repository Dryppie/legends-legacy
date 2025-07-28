import { Component, effect, Input, OnInit } from '@angular/core';
import { MiniButtonComponent } from '../../custom-components/buttons/mini-button/mini-button.component';
import { CharacterActionDto } from '../../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { NgIf } from '@angular/common';
import { GatheringNode } from '../../../models/Dtos/gatheringNode';
import {
  CharacterProfession,
  ProfessionType,
} from '../../../models/Dtos/characterProfession';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../models/enums/characterActionType';

@Component({
  selector: 'app-profession-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf],
  templateUrl: './profession-card.component.html',
})
export class ProfessionCardComponent implements OnInit {
  @Input() gatheringNode!: GatheringNode;
  @Input() characterProfession!: CharacterProfession;
  @Input() iconPath: string = '';
  @Input() professionType!: ProfessionType;

  currentAction: CharacterActionDto | null = null;
  private subscription: Subscription = new Subscription();
  isLocked = true;
  canStartAction: boolean = false;
  startActionText: string = '';

  constructor(public state: CharacterActionsStateService) {
    effect(() => {
      const action = this.state.currentAction();
      this.currentAction = action;
      this.setCanStartAction();
    });
  }

  ngOnInit(): void {
    this.setIsLocked();
    this.startActionText =
      this.professionType === ProfessionType.Mining ? 'Mine' : 'Cut';
  }

  setCanStartAction() {
    this.canStartAction =
      this.currentAction == null ||
      (new Date(this.currentAction.updatedAt).getTime() <= Date.now() &&
        this.currentAction.isDeleted);
  }
  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  specificCard(): boolean {
    return (
      this.currentAction?.gatheringActionDetails?.name == this.gatheringNode.id
    );
  }

  startGatheringAction(): void {
    this.state.startAction(
      CharacterActionType.Gathering,
      this.gatheringNode.id,
    );
  }

  cancelCharacterAction(): void {
    this.state.stopAction();
  }

  setIsLocked() {
    this.isLocked =
      !this.characterProfession ||
      this.characterProfession.level < this.gatheringNode.levelRequirement;
  }
}
