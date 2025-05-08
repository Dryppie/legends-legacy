import { Injectable } from '@angular/core';
import { ApiService } from '../../api/api.service';
import { catchError, map, Observable, throwError } from 'rxjs';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import { CharacterManagerService } from '../../client-side/character-manager/character-manager.service';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  constructor(
    private apiService: ApiService,
    private characterManager: CharacterManagerService,
  ) {}

  public getInventory(): Observable<InventoryDto> {
    return this.apiService.get('inventory').pipe(
      map((inventory) => {
        this.characterManager.setInventory(inventory);
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
