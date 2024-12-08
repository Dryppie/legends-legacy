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
      lootTableId: '825e73e6-d1cd-41c0-9ed8-9980da8ec3b1',
    },
    {
      id: '1',
      name: 'Oak Tree',
      lootTableId: '8d858bf1-7ae1-4210-bdfa-c23f44a56a45',
    },
    {
      id: '1',
      name: 'Birch Tree',
      lootTableId: 'a80e547f-f4ed-4697-8eb2-73249c85d957',
    },
  ];
}
