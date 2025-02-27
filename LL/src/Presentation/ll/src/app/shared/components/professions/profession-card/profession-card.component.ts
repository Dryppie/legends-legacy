import { Component, Input } from '@angular/core';
import { MiniButtonComponent } from '../../mini-button/mini-button.component';
import { CharacterActionsService } from '../../../../core/services/character-actions/character-actions.service';
import {
  CharacterActionDto,
  GatheringActionDetails,
  StartGatheringActionRequest,
} from '../../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { NgIf } from '@angular/common';
import { GatheringType } from '../../../models/enums/gatheringType';

@Component({
  selector: 'app-profession-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf],
  templateUrl: './profession-card.component.html',
  styleUrl: './profession-card.component.css',
})
export class ProfessionCardComponent {
  @Input() gatheringNodeId!: string;
  @Input() gatheringNodeName!: string;
  currentAction: CharacterActionDto | null = null;
  private subscription: Subscription = new Subscription();

  constructor(private characterActionsService: CharacterActionsService) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionsService.currentAction$.subscribe((action) => {
        this.currentAction = action;
      }),
    );
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
      this.currentAction?.gatheringActionDetails?.name == this.gatheringNodeId
    );
  }

  startGatheringAction() {
    const startGatheringActionRequest: StartGatheringActionRequest = {
      gatheringNodeId: this.gatheringNodeId,
      gatheringType: GatheringType.Woodcutting,
    };
    this.characterActionsService.startGatheringAction(
      startGatheringActionRequest,
    );
  }

  cancelCharacterAction() {
    this.characterActionsService.stopCharacterAction();
  }
}
