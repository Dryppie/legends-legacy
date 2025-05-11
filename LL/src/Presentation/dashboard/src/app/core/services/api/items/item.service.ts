import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { ItemBase } from '../../../../shared/models/item';

@Injectable({
  providedIn: 'root',
})
export class ItemService {
  constructor(private apiService: ApiService) {}

  public getItems(): Observable<ItemBase[]> {
    return this.apiService.get('item');
  }

  public updateItem(item: ItemBase): Observable<ItemBase> {
    console.log(item);
    return this.apiService.post('item/updateItemBase', item);
  }
}
