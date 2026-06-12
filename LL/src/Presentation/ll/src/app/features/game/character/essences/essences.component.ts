import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EssenceStateService } from '../../../../core/services/api/essences/essence-state.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import {
  EssenceLoadoutDto,
  PlayerEssenceDto,
} from '../../../../shared/models/essence-system';
import { EssencesAbsorbComponent } from './essences-absorb/essences-absorb.component';

@Component({
  selector: 'app-essences',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DefaultHeaderComponent,
    EssencesAbsorbComponent,
  ],
  templateUrl: './essences.component.html',
})
export class EssencesComponent implements OnInit {
  constructor(public readonly essenceState: EssenceStateService) {}

  public ngOnInit(): void {
    this.essenceState.refresh();
  }

  public selectPlayerEssence(essence: PlayerEssenceDto): void {
    this.essenceState.selectPlayerEssence(essence);
  }

  public favorite(essence: PlayerEssenceDto): void {
    this.essenceState.favorite(essence);
  }

  public spendDust(essence: PlayerEssenceDto): void {
    this.essenceState.spendDust(essence);
  }

  public ascend(essence: PlayerEssenceDto): void {
    this.essenceState.ascend(essence);
  }

  public evolve(essence: PlayerEssenceDto): void {
    this.essenceState.evolve(essence);
  }

  public selectLoadout(loadout: EssenceLoadoutDto): void {
    this.essenceState.selectLoadout(loadout);
  }
}
