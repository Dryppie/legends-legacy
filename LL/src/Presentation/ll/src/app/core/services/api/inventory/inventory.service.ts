import { Injectable } from '@angular/core';
import { ApiService } from '../../api/api.service';
import { catchError, map, Observable, throwError } from 'rxjs';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { EssenceItem } from '../../../../shared/models/item';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  constructor(private apiService: ApiService) {}

  public getInventory(): Observable<InventoryDto> {
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

  public shatterEssence(
    essence: InventoryItem,
    amount: number,
  ): Observable<InventoryItem> {
    const shatterEssence = {
      essenceId: (essence.itemInstance.itemBase as EssenceItem).essence.id,
      amount,
    };
    return this.apiService.post('inventory/shatter', shatterEssence).pipe(
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
