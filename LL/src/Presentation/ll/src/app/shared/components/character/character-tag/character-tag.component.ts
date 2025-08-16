import { Component, Input } from '@angular/core';
import { ClickPopoverComponent } from '../../custom-components/popovers/click-popover/click-popover.component';

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
