import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Region } from '../../../../shared/models/Dtos/regionDto';
import { RegionService } from '../../../../core/services/region/region.service';
import { CombatComponent } from '../../../../shared/components/combat/combat.component';
import { CharacterActionsService } from '../../../../core/services/character-actions/character-actions.service';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { CombatAreaCardComponent } from '../../../../shared/components/combat/combat-area-card/combat-area-card.component';

@Component({
  selector: 'app-region',
  standalone: true,
  imports: [NgIf, NgFor, CombatComponent, CombatAreaCardComponent],
  templateUrl: './region.component.html',
  styleUrl: './region.component.css',
})
export class RegionComponent implements OnInit {
  regionId!: string;
  region!: Region; // You can define a more specific type based on your item data structure
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
      this.region = data as Region;
    });
  }
}
