import { Injectable } from '@angular/core';
import { ApiService } from '../api/api.service';
import { catchError, map, Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class RegionService {
  constructor(private apiService: ApiService) {}

  public getRegionById(id: string): Observable<any> {
    return this.apiService.get(`region/${id}`).pipe(
      map((region) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
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
}
