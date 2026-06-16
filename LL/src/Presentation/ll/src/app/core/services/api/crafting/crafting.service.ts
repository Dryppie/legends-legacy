import { Injectable } from '@angular/core';
import { ApiService } from '../api.service';
import { BehaviorSubject, catchError, map, Observable, throwError } from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { CraftingQueueItem } from '../../../../shared/models/profession';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';

export interface RemoveCraftingQueueItemResponse {
  inventoryItems: InventoryItem[];
  currentAction: CharacterActionDto | null;
}

@Injectable({
  providedIn: 'root',
})
export class CraftingService {
  private readonly queueSubject = new BehaviorSubject<CraftingQueueItem[]>([]);
  /** Observable that callers (components, other services) can subscribe to */
  readonly craftingQueue$ = this.queueSubject.asObservable();

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

  removeItemFromQueue(
    queueItem: { id: string },
  ): Observable<RemoveCraftingQueueItemResponse> {
    return this.api.post('Crafting/RemoveCraftingQueueItem', queueItem.id).pipe(
      map((response) => {
        return response;
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

  setQueue(nextQueue: CraftingQueueItem[]): void {
    // Use a defensive copy so callers can keep mutating their own array safely
    this.queueSubject.next([...nextQueue]);
  }

  get currentQueue(): CraftingQueueItem[] {
    return this.queueSubject.value;
  }
}
