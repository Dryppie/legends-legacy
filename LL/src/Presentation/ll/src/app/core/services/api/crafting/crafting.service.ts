import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { catchError, map, Observable, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class CraftingService {
  constructor(private api: ApiService) {}

  public craftItem(recipeId: string) {
    return this.api
      .post('Crafting/CraftItem', recipeId)
      .pipe(
        map((item) => {
          // this.toastService.showToast(
          //   'Action completed successfully!',
          //   'success',
          // );
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
