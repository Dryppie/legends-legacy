import { Component, Input } from '@angular/core';
import { ClickPopoverComponent } from '../../custom-components/click-popover/click-popover.component';

@Component({
  selector: 'app-character-tag',
  standalone: true,
  imports: [ClickPopoverComponent],
  templateUrl: './character-tag.component.html',
})
export class CharacterTagComponent {
  @Input() characterId!: string;
  @Input() characterName!: string;

  isMenuOpen = false;

  toggleMenu() {
    this.isMenuOpen = !this.isMenuOpen;
  }

  closeMenu() {
    this.isMenuOpen = false;
  }

  onViewProfile() {
    // this.viewProfile.emit(this.characterId);
  }

  onWhisper() {
    // this.whisper.emit(this.characterName);
  }
}
