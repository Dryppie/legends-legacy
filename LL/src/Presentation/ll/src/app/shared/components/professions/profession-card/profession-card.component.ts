import { Component, Input, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { GatheringNode } from '../../../models/Dtos/gatheringNode';
import {
  CharacterProfession,
  ProfessionType,
} from '../../../models/Dtos/characterProfession';

@Component({
  selector: 'app-profession-card',
  standalone: true,
  imports: [NgIf],
  templateUrl: './profession-card.component.html',
})
export class ProfessionCardComponent implements OnInit {
  @Input() gatheringNode!: GatheringNode;
  @Input() characterProfession!: CharacterProfession;
  @Input() iconPath: string = '';
  @Input() professionType!: ProfessionType;

  isLocked = true;

  ngOnInit(): void {
    this.setIsLocked();
  }

  setIsLocked() {
    this.isLocked =
      !this.characterProfession ||
      this.characterProfession.level < this.gatheringNode.levelRequirement;
  }
}
