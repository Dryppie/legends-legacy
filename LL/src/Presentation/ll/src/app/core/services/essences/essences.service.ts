import { Injectable } from '@angular/core';
import { ApiService } from '../api/api.service';
import { ToastService } from '../toast/toast.service';
import { Essence } from '../../../shared/models/essence';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class EssencesService {
  private equippedAndInventoryEssencesSubject = new BehaviorSubject<{
    equippedEssences: Essence[];
    inventoryEssences: Essence[];
  }>({
    equippedEssences: [],
    inventoryEssences: [],
  });

  equippedAndInventoryEssencesSubject$ =
    this.equippedAndInventoryEssencesSubject.asObservable();

  constructor(
    private apiService: ApiService,
    public toastService: ToastService,
  ) {}

  public getEquippedEssencesAndInventoryEssences(): Observable<EquippedAndInventoryEssences> {
    return this.apiService
      .get('essence/GetEquippedEssencesAndInventoryEssences')
      .pipe(
        tap({
          next: (essences) => {
            this.equippedAndInventoryEssencesSubject.next({
              equippedEssences: essences.equippedEssences,
              inventoryEssences: essences.inventoryEssences,
            });
          },
        }),
        catchError((error) => {
          return throwError(() => error);
        }),
      );
  }

  public equipEssence(essenceId: string): void {
    this.apiService
      .post('essence/EquipEssence', essenceId)
      .pipe(switchMap(() => this.getEquippedEssencesAndInventoryEssences()))
      .subscribe({
        next: () => {
          this.getEquippedEssencesAndInventoryEssences();
          this.toastService.showToast(
            'Essence equipped successfully!',
            'success',
            true,
          );
        },
        error: (error) => {
          console.error('Failed to equip essence: ', error);
        },
      });
  }

  public deleteEquippedEssence(essenceId: string): void {
    this.apiService
      .post('essence/DeleteEquippedEssence', essenceId)
      .pipe(switchMap(() => this.getEquippedEssencesAndInventoryEssences()))
      .subscribe({
        next: () => {
          this.getEquippedEssencesAndInventoryEssences();
          this.toastService.showToast(
            'Essence removed successfully!',
            'success',
            true,
          );
        },
        error: (error) => {
          console.error('Failed to remove essence: ', error);
        },
      });
  }
}

interface EquippedAndInventoryEssences {
  // Define the properties of the interface here
  equippedEssences: Essence[];
  inventoryEssences: Essence[];
}
