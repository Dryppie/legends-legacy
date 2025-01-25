import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { GatheringNode } from '../../../shared/models/Dtos/gatheringNode';
import { ApiService } from '../api/api.service';

@Injectable({
  providedIn: 'root',
})
export class ProfessionsService {
  constructor(private apiService: ApiService) {}

  getGatheringNodesById(id: string): Observable<GatheringNode[]> {
    let gatheringNodes: GatheringNode[] = [];
    if (id.includes('woodcutting')) {
      gatheringNodes = this.getWoodcuttingNodes();
    }

    return of(gatheringNodes);
  }

  getWoodcuttingNodes(): GatheringNode[] {
    let gatheringNodes: GatheringNode[] = [
      {
        id: 'woodcutting_tree',
        name: 'Tree',
      },
      {
        id: 'woodcutting_oak',
        name: 'Oak Tree',
      },
      {
        id: 'woodcutting_birch',
        name: 'Birch Tree',
      },
    ];

    return gatheringNodes;
  }
}
