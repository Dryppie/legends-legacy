import { NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { RegionDto } from '../../../../shared/models/Dtos/regionDto';
import { RegionService } from '../../../../core/services/region/region.service';
import { ButtonComponent } from '../../../../shared/components/button/button.component';

@Component({
  selector: 'app-region',
  standalone: true,
  imports: [NgIf, ButtonComponent],
  templateUrl: './region.component.html',
  styleUrl: './region.component.css',
})
export class RegionComponent implements OnInit {
  regionId!: string;
  region!: RegionDto; // You can define a more specific type based on your item data structure

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

  getRegionDetails(id: string) {
    this.regionService.getRegionById(id).subscribe((data: any) => {
      this.region = data as RegionDto;
    });
  }

  Battle() {
    throw new Error('Method not implemented.');
  }
}
