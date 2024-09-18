import { Injectable } from '@angular/core';
import { ApiService } from '../api/api.service';
import { catchError, Observable, tap, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  constructor(private apiService: ApiService) {}

  public getInventory(): Observable<any> {
    return this.apiService.get('inventory').pipe(
      tap((inventory) => {
        // this.toastService.showToast(
        //   'Action completed successfully!',
        //   'success',
        // );
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
