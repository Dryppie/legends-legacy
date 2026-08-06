import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  AbilityCatalogBehaviorDiagnosticReport,
  AbilityCatalogCoverageReport,
  AbilityCatalogDiagnosticReport,
  AbilityBalanceSimulationReport,
  AbilityBalanceSimulationRequest,
  AbilityBalanceAuditReport,
  AbilityBalanceAuditRequest,
  RegionOneContentDiagnosticReport,
} from '../../../../shared/models/diagnostics/ability-catalog-diagnostics';
import {
  DungeonSimulationOptions,
  DungeonSimulationReport,
  DungeonSimulationRequest,
} from '../../../../shared/models/diagnostics/dungeon-simulation';
import {
  AreaSimulationOptions,
  AreaSimulationReport,
  AreaSimulationRequest,
  RegionAreaBalanceReport,
  RegionAreaBalanceRequest,
} from '../../../../shared/models/diagnostics/area-simulation';

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
    return this.apiService.post(
      'diagnostics/ability-balance-simulation',
      request,
    );
  }

  public runAbilityBalanceAudit(
    request: AbilityBalanceAuditRequest,
  ): Observable<AbilityBalanceAuditReport> {
    return this.apiService.post('diagnostics/ability-balance-audit', request);
  }

  public getDungeonSimulationOptions(): Observable<DungeonSimulationOptions> {
    return this.apiService.get('diagnostics/dungeon-simulation-options');
  }

  public runDungeonSimulation(
    request: DungeonSimulationRequest,
  ): Observable<DungeonSimulationReport> {
    return this.apiService.post('diagnostics/dungeon-simulation', request);
  }

  public getAreaSimulationOptions(): Observable<AreaSimulationOptions> {
    return this.apiService.get('diagnostics/area-simulation-options');
  }

  public runAreaSimulation(
    request: AreaSimulationRequest,
  ): Observable<AreaSimulationReport> {
    return this.apiService.post('diagnostics/area-simulation', request);
  }

  public analyzeRegionAreaBalance(
    request: RegionAreaBalanceRequest,
  ): Observable<RegionAreaBalanceReport> {
    return this.apiService.post('diagnostics/region-area-balance', request);
  }

}
