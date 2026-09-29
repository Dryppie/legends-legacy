import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { OperatorContextService } from '../../operator-context.service';
import { LiveOpsApiService } from '../../liveops-api.service';
import { ServerOperation, ServerOperationPage, ServerOperationStatus } from '../../liveops.models';
import { OperationJournalService } from '../../operation-journal.service';
import { readableName } from '../../shared/admin-presentation';

@Component({ selector: 'app-operations', standalone: true, imports: [CommonModule, FormsModule, RouterLink], templateUrl: './operations.component.html', styles: [`
  .workspace-tools label { display: flex; align-items: center; gap: 8px; }
  .workspace-tools input[type=checkbox] { width: auto; margin: 0; }
  .case-list .case-form { margin: 0; padding: 16px; gap: 8px; min-width: 0; }
  .case-list .workspace-tools { margin: 4px 0 0; }
  .case-list a { display: inline; border: 0; padding: 8px 0; text-decoration: underline; }
  .case-form > p, .case-history p { margin: 0; }
  .case-history { padding: 0; }
  .case-form > span { overflow-wrap: anywhere; }
`] })
export class OperationsComponent implements OnInit, OnDestroy {
  @ViewChild('resultHeading') resultHeading?: ElementRef<HTMLElement>;
  data: ServerOperationPage | null = null; detail: ServerOperationStatus | null = null;
  page = 1; unresolvedOnly = true; loading = false; inspecting = false; error = ''; copyMessage = '';
  readonly label = readableName; private generation = 0; private detailGeneration = 0;
  private subscription?: Subscription;
  constructor(private api: LiveOpsApiService, private journal: OperationJournalService,
    readonly operator: OperatorContextService = new OperatorContextService(), private route?: ActivatedRoute) {}
  get canRepair(): boolean { return this.operator.hasPermission(this.operator.permissions.superadmin); }
  ngOnInit(): void {
    if (!this.route) { void this.load(); return; }
    this.subscription = this.route.queryParamMap.subscribe(params => {
      const id = params.get('operation'); this.page = 1; if (id) this.unresolvedOnly = false;
      void this.load(id ?? undefined);
    });
  }
  ngOnDestroy(): void { this.generation++; this.detailGeneration++; this.subscription?.unsubscribe(); }
  async load(operationId?: string): Promise<void> {
    const generation = ++this.generation; this.detailGeneration++; this.inspecting = false;
    this.data = null; this.detail = null; this.error = ''; this.loading = true;
    try { const response = await this.api.operations(this.page, this.unresolvedOnly); if (generation !== this.generation) return;
      if (!response.isSuccess || !response.data) throw new Error(response.errorMessage); this.data = response.data;
      if (operationId) await this.inspect(operationId);
    } catch { if (generation === this.generation) this.error = 'The operation register is unavailable. Existing recovery references remain unchanged.'; }
    finally { if (generation === this.generation) this.loading = false; }
  }
  async inspect(row: ServerOperation | string): Promise<void> {
    const generation = ++this.detailGeneration; this.detail = null; this.inspecting = true; this.error = ''; this.copyMessage = '';
    try { const response = await this.api.operationStatus(typeof row === 'string' ? row : row.operationId); if (generation !== this.detailGeneration) return;
      if (!response.isSuccess || !response.data) throw new Error(response.errorMessage); this.detail = response.data;
      if (typeof row !== 'string') row.outcome = response.data.operation.outcome;
      this.journal.applyServerOutcome(response.data.operation);
      requestAnimationFrame(() => { if (generation === this.detailGeneration) this.resultHeading?.nativeElement.focus(); });
    } catch { if (generation === this.detailGeneration) this.error = 'The outcome could not be checked. Keep the original reference and retry this read.'; }
    finally { if (generation === this.detailGeneration) this.inspecting = false; }
  }
  async copy(): Promise<void> { try { await navigator.clipboard.writeText(JSON.stringify({ audience: 'Internal diagnostics', checkedAt: new Date().toISOString(), ...this.detail }, null, 2)); this.copyMessage = 'Internal result copied with references and coverage.'; } catch { this.copyMessage = 'Clipboard unavailable.'; } }
}
