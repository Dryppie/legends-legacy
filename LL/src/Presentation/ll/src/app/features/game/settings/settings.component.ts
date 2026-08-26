import { Component, effect, HostListener } from '@angular/core';
import { A11yModule } from '@angular/cdk/a11y';
import { CommonModule } from '@angular/common';
import { UserInfoDto } from '../../../shared/models/Dtos/userInfoDto';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { CharacterDto } from '../../../shared/models/Dtos/characterDto';
import { SignupComponent } from '../../public/landing/signup/signup.component';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { FormsModule } from '@angular/forms';
import { RegularButtonComponent } from '../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { DefaultHeaderComponent } from '../../../shared/components/default-header/default-header.component';
import {
  ChatLayout,
  ChatLayoutPreferenceService,
} from '../../../core/services/client-side/chat-layout/chat-layout-preference.service';
import {
  SidebarLayout,
  SidebarLayoutPreferenceService,
} from '../../../core/services/client-side/sidebar-layout/sidebar-layout-preference.service';
import { GoogleSignInButtonComponent } from '../../../shared/components/google-sign-in-button/google-sign-in-button.component';
import {
  ReadingFont,
  ReadingFontSize,
  TypographyPreferenceService,
} from '../../../core/services/client-side/typography/typography-preference.service';

@Component({
  selector: 'app-settings',
  imports: [
    CommonModule,
    SignupComponent,
    FormsModule,
    RegularButtonComponent,
    DefaultHeaderComponent,
    GoogleSignInButtonComponent,
    A11yModule,
  ],
  templateUrl: './settings.component.html',
})
export class SettingsComponent {
  private modalTrigger: HTMLElement | null = null;
  userInfo: UserInfoDto | null = null; // Initialize it to null first
  character: CharacterDto | null = null; // Initialize it to null first

  disableLoginLink: boolean = false;

  showNameModal = false;
  showBindEmailModal = false;
  newCharacterName = '';

  readonly currentCharacter;
  public readonly guild;
  readonly chatLayout;
  readonly sidebarLayout;
  readonly readingFont;
  readonly readingFontSize;
  readonly readingFontOptions: ReadonlyArray<{
    value: ReadingFont;
    label: string;
  }> = [
    {
      value: 'default',
      label: 'Game default',
    },
    {
      value: 'readable',
      label: 'Readable sans',
    },
    {
      value: 'system',
      label: 'System',
    },
  ];
  readonly readingFontSizeOptions: ReadonlyArray<{
    value: ReadingFontSize;
    label: string;
  }> = [
    {
      value: 'default',
      label: '14px',
    },
    {
      value: 'large',
      label: '16px',
    },
    {
      value: 'extra-large',
      label: '18px',
    },
  ];

  constructor(
    private authService: AuthService,
    private readonly characterService: CharacterService,
    private readonly guildState: GuildStateService,
    private readonly chatLayoutPreference: ChatLayoutPreferenceService,
    private readonly sidebarLayoutPreference: SidebarLayoutPreferenceService,
    private readonly typographyPreference: TypographyPreferenceService,
  ) {
    this.guild = guildState.guild;
    this.currentCharacter = this.authService.currentCharacter;
    this.chatLayout = this.chatLayoutPreference.layout;
    this.sidebarLayout = this.sidebarLayoutPreference.layout;
    this.readingFont = this.typographyPreference.readingFont;
    this.readingFontSize = this.typographyPreference.readingFontSize;

    effect(() => {
      this.userInfo = this.authService.userInfo();
    });
  }

  version = '0.5.0'; // or pull from environment

  ngOnInit() {
    this.authService.getUserInfo().subscribe((userInfo) => {
      this.userInfo = userInfo;
    });
  }

  logout() {
    this.authService.logout();
  }

  setChatLayout(layout: ChatLayout): void {
    this.chatLayoutPreference.setLayout(layout);
  }

  setSidebarLayout(layout: SidebarLayout): void {
    this.sidebarLayoutPreference.setLayout(layout);
  }

  setReadingFont(readingFont: ReadingFont): void {
    this.typographyPreference.setReadingFont(readingFont);
  }

  setReadingFontSize(readingFontSize: ReadingFontSize): void {
    this.typographyPreference.setReadingFontSize(readingFontSize);
  }

  resetReadingPreferences(): void {
    this.typographyPreference.resetReadingPreferences();
  }

  convertToRegistered() {
    this.captureModalTrigger();
    this.showBindEmailModal = true;
  }

  editName() {
    this.captureModalTrigger();
    this.newCharacterName = this.currentCharacter()?.name ?? '';
    this.showNameModal = true;
  }

  submitNameChange() {
    if (!this.newCharacterName.trim()) return;

    this.characterService
      .renameCharacter(this.newCharacterName)
      .subscribe(() => {
        this.closeNameModal();
        if (this.character) {
          this.character.name = this.newCharacterName;
        }
      });
  }

  closeEmailModal() {
    this.showBindEmailModal = false;
    this.restoreModalTrigger();
  }

  closeNameModal() {
    this.showNameModal = false;
    this.restoreModalTrigger();
  }

  @HostListener('document:keydown.escape', ['$event'])
  closeOpenModal(event: KeyboardEvent): void {
    if (this.showNameModal) {
      event.preventDefault();
      this.closeNameModal();
    } else if (this.showBindEmailModal) {
      event.preventDefault();
      this.closeEmailModal();
    }
  }

  private captureModalTrigger(): void {
    this.modalTrigger =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
  }

  private restoreModalTrigger(): void {
    const target = this.modalTrigger;
    this.modalTrigger = null;
    queueMicrotask(() => target?.focus());
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
