import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { catchError, map, Observable, throwError } from 'rxjs';
import { ToastService } from '../../client-side/toast/toast.service';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

@Injectable({
  providedIn: 'root',
})
export class CraftingService {
  constructor(
    private api: ApiService,
    private toast: ToastService,
  ) {}

  public craftItem(recipeId: string): Observable<InventoryItem> {
    return this.api.post('Crafting/CraftItem', recipeId).pipe(
      map((item: InventoryItem) => {
        this.toast.showToast(
          `Crafted ${item.itemInstance.itemBase.name}`,
          'success',
          true,
          'tr',
        );
        return item;
      }),

      catchError(() => {
        // this.toastService.showToast(
        //   'Login Failed',
        //   'Wrong email or password',
        //   'error',
        //   't',
        // );
        return throwError(() => new Error('Failed to craft item'));
      }),
    );
  }
}
