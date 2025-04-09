import { Component, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './shared/components/toast/toast.component';
import { ToastService } from './core/services/client-side/toast/toast.service';
import { CharacterActionsService } from './core/services/api/character-actions/character-actions.service';
import { ModalContainerComponent } from './shared/components/modal-container/modal-container.component';
import { AuthService } from './core/services/api/auth/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastComponent, ModalContainerComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  title = 'll';
  @ViewChild('toast') toastComponent!: ToastComponent;

  constructor(
    private authService: AuthService,
    private characterActionsService: CharacterActionsService,
    private toastService: ToastService,
  ) {}

  ngOnInit(): void {
    this.authService.isAuthenticated$.subscribe((isAuthenticated) => {
      if (isAuthenticated) {
        this.characterActionsService.init();
      }
    });
  }

  ngOnDestroy(): void {}

  ngAfterViewInit() {
    this.toastService.register(this.toastComponent);
  }
}
