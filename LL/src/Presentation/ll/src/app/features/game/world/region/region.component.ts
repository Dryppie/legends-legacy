import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { RegionDto } from '../../../../shared/models/Dtos/regionDto';
import { RegionService } from '../../../../core/services/region/region.service';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { CharacterActionsService } from '../../../../core/services/character-actions/character-actions.service';
import {
  CharacterActionDto,
  CombatActionDetails,
  StartCombatActionRequest,
} from '../../../../shared/models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-region',
  standalone: true,
  imports: [NgIf, NgFor, ButtonComponent, CombatComponent],
  templateUrl: './region.component.html',
  styleUrl: './region.component.css',
})
export class RegionComponent implements OnInit {
  regionId!: string;
  region!: RegionDto; // You can define a more specific type based on your item data structure
  combatStarted = false;
  private subscription: Subscription = new Subscription();
  currentAction: CharacterActionDto | null = null;

  constructor(
    private route: ActivatedRoute,
    private regionService: RegionService,
    private characterActionService: CharacterActionsService,
  ) {}

  ngOnInit(): void {
    this.subscription.add(
      this.characterActionService.currentAction$.subscribe((action) => {
        this.currentAction = action;
      }),
    );
    this.route.paramMap.subscribe((params) => {
      this.regionId = params.get('id') ?? '';
      this.getRegionDetails(this.regionId);
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  getRegionDetails(id: string) {
    this.regionService.getRegionById(id).subscribe((data: any) => {
      this.region = data as RegionDto;
    });
  }

  Battle() {
    const CombatActionDetails: CombatActionDetails = {
      characterTeam: [],
      enemyTeam: this.region.areas
        .map((a) => a.creatures)
        .flat()
        .map((c) => c.id),
    };
    const startCharacterActionRequest: StartCombatActionRequest = {
      combatActionDetails: CombatActionDetails,
    };
    this.characterActionService.startCombatAction(startCharacterActionRequest);
    this.combatStarted = true;
  }
}
