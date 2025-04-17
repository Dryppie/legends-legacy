import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserInfoDto } from '../../../shared/models/Dtos/userInfoDto';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { CharacterDto } from '../../../shared/models/Dtos/characterDto';
 
@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent {
  userInfo: UserInfoDto | null = null; // Initialize it to null first
  character: CharacterDto | null = null; // Initialize it to null first

  constructor(
    private authService : AuthService,
  ) {}

  version = '1.0.0'; // or pull from environment

  ngOnInit() {
  this.authService.getUserInfo().subscribe(userInfo => {
    this.userInfo = userInfo;
    console.log(this.userInfo);
  });

  this.authService.currentCharacter$.subscribe(character => {
    this.character = character;
    console.log(this.character);
  })
}

  logout() {
    this.authService.logout();
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
