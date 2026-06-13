import { Component } from '@angular/core';
import { NgIf } from '@angular/common';
import { AppUpdateService } from '../../../core/services/client-side/app-update/app-update.service';

@Component({
  selector: 'app-update-popup',
  standalone: true,
  imports: [NgIf],
  templateUrl: './app-update-popup.component.html',
})
export class AppUpdatePopupComponent {
  get updateAvailable() {
    return this.updates.updateAvailable;
  }

  constructor(private readonly updates: AppUpdateService) {}

  refresh(): void {
    this.updates.refresh();
  }
}
