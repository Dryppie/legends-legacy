import { NgFor, NgIf } from '@angular/common';
import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Region } from '../../../../shared/models/Dtos/regionDto';
import { RegionService } from '../../../../core/services/client-side/region/region.service';
import { CombatAreaCardComponent } from '../../../../shared/components/combat/combat-area-card/combat-area-card.component';
import { TourService } from '../../../../core/services/client-side/tutorial-tour/tour.service';
import { TabsComponent } from '../../../../shared/components/custom-components/tabs/tabs.component';
import { TabComponent } from '../../../../shared/components/custom-components/tabs/tab/tab.component';
import { RaidsComponent } from './raids/raids.component';
import { DungeonsComponent } from './dungeons/dungeons.component';

@Component({
  selector: 'app-region',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    CombatAreaCardComponent,
    TabsComponent,
    TabComponent,
    RaidsComponent,
    DungeonsComponent,
  ],
  templateUrl: './region.component.html',
})
export class RegionComponent implements OnInit {
  regionId!: string;
  region!: Region; // You can define a more specific type based on your item data structure

  constructor(
    private route: ActivatedRoute,
    private regionService: RegionService,
    private tour: TourService,
  ) {
    this.tour.start('world-page');
  }

  ngOnInit(): void {
    this.setColumnCount();

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

  columnCount = 1;
  @HostListener('window:resize')
  onResize() {
    this.setColumnCount();
  }

  private setColumnCount() {
    const width = window.innerWidth;

    if (width >= 1280)
      this.columnCount = 4; // xl
    else if (width >= 1024)
      this.columnCount = 3; // lg
    else if (width >= 768)
      this.columnCount = 2; // md
    else this.columnCount = 0; // base
  }

  isLastInRow(index: number): boolean {
    return (index + 1) % this.columnCount === 0;
  }
}
