import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import { EssenceCatalogReport } from '../../../../shared/models/essences/essence-catalog';

@Injectable({
  providedIn: 'root',
})
export class EssenceCatalogService {
  constructor(private apiService: ApiService) {}

  public getCatalog(): Observable<EssenceCatalogReport> {
    return this.apiService.get('essencecatalog');
  }
}
