import {
  AfterViewInit,
  Component,
  OnInit,
  ViewChild,
} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './shared/components/toast/toast.component';
import { ModalContainerComponent } from './shared/components/modal-container/modal-container.component';
import { SessionSummaryPopupComponent } from './shared/components/session-summary-popup/session-summary-popup.component';
import { GoogleAuthService } from './core/services/api/auth/google-auth.service';
import { ToastService } from './core/services/client-side/components/toast/toast.service';
import { AppUpdatePopupComponent } from './shared/components/app-update-popup/app-update-popup.component';
import { AppUpdateService } from './core/services/client-side/app-update/app-update.service';
import { FirstPartyTourOverlayComponent } from './shared/components/first-party-tour-overlay/first-party-tour-overlay.component';

@Component({
    selector: 'app-root',
    imports: [
        RouterOutlet,
        ToastComponent,
        ModalContainerComponent,
        SessionSummaryPopupComponent,
        AppUpdatePopupComponent,
        FirstPartyTourOverlayComponent,
    ],
    templateUrl: './app.component.html'
})
export class AppComponent implements OnInit, AfterViewInit {
  title = 'll';

  @ViewChild('toast', { static: true }) toastComponent!: ToastComponent;

  constructor(
    private readonly googleAuth: GoogleAuthService,
    private readonly toastService: ToastService,
    private readonly appUpdate: AppUpdateService,
  ) {}

  ngOnInit(): void {
    this.googleAuth.init(); // load GSI script once
    this.appUpdate.start();
  }

  ngAfterViewInit(): void {
    this.toastService.register(this.toastComponent); // hook up toast outlet
  }
}
