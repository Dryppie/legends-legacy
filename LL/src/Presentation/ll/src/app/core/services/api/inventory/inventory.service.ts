import { Injectable } from '@angular/core';
import { ApiService, VersionedMutationResult } from '../../api/api.service';
import { catchError, map, Observable, throwError } from 'rxjs';
import { InventoryDto } from '../../../../shared/models/Dtos/inventoryDto';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

export interface OpenSelectionCrateResponse {
  consumedItemInstanceId: string;
  grantId: string;
  rewards: InventoryItem[];
  inventoryItems: InventoryItem[];
}

export interface TransferInventoryItemResponse {
  itemInstanceId: string;
  recipientName: string;
  quantity: number;
  inventoryItems: InventoryItem[];
}

export interface SetInventoryItemFavoriteResponse {
  itemInstanceId: string;
  isFavorite: boolean;
  inventoryItems: InventoryItem[];
}

export interface MarkInventoryItemSeenResponse {
  itemInstanceId: string;
  inventoryItems: InventoryItem[];
}

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  constructor(private apiService: ApiService) {}

  public getInventory(): Observable<InventoryDto> {
    return this.apiService.get('inventory').pipe(
      map((inventory) => {
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

  openSelectionContainer(
    containerItemInstanceId: string,
    optionId: string,
  ): Observable<VersionedMutationResult<OpenSelectionCrateResponse>> {
    return this.apiService.postVersioned<OpenSelectionCrateResponse>(
      `inventory/items/${containerItemInstanceId}/open-selection-container`,
      { optionId },
      {
        stateSyncScopesHandledByResponse: ['inventory'],
      },
    );
  }

  markItemSeen(
    itemInstanceId: string,
  ): Observable<VersionedMutationResult<MarkInventoryItemSeenResponse>> {
    return this.apiService.postVersioned<MarkInventoryItemSeenResponse>(
      `inventory/items/${itemInstanceId}/seen`,
      {},
      {
        stateSyncScopesHandledByResponse: ['inventory'],
      },
    );
  }

  setItemFavorite(
    itemInstanceId: string,
    isFavorite: boolean,
  ): Observable<VersionedMutationResult<SetInventoryItemFavoriteResponse>> {
    return this.apiService.postVersioned<SetInventoryItemFavoriteResponse>(
      `inventory/items/${itemInstanceId}/favorite`,
      { isFavorite },
      {
        stateSyncScopesHandledByResponse: ['inventory'],
      },
    );
  }

  transferItem(
    itemInstanceId: string,
    recipientName: string,
    quantity: number,
  ): Observable<VersionedMutationResult<TransferInventoryItemResponse>> {
    return this.apiService.postVersioned<TransferInventoryItemResponse>(
      `inventory/items/${itemInstanceId}/transfer`,
      { recipientName, quantity },
      {
        stateSyncScopesHandledByResponse: ['inventory'],
      },
    );
  }
}
