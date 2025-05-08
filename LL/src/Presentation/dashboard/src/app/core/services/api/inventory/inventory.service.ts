import { Injectable } from '@angular/core';
import { ApiService } from '../../api/api.service';
import { catchError, map, Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  constructor(private apiService: ApiService) {}

  public getInventory(): Observable<any> {
    return this.apiService.get('inventory').pipe(
      map((inventory) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
        return inventory;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to get inventory'));
      }),
    );
  }
}
