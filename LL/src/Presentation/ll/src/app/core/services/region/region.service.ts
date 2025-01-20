import { Injectable } from '@angular/core';
import { ApiService } from '../api/api.service';
import { catchError, map, Observable, of, throwError } from 'rxjs';
import { Region } from '../../../shared/models/Dtos/regionDto';

@Injectable({
  providedIn: 'root',
})
export class RegionService {
  constructor(private apiService: ApiService) {}

  public getRegionById(url: string): Observable<Region> {
    let region: Region = { name: '', areas: [] };
    if (url.includes('shenic')) {
      region = this.getShenicRegion();
    }
    // else if (url.includes('city')) {
    //   region = getCitySidebar();
    // } else if (url.includes('professions')) {
    //   region = getProfessionSidebar();
    // } else if (url.includes('world')) {
    //   region = getWorldSidebar();
    // }

    return of(region);
  }

  public getRegionById1(id: string): Observable<any> {
    return this.apiService.get(`region/${id}`).pipe(
      map((region) => {
        return region;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error(`Failed to get region: ${id}`));
      }),
    );
  }

  private getShenicRegion(): Region {
    let shenicRegion: Region = {
      name: 'Shenic',
      areas: [
        {
          name: 'Lumo Ruins',
          creatures: [
            'Goblin',
            'Goblin Archer',
            'Goblin Warrior',
            'Goblin Shaman',
            'Large Rat',
          ],
        },
        {
          name: 'Plains',
          creatures: [],
        },
        {
          name: 'Mountains',
          creatures: [],
        },
        {
          name: 'Swamp',
          creatures: [],
        },
        {
          name: 'Desert',
          creatures: [],
        },
      ],
    };

    return shenicRegion;
  }
}
