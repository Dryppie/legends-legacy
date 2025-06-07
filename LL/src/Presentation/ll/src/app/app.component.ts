import { Component, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './shared/components/toast/toast.component';
import { ToastService } from './core/services/client-side/toast/toast.service';
import { CharacterActionsService } from './core/services/api/character-actions/character-actions.service';
import { ModalContainerComponent } from './shared/components/modal-container/modal-container.component';
import { AuthService } from './core/services/api/auth/auth.service';
import { switchMap, take } from 'rxjs';
import { SessionSummaryPopupComponent } from './shared/components/session-summary-popup/session-summary-popup.component';
import { GoogleAuthService } from './core/services/api/auth/google-auth.service';

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
export class AppComponent {
  title = 'll';
  @ViewChild('toast') toastComponent!: ToastComponent;

  constructor(
    private authService: AuthService,
    private googleAuth: GoogleAuthService,
    private characterActionsService: CharacterActionsService,
    private toastService: ToastService,
  ) {}

  ngOnInit(): void {
    this.googleAuth.init();
    this.authService.isAuthenticated$
      .pipe(
        switchMap((isAuthenticated) => {
          if (isAuthenticated) {
            this.characterActionsService.init();
          }
          return [];
        }),
        take(1),
      )
      .subscribe();
  }

  ngOnDestroy(): void {}

  ngAfterViewInit() {
    this.toastService.register(this.toastComponent);
  }
}
