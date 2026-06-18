import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserInfoDto } from '../../../shared/models/Dtos/userInfoDto';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { CharacterDto } from '../../../shared/models/Dtos/characterDto';
import { SignupComponent } from '../../public/landing/signup/signup.component';
import { GoogleAuthService } from '../../../core/services/api/auth/google-auth.service';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { FormsModule } from '@angular/forms';
import { RegularButtonComponent } from '../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { DefaultHeaderComponent } from '../../../shared/components/default-header/default-header.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    SignupComponent,
    FormsModule,
    RegularButtonComponent,
    DefaultHeaderComponent,
  ],
  templateUrl: './settings.component.html',
})
export class SettingsComponent {
  userInfo: UserInfoDto | null = null; // Initialize it to null first
  character: CharacterDto | null = null; // Initialize it to null first

  disableLoginLink: boolean = false;

  showNameModal = false;
  showBindEmailModal = false;
  newCharacterName = '';

  readonly currentCharacter;
  public readonly guild;

  constructor(
    private authService: AuthService,
    private googleService: GoogleAuthService,
    private readonly characterService: CharacterService,
    private readonly guildState: GuildStateService,
  ) {
    this.guild = guildState.guild;
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
    this.showBindEmailModal = true;
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
        if (this.character) {
          this.character.name = this.newCharacterName;
        }
      });
  }

  closeEmailModal() {
    this.showBindEmailModal = false;
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

  experiencePercent(character: CharacterDto): number {
    if (character.experienceUntilNextLevel <= 0) return 100;

    return Math.min(
      100,
      (character.experience / character.experienceUntilNextLevel) * 100,
    );
  }

  accountTypeLabel(): string {
    if (!this.userInfo) return 'Loading';
    return this.userInfo.isRegisteredUser ? 'Registered' : 'Guest';
  }

  accountTypeClass(): string {
    if (!this.userInfo) return 'll-badge-muted';
    return this.userInfo.isRegisteredUser
      ? 'll-badge-success'
      : 'll-badge-warning';
  }

  gmailStatusLabel(): string {
    if (!this.userInfo) return 'Loading';
    return this.userInfo.isGmailBound ? 'Bound' : 'Not bound';
  }

  gmailStatusClass(): string {
    if (!this.userInfo) return 'll-badge-muted';
    return this.userInfo.isGmailBound ? 'll-badge-success' : 'll-badge-muted';
  }

  emailStatusLabel(): string {
    if (!this.userInfo) return 'Loading';
    return this.userInfo.isRegisteredUser ? 'Bound' : 'Not bound';
  }

  emailStatusClass(): string {
    if (!this.userInfo) return 'll-badge-muted';
    return this.userInfo.isRegisteredUser
      ? 'll-badge-success'
      : 'll-badge-warning';
  }
}
