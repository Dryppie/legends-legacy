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
import { CharacterActionType } from '../../../models/enums/characterActionType';

@Component({
  selector: 'app-profession-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf],
  templateUrl: './profession-card.component.html',
  styleUrl: './profession-card.component.css',
})
export class ProfessionCardComponent {
  @Input() gatheringNodeName!: string;
  @Input() gatheringNodeLootTableId!: string;
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

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  specificCard(): boolean {
    return this.currentAction?.lootTableId == this.gatheringNodeLootTableId;
  }

  startGatheringAction() {
    const gatheringActionDetails: GatheringActionDetails = {
      name: this.gatheringNodeName,
      gatheringType: GatheringType.Woodcutting,
      lootTableId: this.gatheringNodeLootTableId,
    };
    console.log(this.gatheringNodeLootTableId);
    const startGatheringActionRequest: StartGatheringActionRequest = {
      gatheringActionDetails: gatheringActionDetails,
    };
    this.characterActionsService.startGatheringAction(
      startGatheringActionRequest,
    );
  }

  cancelCharacterAction() {
    this.characterActionsService.stopCharacterAction();
  }
}
