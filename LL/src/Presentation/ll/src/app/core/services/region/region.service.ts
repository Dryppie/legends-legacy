import { Injectable } from '@angular/core';
import { ApiService } from '../api/api.service';
import { Observable, of } from 'rxjs';
import { Region } from '../../../shared/models/Dtos/regionDto';

@Injectable({
  providedIn: 'root',
})
export class RegionService {
  constructor(private apiService: ApiService) {}

  public getRegionById(id: string): Observable<Region> {
    let region: Region = { name: '', areas: [], dungeons: [], raids: [] };
    if (id.includes('shenic')) {
      region = this.getShenicRegion();
    }
    // else if (id.includes('city')) {
    //   region = getCitySidebar();
    // } else if (id.includes('professions')) {
    //   region = getProfessionSidebar();
    // } else if (id.includes('world')) {
    //   region = getWorldSidebar();
    // }

    return of(region);
  }

  private getShenicRegion(): Region {
    let shenicRegion: Region = {
      name: 'Shenic',
      areas: [
        {
          id: 'region_01_area_01',
          name: 'Lumo Ruins',
          creatures: ['Goblin', 'Goblin Archer', 'Goblin Warrior', 'Large Rat'],
        },
        {
          id: 'region_01_area_02',
          name: 'Blood Grove',
          creatures: ['Flame Imp', 'Frost Imp', 'Shadow Imp', 'Vampire Bat'],
        },
        {
          id: 'region_01_area_03',
          name: 'Crystal Creek',
          creatures: [
            'Blue Slime',
            'Brown Slime',
            'Green Slime',
            'Rainbow Slime',
            'Red Slime',
            'Transparent Slime',
          ],
        },
        // {
        //   name: 'Oak Thicket',
        //   creatures: [],
        // },
        // {
        //   name: 'Old Forest',
        //   creatures: [],
        // },
        // {
        //   name: 'Twilight Clearing',
        //   creatures: [],
        // },
      ],
      dungeons: [],
      raids: [],
    };

    return shenicRegion;
  }
}
