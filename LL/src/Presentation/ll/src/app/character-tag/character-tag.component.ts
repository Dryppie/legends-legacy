import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-character-tag',
  standalone: true,
  imports: [],
  templateUrl: './character-tag.component.html',
})
export class CharacterTagComponent {
  @Input() characterId!: string;
  @Input() characterName!: string;

  onViewProfile() {
    // this.viewProfile.emit(this.characterId);
  }

  onWhisper() {
    // this.whisper.emit(this.characterName);
  }
}
