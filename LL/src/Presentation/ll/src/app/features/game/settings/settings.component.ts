import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserInfoDto } from '../../../shared/models/Dtos/userInfoDto';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { CharacterDto } from '../../../shared/models/Dtos/characterDto';
import { SignupComponent } from '../../public/landing/signup/signup.component';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { environment } from '../../../../environments/environment';
import { emailValidator } from '../../../shared/validators/email-validator';
import { passwordValidator } from '../../../shared/validators/password-validator';
import { passwordMatchValidator } from '../../../shared/validators/password-match-validator';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, SignupComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css',
})
export class SettingsComponent {
  userInfo: UserInfoDto | null = null; // Initialize it to null first
  character: CharacterDto | null = null; // Initialize it to null first

  disableLoginLink: boolean = false;

  constructor(private authService: AuthService) {}

  version = '1.0.0'; // or pull from environment

  ngOnInit() {
    this.authService.getUserInfo().subscribe((userInfo) => {
      this.userInfo = userInfo;
    });

    this.authService.currentCharacter$.subscribe((character) => {
      this.character = character;
    });
  }

  logout() {
    this.authService.logout();
  }

  convertToRegistered() {
    this.setPanel('convertAccount');
  }

  changeCredentials() {}

  deleteAccount() {}

  sendFeedback() {}

  reportBug() {}

  viewPatchNotes() {
    this.setPanel('patchNotes');
  }

  viewCredits() {}

  activePanel: string | null = null;

  setPanel(panel: string) {
    this.activePanel = panel;
  }
}
