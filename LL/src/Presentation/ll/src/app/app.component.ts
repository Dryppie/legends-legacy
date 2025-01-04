import { Component, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastComponent } from './shared/components/toast/toast.component';
import { ToastService } from './core/services/toast/toast.service';
import { CharacterActionsService } from './core/services/character-actions/character-actions.service';
import { ModalContainerComponent } from './shared/components/modal-container/modal-container.component';

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
    private toastService: ToastService,
    private characterActionsService: CharacterActionsService,
  ) {}

  ngOnInit(): void {
    this.characterActionsService.init();
  }

  ngOnDestroy(): void {}

  ngAfterViewInit() {
    this.toastService.register(this.toastComponent);
  }
}
