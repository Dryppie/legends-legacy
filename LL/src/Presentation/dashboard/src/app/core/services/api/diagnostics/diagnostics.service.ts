import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../api.service';
import {
  AbilityCatalogBehaviorDiagnosticReport,
  AbilityCatalogCoverageReport,
  AbilityCatalogDiagnosticReport,
  AbilityBalanceSimulationReport,
  AbilityBalanceSimulationRequest,
  RegionOneContentDiagnosticReport,
} from '../../../../shared/models/diagnostics/ability-catalog-diagnostics';
import {
  DungeonSimulationOptions,
  DungeonSimulationReport,
  DungeonSimulationRequest,
  DungeonSimulationCharacter,
  CombatRatingBreakdown,
} from '../../../../shared/models/diagnostics/dungeon-simulation';

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

  public getDungeonSimulationOptions(): Observable<DungeonSimulationOptions> {
    return this.apiService.get('diagnostics/dungeon-simulation-options');
  }

  public runDungeonSimulation(
    request: DungeonSimulationRequest,
  ): Observable<DungeonSimulationReport> {
    return this.apiService.post('diagnostics/dungeon-simulation', request);
  }

  public getDungeonSimulationCombatRating(
    character: DungeonSimulationCharacter,
  ): Observable<CombatRatingBreakdown> {
    return this.apiService.post(
      'diagnostics/dungeon-simulation-combat-rating',
      character,
    );
  }
}
