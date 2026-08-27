import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  AbilityBalanceAuditReport,
  AbilityBalanceAuditRequest,
  AbilityBalanceSimulationReport,
  AbilityBalanceSimulationRequest,
  AbilityCatalogBehaviorDiagnosticReport,
  AbilityCatalogCoverageReport,
  AbilityCatalogDiagnosticReport,
  RegionOneContentDiagnosticReport,
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

  public getRegionOneContent(): Observable<RegionOneContentDiagnosticReport> {
    return this.apiService.get('diagnostics/region-one-content');
  }

  public runAbilityBalanceSimulation(
    request: AbilityBalanceSimulationRequest,
  ): Observable<AbilityBalanceSimulationReport> {
    return this.apiService.post('diagnostics/ability-balance-simulation', request);
  }

  public runAbilityBalanceAudit(
    request: AbilityBalanceAuditRequest,
  ): Observable<AbilityBalanceAuditReport> {
    return this.apiService.post('diagnostics/ability-balance-audit', request);
  }

}
