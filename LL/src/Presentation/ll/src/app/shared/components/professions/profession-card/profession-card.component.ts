import { Component, Input } from '@angular/core';
import { MiniButtonComponent } from '../../mini-button/mini-button.component';
import { CharacterActionsService } from '../../../../core/services/character-actions/character-actions.service';
import { CharacterActionDto } from '../../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-profession-card',
  standalone: true,
  imports: [MiniButtonComponent, NgIf],
  templateUrl: './profession-card.component.html',
  styleUrl: './profession-card.component.css',
})
export class ProfessionCardComponent {
  @Input() professionNodeLootTable!: string;
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
    return this.currentAction?.lootTableId == this.professionNodeLootTable;
  }

  startCharacterAction() {
    this.characterActionsService.startCharacterAction();
  }

  cancelCharacterAction() {
    this.characterActionsService.stopCharacterAction();
  }
}
