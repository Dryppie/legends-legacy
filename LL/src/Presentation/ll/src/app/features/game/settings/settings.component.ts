import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserInfoDto } from '../../../shared/models/Dtos/userInfoDto';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { CharacterDto } from '../../../shared/models/Dtos/characterDto';
import { SignupComponent } from '../../public/landing/signup/signup.component';
import { GoogleAuthService } from '../../../core/services/api/auth/google-auth.service';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { FormsModule } from '@angular/forms';
import { RegularButtonComponent } from '../../../shared/components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, SignupComponent, FormsModule, RegularButtonComponent],
  templateUrl: './settings.component.html',
})
export class SettingsComponent {
  userInfo: UserInfoDto | null = null; // Initialize it to null first
  character: CharacterDto | null = null; // Initialize it to null first

  disableLoginLink: boolean = false;

  showNameModal = false;
  newCharacterName = '';

  readonly currentCharacter;

  constructor(
    private authService: AuthService,
    private googleService: GoogleAuthService,
    private readonly characterService: CharacterService,
  ) {
    this.currentCharacter = this.authService.currentCharacter;
  }

  version = '1.0.0'; // or pull from environment

  ngOnInit() {
    this.authService.getUserInfo().subscribe((userInfo) => {
      this.userInfo = userInfo;
    });
  }

  logout() {
    this.authService.logout();
  }

  convertToRegistered() {
    this.setPanel('convertAccount');
  }

  editName() {
    this.newCharacterName = this.currentCharacter()?.name ?? '';
    this.showNameModal = true;
  }

  submitNameChange() {
    if (!this.newCharacterName.trim()) return;

    this.characterService
      .renameCharacter(this.newCharacterName)
      .subscribe(() => {
        this.showNameModal = false;
        // Optionally update local state
        this.character!.name = this.newCharacterName;
      });
  }

  closeNameModal() {
    this.showNameModal = false;
  }

  bindGmail() {
    this.googleService.prompt();
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
