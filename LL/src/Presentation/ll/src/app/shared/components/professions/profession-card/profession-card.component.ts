import { Component, Input, OnInit } from '@angular/core';
import { MiniButtonComponent } from '../../mini-button/mini-button.component';
import { CharacterActionsService } from '../../../../core/services/api/character-actions/character-actions.service';
import { CharacterActionDto } from '../../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { NgIf } from '@angular/common';
import { GatheringNode } from '../../../models/Dtos/gatheringNode';
import {
  CharacterProfession,
  ProfessionType,
} from '../../../models/Dtos/characterProfession';

@Component({
  selector: 'app-profession-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf],
  templateUrl: './profession-card.component.html',
  styleUrl: './profession-card.component.css',
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
  constructor(private characterActionsService: CharacterActionsService) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionsService.currentAction$.subscribe((action) => {
        this.currentAction = action;
        this.setCanStartAction();
      }),
    );
    this.setIsLocked();
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

  startGatheringAction() {
    this.characterActionsService.startGatheringAction(this.gatheringNode.id);
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
