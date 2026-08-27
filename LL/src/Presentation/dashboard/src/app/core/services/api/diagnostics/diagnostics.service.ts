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
  CombatCharacterProfileGenerationReport,
  CombatCharacterProfileGenerationRequest,
  CombatCharacterProfileCatalogDocument,
  CombatCharacterProfileCatalogValidationReport,
  CombatCharacterProfileBatchGenerationReport,
  CombatCharacterProfileBatchGenerationRequest,
  RegionOneContentDiagnosticReport,
  WorldTowerAuditCampaign,
  WorldTowerAuditCampaignEvidence,
  WorldTowerAuditCampaignOptions,
  WorldTowerCalibrationCertificationOptions,
  WorldTowerCalibrationCertificationReport,
  WorldTowerProfileShadowCalibrationOptions,
  WorldTowerProfileShadowCalibrationReport,
  WorldTowerProfileScenarioRequirement,
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

  public generateCombatCharacterProfiles(
    request: CombatCharacterProfileGenerationRequest,
  ): Observable<CombatCharacterProfileGenerationReport> {
    return this.apiService.post('diagnostics/combat-character-profiles', request);
  }

  public generateCombatCharacterProfileBatch(
    request: CombatCharacterProfileBatchGenerationRequest,
  ): Observable<CombatCharacterProfileBatchGenerationReport> {
    return this.apiService.post('diagnostics/combat-character-profiles/batch', request);
  }

  public getCombatCharacterProfileCatalog(): Observable<CombatCharacterProfileCatalogValidationReport> {
    return this.apiService.get('diagnostics/combat-character-profile-catalog');
  }

  public validateCombatCharacterProfileCatalog(
    catalog: CombatCharacterProfileCatalogDocument,
  ): Observable<CombatCharacterProfileCatalogValidationReport> {
    return this.apiService.post(
      'diagnostics/combat-character-profile-catalog/validate',
      catalog,
    );
  }

  public runWorldTowerProfileShadowCalibration(
    options: WorldTowerProfileShadowCalibrationOptions,
  ): Observable<WorldTowerProfileShadowCalibrationReport> {
    return this.apiService.post(
      'diagnostics/world-tower-profile-shadow-calibration',
      options,
    );
  }

  public runWorldTowerCalibrationCertification(
    options: WorldTowerCalibrationCertificationOptions,
  ): Observable<WorldTowerCalibrationCertificationReport> {
    return this.apiService.post(
      'diagnostics/world-tower-calibration-certification',
      options,
    );
  }

  public getWorldTowerProfileRequirements(
    minimumFloor = 1,
    maximumFloor = 15,
  ): Observable<WorldTowerProfileScenarioRequirement[]> {
    return this.apiService.get(
      `diagnostics/world-tower-profile-requirements?minimumFloor=${minimumFloor}&maximumFloor=${maximumFloor}`,
    );
  }

  public getWorldTowerAuditCampaigns(): Observable<WorldTowerAuditCampaign[]> {
    return this.apiService.get('diagnostics/world-tower-audit-campaigns');
  }

  public getWorldTowerAuditCampaign(id: string): Observable<WorldTowerAuditCampaign> {
    return this.apiService.get(`diagnostics/world-tower-audit-campaigns/${id}`);
  }

  public createWorldTowerAuditCampaign(
    options: WorldTowerAuditCampaignOptions,
  ): Observable<WorldTowerAuditCampaign> {
    return this.apiService.post('diagnostics/world-tower-audit-campaigns', options);
  }

  public cancelWorldTowerAuditCampaign(id: string): Observable<WorldTowerAuditCampaign> {
    return this.apiService.post(
      `diagnostics/world-tower-audit-campaigns/${id}/cancel`,
      {},
    );
  }

  public retryWorldTowerAuditCampaign(id: string): Observable<WorldTowerAuditCampaign> {
    return this.apiService.post(
      `diagnostics/world-tower-audit-campaigns/${id}/retry`,
      {},
    );
  }

  public getWorldTowerAuditCampaignCatalog(
    id: string,
  ): Observable<CombatCharacterProfileCatalogDocument> {
    return this.apiService.get(`diagnostics/world-tower-audit-campaigns/${id}/catalog`);
  }

  public getWorldTowerAuditCampaignEvidence(
    id: string,
  ): Observable<WorldTowerAuditCampaignEvidence> {
    return this.apiService.get(`diagnostics/world-tower-audit-campaigns/${id}/evidence`);
  }
}
