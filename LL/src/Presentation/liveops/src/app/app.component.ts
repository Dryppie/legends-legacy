import { HttpErrorResponse } from '@angular/common/http';
import { Component, HostListener, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LiveOpsApiService } from './liveops-api.service';
import { OperatorContextService } from './operator-context.service';
import { inject } from '@angular/core';
import { OperationJournalService } from './operation-journal.service';
import { WorkspaceStateService } from './workspace-state.service';
import { OperatorDraftService } from './operator-draft.service';
import { GlobalSearchComponent } from './shared/global-search.component';
import { DraftStatusComponent } from './shared/draft-status.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, GlobalSearchComponent, DraftStatusComponent],
  templateUrl: './app.component.html',
})
export class AppComponent implements OnInit {
  readonly journal = inject(OperationJournalService);
  readonly workspace = inject(WorkspaceStateService);
  readonly drafts = inject(OperatorDraftService);
  authenticationRequired = false;
  authenticationDenied = false;
  loadingSession = true;
  shellError = '';
  unsavedLogout = false;

  constructor(
    private readonly api: LiveOpsApiService,
    readonly operator: OperatorContextService,
    private readonly router: Router,
  ) {}

  async ngOnInit(): Promise<void> {
    this.loadingSession = true;
    this.shellError = '';
    this.authenticationDenied = new URLSearchParams(window.location.search)
      .get('authentication') === 'denied';
    try {
      const session = await this.api.session();
      await this.api.initializeAntiforgery();
      this.operator.session = session;
      this.operator.sessionExpired = false;
      this.authenticationRequired = false;
      this.journal.initialize(this.operator.session.subject, this.operator.session.environment);
      this.drafts.initialize(session.subject, session.environment);
      await this.workspace.initialize(this.drafts);
      void this.checkOperations();
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        this.authenticationRequired = true;
      } else {
        this.shellError = this.errorMessage(error);
      }
    } finally {
      this.loadingSession = false;
    }
  }

  login(): void {
    const returnUrl = this.router.url || '/dashboard';
    window.location.assign(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  }

  checkOperations(): Promise<void> { return this.operator.session ? this.journal.reconcile(this.api, this.operator.session.subject) : Promise.resolve(); }
  @HostListener('window:beforeunload', ['$event'])
  protectUnsavedDrafts(event: BeforeUnloadEvent): void {
    if (this.drafts.unsaved.length) { event.preventDefault(); event.returnValue = ''; }
  }
  async restoreWorkspace(): Promise<void> { await this.workspace.initialize(this.drafts); }

  async logout(discardUnsaved = false): Promise<void> {
    try {
      if (!discardUnsaved && !await this.drafts.flush()) { this.unsavedLogout = true; this.shellError = 'Some private drafts are not saved. Save them before signing out, or explicitly sign out without the unsaved text.'; return; }
      await this.api.logout();
      this.workspace.clear();
      this.drafts.clear();
      window.location.assign('/');
    } catch (error) {
      this.shellError = this.errorMessage(error);
    }
  }

  private errorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      return error.error?.errorMessage ?? error.error?.message ?? error.message;
    }
    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }
}
