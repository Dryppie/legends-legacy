import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  AbilityCatalogV2BehaviorDiagnosticReport,
  AbilityCatalogV2CoverageReport,
  AbilityCatalogV2DiagnosticReport,
} from '../../../../shared/models/diagnostics/ability-catalog-v2-behavior-diagnostics';

@Injectable({
  providedIn: 'root',
})
export class DiagnosticsService {
  constructor(private apiService: ApiService) {}

  public getAbilityCatalogV2(): Observable<AbilityCatalogV2DiagnosticReport> {
    return this.apiService.get('diagnostics/ability-catalog-v2');
  }

  public getAbilityCatalogV2Coverage(): Observable<AbilityCatalogV2CoverageReport> {
    return this.apiService.get('diagnostics/ability-catalog-v2-coverage');
  }

  public getAbilityCatalogV2Behaviors(): Observable<AbilityCatalogV2BehaviorDiagnosticReport> {
    return this.apiService.get('diagnostics/ability-catalog-v2-behaviors');
  }
}
