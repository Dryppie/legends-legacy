import { Injectable } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { ApiService } from '../api.service';
import {
  BehaviorSubject,
  catchError,
  map,
  Observable,
  Subject,
  throwError,
} from 'rxjs';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { CraftingQueueItem } from '../../../../shared/models/profession';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import {
  CraftingRecipe,
  CraftItemsRequest,
  CraftItemsResult,
  LearnBlueprintResult,
} from '../../../../shared/models/crafting-v2';
import { ApiResponse } from '../../../../shared/models/response';

export interface RemoveCraftingQueueItemResponse {
  inventoryItems: InventoryItem[];
  currentAction: CharacterActionDto | null;
}

@Injectable({
  providedIn: 'root',
})
export class CraftingService {
  private readonly queueSubject = new BehaviorSubject<CraftingQueueItem[]>([]);
  private readonly blueprintLearnedSubject =
    new Subject<LearnBlueprintResult>();
  /** Observable that callers (components, other services) can subscribe to */
  readonly craftingQueue$ = this.queueSubject.asObservable();
  readonly blueprintLearned$ = this.blueprintLearnedSubject.asObservable();

  constructor(
    private api: ApiService,
    private toast: ToastService,
  ) {}

  public getRecipes(targetTier = 1): Observable<CraftingRecipe[]> {
    const params = new HttpParams().set('targetTier', targetTier);
    return this.api.get('Crafting/recipes', params).pipe(
      map((response) => this.unwrapResponse<CraftingRecipe[]>(response)),
      catchError(() =>
        throwError(() => new Error('Failed to load crafting recipes')),
      ),
    );
  }

  public craftItems(request: CraftItemsRequest): Observable<CraftItemsResult> {
    return this.api.post('Crafting/craft', request).pipe(
      map((response) => {
        const result = this.unwrapResponse<CraftItemsResult>(response);
        const count = result.createdItems.length;
        this.toast.showToast(
          `Crafted ${count} item${count === 1 ? '' : 's'}`,
          'success',
          true,
          'tr',
        );
        return result;
      }),
      catchError(() => throwError(() => new Error('Failed to craft items'))),
    );
  }

  public learnBlueprint(
    blueprintItemInstanceId: string,
    recipeId: string,
  ): Observable<LearnBlueprintResult> {
    return this.api
      .post('Crafting/blueprints/learn', { blueprintItemInstanceId, recipeId })
      .pipe(
        map((response) => {
          const result = this.unwrapResponse<LearnBlueprintResult>(response);
          this.blueprintLearnedSubject.next(result);
          this.toast.showToast(
            `Learned ${result.blueprintName} for ${result.recipeName}`,
            'success',
            true,
            'tr',
          );
          return result;
        }),
        catchError((err) =>
          throwError(
            () => new Error(err.message ?? 'Failed to learn blueprint'),
          ),
        ),
      );
  }

  removeItemFromQueue(queueItem: {
    id: string;
  }): Observable<RemoveCraftingQueueItemResponse> {
    return this.api.post('Crafting/RemoveCraftingQueueItem', queueItem.id).pipe(
      map((response) => {
        const result =
          this.unwrapResponse<RemoveCraftingQueueItemResponse>(response);
        this.toast.showToast('Removed item from queue', 'success', true, 'tr');
        return result;
      }),

      catchError(() => {
        return throwError(() => new Error('Failed to remove item from queue'));
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

  private unwrapResponse<T>(response: T | ApiResponse<T>): T {
    if (
      response &&
      typeof response === 'object' &&
      'isSuccess' in response &&
      'data' in response
    ) {
      const apiResponse = response as ApiResponse<T>;
      if (!apiResponse.isSuccess) {
        throw new Error(apiResponse.errorMessage ?? 'Request failed');
      }

      if (apiResponse.data == null) {
        throw new Error('Response did not include data');
      }

      return apiResponse.data;
    }

    return response as T;
  }
}
