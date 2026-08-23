import { Injectable, signal } from '@angular/core';
import { HttpParams } from '@angular/common/http';
import { ApiService, VersionedMutationResult } from '../api.service';
import {
  BehaviorSubject,
  catchError,
  map,
  Observable,
  Subject,
  throwError,
} from 'rxjs';
import { CraftingQueueItem } from '../../../../shared/models/profession';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { TemperingQueueMutationResponse } from '../../../../shared/models/Dtos/temperingQueueMutationDto';
import {
  CraftingRecipe,
  CraftItemsRequest,
  CraftItemsResult,
  LearnBlueprintResult,
} from '../../../../shared/models/crafting-v2';
import { ApiResponse } from '../../../../shared/models/response';
import { TemperingOutcomeEntry } from '../../../../shared/models/Dtos/temperingSessionDto';

export type CraftingQueueMoveDirection = 'Up' | 'Down' | 'Top';

export interface MoveCraftingQueueItemResponse {
  currentAction: CharacterActionDto;
}

@Injectable({
  providedIn: 'root',
})
export class CraftingService {
  private readonly queueSubject = new BehaviorSubject<CraftingQueueItem[]>([]);
  private readonly blueprintLearnedSubject =
    new Subject<LearnBlueprintResult>();
  private readonly recentTemperingOutcomesSignal = signal<
    TemperingOutcomeEntry[]
  >([]);
  /** Observable that callers (components, other services) can subscribe to */
  readonly craftingQueue$ = this.queueSubject.asObservable();
  readonly blueprintLearned$ = this.blueprintLearnedSubject.asObservable();
  readonly recentTemperingOutcomes =
    this.recentTemperingOutcomesSignal.asReadonly();

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

  public craftItems(
    request: CraftItemsRequest,
  ): Observable<VersionedMutationResult<CraftItemsResult>> {
    return this.api
      .postVersioned<ApiResponse<CraftItemsResult>>('Crafting/craft', request, {
        stateSyncScopesHandledByResponse: ['inventory'],
      })
      .pipe(
        map((response) => {
          const result = this.unwrapResponse<CraftItemsResult>(response.data);
          const count = result.createdItems.length;
          this.toast.showToast(
            `Crafted ${count} item${count === 1 ? '' : 's'}`,
            'success',
            true,
            'tr',
          );
          return { data: result, domainVersions: response.domainVersions };
        }),
        catchError(() => throwError(() => new Error('Failed to craft items'))),
      );
  }

  public learnBlueprint(
    blueprintItemInstanceId: string,
    recipeId: string,
  ): Observable<VersionedMutationResult<LearnBlueprintResult>> {
    return this.api
      .postVersioned<
        ApiResponse<LearnBlueprintResult>
      >('Crafting/blueprints/learn', { blueprintItemInstanceId, recipeId }, { stateSyncScopesHandledByResponse: ['inventory'] })
      .pipe(
        map((response) => {
          const result = this.unwrapResponse<LearnBlueprintResult>(
            response.data,
          );
          this.blueprintLearnedSubject.next(result);
          this.toast.showToast(
            `Learned ${result.blueprintName} for ${result.recipeName}`,
            'success',
            true,
            'tr',
          );
          return { data: result, domainVersions: response.domainVersions };
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
  }): Observable<VersionedMutationResult<TemperingQueueMutationResponse>> {
    return this.api
      .postVersioned<ApiResponse<TemperingQueueMutationResponse>>(
        'Crafting/RemoveCraftingQueueItem',
        queueItem.id,
        {
          stateSyncScopesHandledByResponse: ['inventory'],
        },
      )
      .pipe(
        map((response) => {
          const result = this.unwrapResponse<TemperingQueueMutationResponse>(
            response.data,
          );
          this.toast.showToast(
            'Removed item from queue',
            'success',
            true,
            'tr',
          );
          return { data: result, domainVersions: response.domainVersions };
        }),

        catchError(() => {
          return throwError(
            () => new Error('Failed to remove item from queue'),
          );
        }),
      );
  }

  cancelTemperingQueue(): Observable<
    VersionedMutationResult<TemperingQueueMutationResponse>
  > {
    return this.api
      .postVersioned<ApiResponse<TemperingQueueMutationResponse>>(
        'Crafting/queue/cancel',
        {},
        {
          stateSyncScopesHandledByResponse: ['inventory'],
        },
      )
      .pipe(
        map((response) => {
          const result = this.unwrapResponse<TemperingQueueMutationResponse>(
            response.data,
          );
          this.toast.showToast(
            'Cancelled the Tempering queue',
            'success',
            true,
            'tr',
          );
          return { data: result, domainVersions: response.domainVersions };
        }),
        catchError((error) =>
          throwError(
            () =>
              new Error(
                error?.message ?? 'Failed to cancel the Tempering queue',
              ),
          ),
        ),
      );
  }

  moveQueueItem(
    queueItemId: string,
    direction: CraftingQueueMoveDirection,
  ): Observable<MoveCraftingQueueItemResponse> {
    return this.api
      .post('Crafting/queue/move', { queueItemId, direction })
      .pipe(
        map((response) =>
          this.unwrapResponse<MoveCraftingQueueItemResponse>(response),
        ),
        catchError((error) =>
          throwError(
            () =>
              new Error(
                error?.message ?? 'Failed to reposition the queue item',
              ),
          ),
        ),
      );
  }

  setQueue(nextQueue: CraftingQueueItem[]): void {
    // Use a defensive copy so callers can keep mutating their own array safely
    this.queueSubject.next([...nextQueue]);
  }

  recordTemperingOutcomes(outcomes: TemperingOutcomeEntry[]): void {
    if (outcomes.length === 0) return;

    const byId = new Map(
      [...this.recentTemperingOutcomesSignal(), ...outcomes].map((outcome) => [
        outcome.id,
        outcome,
      ]),
    );
    this.recentTemperingOutcomesSignal.set(
      [...byId.values()]
        .sort(
          (left, right) =>
            new Date(right.occurredAt).getTime() -
            new Date(left.occurredAt).getTime(),
        )
        .slice(0, 5),
    );
  }

  clearTemperingOutcomes(): void {
    this.recentTemperingOutcomesSignal.set([]);
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
