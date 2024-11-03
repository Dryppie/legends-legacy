import { Component, ViewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './layout/dashboard/sidebar/sidebar.component';
import { DashboardComponent } from './layout/dashboard/dashboard.component';
import { ToastComponent } from './shared/components/toast/toast.component';
import { ToastService } from './core/services/toast/toast.service';
import { CharacterActionsService } from './core/services/character-actions/character-actions.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, DashboardComponent, ToastComponent],
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

  ngOnInit(): void {}

  ngOnDestroy(): void {}

  ngAfterViewInit() {
    this.toastService.register(this.toastComponent);
  }
}
