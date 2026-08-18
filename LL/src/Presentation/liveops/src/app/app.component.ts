import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LiveOpsApiService } from './liveops-api.service';
import { OperatorContextService } from './operator-context.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html',
})
export class AppComponent implements OnInit {
  authenticationRequired = false;
  authenticationDenied = false;
  loadingSession = true;
  shellError = '';

  constructor(
    private readonly api: LiveOpsApiService,
    readonly operator: OperatorContextService,
    private readonly router: Router,
  ) {}

  async ngOnInit(): Promise<void> {
    this.authenticationDenied = new URLSearchParams(window.location.search)
      .get('authentication') === 'denied';
    try {
      this.operator.session = await this.api.session();
      await this.api.initializeAntiforgery();
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
    const returnUrl = this.router.url.split('?')[0] || '/dashboard';
    window.location.assign(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  }

  async logout(): Promise<void> {
    try {
      await this.api.logout();
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
