import { Component, Input } from '@angular/core';
import { ClickPopoverComponent } from '../../custom-components/popovers/click-popover/click-popover.component';
import { Router } from '@angular/router';
import { ChatService } from '../../../../core/services/ll-chat/chat-service/chat.service';

@Component({
  selector: 'app-character-tag',
  standalone: true,
  imports: [ClickPopoverComponent],
  templateUrl: './character-tag.component.html',
})
export class CharacterTagComponent {
  @Input() id!: string;
  @Input() name!: string;

  isMenuOpen = false;

  constructor(
    private readonly router: Router,
    private readonly chat: ChatService,
  ) {}

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
