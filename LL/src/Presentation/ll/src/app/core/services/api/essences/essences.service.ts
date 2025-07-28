import { Injectable } from '@angular/core';
import { ApiService } from '../../api/api.service';
import { Essence } from '../../../../shared/models/essence';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { EssenceSlot } from '../../../../shared/models/essenceSlot';
import { ToastService } from '../../client-side/components/toast/toast.service';

@Injectable({
  providedIn: 'root',
})
export class EssencesService {
  private equippedEssencesSubject = new BehaviorSubject<Essence[]>([]);

  equippedEssencesSubject$ = this.equippedEssencesSubject.asObservable();

  constructor(
    private apiService: ApiService,
    public toastService: ToastService,
  ) {}

  public getEquippedEssences(): Observable<EssenceSlot[]> {
    return this.apiService.get('essence/GetEquippedEssences').pipe(
      tap({
        next: (essences) => {
          this.equippedEssencesSubject.next(essences);
        },
      }),
      catchError((error) => {
        return throwError(() => error);
      }),
    );
  }

  public equipEssence(essenceId: string): Observable<boolean> {
    return this.apiService.post('essence/EquipEssence', essenceId).pipe(
      tap(() => {
        this.toastService.showToast(
          'Essence equipped successfully!',
          'success',
          true,
        );
      }),
    );
  }

  public deleteEquippedEssence(essenceId: string): void {
    this.apiService
      .post('essence/DeleteEquippedEssence', essenceId)
      .pipe()
      .subscribe({
        next: () => {
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
