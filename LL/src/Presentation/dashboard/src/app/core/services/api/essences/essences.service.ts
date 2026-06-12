import { Injectable } from '@angular/core';
import { ApiService } from '../../api/api.service';
import { Essence } from '../../../../shared/models/essence';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { switchMap, tap } from 'rxjs/operators';
import { ToastService } from '../../client-side/toast/toast.service';

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
    return of({ equippedEssences: [], inventoryEssences: [] }).pipe(
      tap((essences) => this.equippedAndInventoryEssencesSubject.next(essences)),
    );
  }

  public equipEssence(essenceId: string): void {
    this.apiService
      .post(`essence/items/${essenceId}/absorb`, {})
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
    void essenceId;
  }
}

interface EquippedAndInventoryEssences {
  // Define the properties of the interface here
  equippedEssences: Essence[];
  inventoryEssences: Essence[];
}
