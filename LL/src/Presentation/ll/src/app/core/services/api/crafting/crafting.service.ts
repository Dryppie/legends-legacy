import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { catchError, map, throwError } from 'rxjs';
import { ToastService } from '../../client-side/toast/toast.service';

@Injectable({
  providedIn: 'root',
})
export class CraftingService {
  constructor(
    private api: ApiService,
    private toast: ToastService,
  ) {}

  public craftItem(recipeId: string) {
    return this.api
      .post('Crafting/CraftItem', recipeId)
      .pipe(
        map((item) => {
          this.toast.showToast(`Crafted item!`, 'success', true, 'tr');
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
      )
      .subscribe();
  }
}
