import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { ItemComponent } from '../../item/item.component';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import { DungeonPreviewData } from '../../../models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../models/enums/dungeonDifficulty';

@Component({
  selector: 'app-dungeon-card',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, RegularButtonComponent, ItemComponent],
  templateUrl: './dungeon-card.component.html',
})
export class DungeonCardComponent {
  @Input({ required: true }) previewData!: DungeonPreviewData;

  @Input() height = 176;
  @Input() cornerSize = 32;

  @Output() backEvent = new EventEmitter<void>();

  dungeonDifficulty = DungeonDifficulty;
  showPreview = signal(false);
  difficulty = signal<DungeonDifficulty>(DungeonDifficulty.Normal);

  constructor(private readonly dungeonState: DungeonStateService) {}

  startDungeon() {
    this.dungeonState.startDungeon(this.previewData.id, this.difficulty());
  }

  togglePreview() {
    this.showPreview.set(!this.showPreview());
  }

  selectDifficulty(d: DungeonDifficulty) {
    if (this.previewData.unlockedDifficulties.includes(d)) {
      this.difficulty.set(d);
    }
  }

  back() {
    this.backEvent.emit();
  }
}
