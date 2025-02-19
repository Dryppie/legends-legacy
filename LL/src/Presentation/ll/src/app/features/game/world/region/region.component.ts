import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Region } from '../../../../shared/models/Dtos/regionDto';
import { RegionService } from '../../../../core/services/region/region.service';
import { CombatAreaCardComponent } from '../../../../shared/components/combat/combat-area-card/combat-area-card.component';

@Component({
  selector: 'app-region',
  standalone: true,
  imports: [NgIf, NgFor, CombatAreaCardComponent],
  templateUrl: './region.component.html',
  styleUrl: './region.component.css',
})
export class RegionComponent implements OnInit {
  regionId!: string;
  region!: Region; // You can define a more specific type based on your item data structure

  constructor(
    private route: ActivatedRoute,
    private regionService: RegionService,
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.regionId = params.get('id') ?? '';
      this.getRegionDetails(this.regionId);
    });
  }

  ngOnDestroy(): void {}

  getRegionDetails(id: string) {
    this.regionService.getRegionById(id).subscribe((data: any) => {
      this.region = data as Region;
    });
  }
}
