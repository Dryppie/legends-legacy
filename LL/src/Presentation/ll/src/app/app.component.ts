import {
  AfterViewInit,
  Component,
  effect,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './shared/components/toast/toast.component';
import { ModalContainerComponent } from './shared/components/modal-container/modal-container.component';
import { AuthService } from './core/services/api/auth/auth.service';
import { SessionSummaryPopupComponent } from './shared/components/session-summary-popup/session-summary-popup.component';
import { GoogleAuthService } from './core/services/api/auth/google-auth.service';
import { CharacterActionsStateService } from './core/services/api/character-actions/character-actions.state.service';
import { ToastService } from './core/services/client-side/components/toast/toast.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    ToastComponent,
    ModalContainerComponent,
    SessionSummaryPopupComponent,
  ],
  templateUrl: './app.component.html',
})
export class AppComponent implements OnInit, AfterViewInit {
  title = 'll';

  @ViewChild('toast', { static: true }) toastComponent!: ToastComponent;

  /** Prevents calling `characterActionsService.init()` more than once */
  private readonly initDone = signal(false);

  constructor(
    private readonly authService: AuthService, // now exposes `isAuthenticated()` signal
    private readonly googleAuth: GoogleAuthService,
    private readonly state: CharacterActionsStateService,
    private readonly toastService: ToastService,
  ) {
    /* ───────────────────────────────────────────────
     *  Side-effect : run when `isAuthenticated` flips
     * ─────────────────────────────────────────────── */
    effect(
      () => {
        const loggedIn = this.authService.isAuthenticated(); // read only
        if (loggedIn && !this.initDone()) {
          this.state.init();
          this.initDone.set(true); // <-- write
        }
      },
      { allowSignalWrites: true },
    );
  }

  /* ─────────────────────────────────────────────── */
  ngOnInit(): void {
    this.googleAuth.init(); // load GSI script once
  }

  ngAfterViewInit(): void {
    this.toastService.register(this.toastComponent); // hook up toast outlet
  }
}
