import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  AbilityCatalogBehaviorDiagnosticReport,
  AbilityCatalogCoverageReport,
  AbilityCatalogDiagnosticReport,
} from '../../../../shared/models/diagnostics/ability-catalog-diagnostics';

@Injectable({
  providedIn: 'root',
})
export class DiagnosticsService {
  constructor(private apiService: ApiService) {}

  public getAbilityCatalog(): Observable<AbilityCatalogDiagnosticReport> {
    return this.apiService.get('diagnostics/ability-catalog');
  }

  public getAbilityCatalogCoverage(): Observable<AbilityCatalogCoverageReport> {
    return this.apiService.get('diagnostics/ability-catalog-coverage');
  }

  public getAbilityCatalogBehaviors(): Observable<AbilityCatalogBehaviorDiagnosticReport> {
    return this.apiService.get('diagnostics/ability-catalog-behaviors');
  }
}
