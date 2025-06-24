import { Component, effect, signal } from '@angular/core';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { NgFor, NgIf } from '@angular/common';
import { BuildingUpgradeView } from '../../../../../../shared/models/guilds/buildings/buildingUpgradeView';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-guild-buildings',
  standalone: true,
  imports: [NgIf, NgFor, NumberFormatPipe],
  templateUrl: './guild-buildings.component.html',
})
export class GuildBuildingsComponent {
  readonly upgrades;
  readonly guild;

  readonly selected = signal<BuildingUpgradeView | null>(null);

  constructor(private readonly state: GuildStateService) {
    this.upgrades = this.state.upgrades;
    this.guild = this.state.guild;

    effect(
      () => {
        const list = this.upgrades();
        if (list.length > 0 && !this.selected()) {
          this.selected.set(list[0]);
        }
      },
      { allowSignalWrites: true },
    );
  }

  select(upgrade: BuildingUpgradeView): void {
    this.selected.set(upgrade);
  }

  upgradeSelected(): void {
    const current = this.selected();
    if (!current?.definition?.id || !current.nextCost) return;
    console.log(current);
    // this.guildService.upgradeGuildBuilding(current.definition.id).subscribe({
    //   next: () => this.state.refreshUpgrades(),
    // });
  }

  isSelected(upgrade: BuildingUpgradeView): boolean {
    return this.selected()?.definition.id === upgrade.definition.id;
  }
}
