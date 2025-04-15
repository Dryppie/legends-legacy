import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent {
  version = '1.0.0'; // or pull from environment

  logout() {
    console.log('Logging out...');
  }

  convertToRegistered() {
    console.log('Converting guest account...');
  }

  changeCredentials() {
    console.log('Navigating to change credentials...');
  }

  deleteAccount() {
    console.log('Delete account confirmation flow...');
  }

  sendFeedback() {
    console.log('Redirect to feedback form...');
  }

  reportBug() {
    console.log('Open bug report modal...');
  }

  viewPatchNotes() {
    console.log('Navigating to patch notes...');
  }

  viewCredits() {
    console.log('Show credits modal or page...');
  }

  activePanel: string | null = null;

  setPanel(panel: string) {
    this.activePanel = panel;
  }
}
