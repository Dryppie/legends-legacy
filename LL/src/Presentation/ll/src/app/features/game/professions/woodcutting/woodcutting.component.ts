import { Component } from '@angular/core';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { ProfessionCardComponent } from '../../../../shared/components/professions/profession-card/profession-card.component';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { NgFor } from '@angular/common';

@Component({
  selector: 'app-woodcutting',
  standalone: true,
  imports: [ProfessionHeaderComponent, ProfessionCardComponent, NgFor],
  templateUrl: './woodcutting.component.html',
  styleUrl: './woodcutting.component.css',
})
export class WoodcuttingComponent {
  gatheringNodes: GatheringNode[] = [
    {
      id: '1',
      name: 'Tree',
      lootTableId: 'dfbb3a5b-8ea1-47ab-b71d-92f1dbb4cc85',
    },
    {
      id: '1',
      name: 'Oak Tree',
      lootTableId: '34fe102f-139e-4701-af3f-209b9899ef8e',
    },
    {
      id: '1',
      name: 'Birch Tree',
      lootTableId: '613cb7d0-5a81-45df-b630-03d26f7973b4',
    },
  ];
}
