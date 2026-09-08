import { Component, Input } from '@angular/core';
import { PopoverComponent } from '../../custom-components/popover/popover.component';
import { Router } from '@angular/router';
import { ChatService } from '../../../../core/services/ll-chat/chat-service/chat.service';

@Component({
  selector: 'app-character-tag',
  imports: [PopoverComponent],
  templateUrl: './character-tag.component.html',
  styleUrl: './character-tag.component.scss',
})
export class CharacterTagComponent {
  @Input() id!: string;
  @Input() name!: string;
  @Input() titleDisplayName?: string | null;
  @Input() mention = false;

  isMenuOpen = false;

  constructor(
    private readonly router: Router,
    private readonly chat: ChatService,
  ) {}

  get displayName(): string {
    if (this.mention) return `@${this.name}`;
    const titleDisplayName = this.titleDisplayName?.trim();
    return titleDisplayName || this.name;
  }

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  closeMenu() {
    this.isMenuOpen = false;
  }

  onViewProfile() {
    void this.router.navigate(['/game/character/character-overview'], {
      queryParams: { characterName: this.name },
    });
  }

  onWhisper() {
    this.chat.prepareWhisperToName(this.name);
  }
}
