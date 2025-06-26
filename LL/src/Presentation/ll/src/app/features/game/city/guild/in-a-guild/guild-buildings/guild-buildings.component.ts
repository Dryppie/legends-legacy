import { Component, computed, effect, signal } from '@angular/core';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { NgFor, NgIf } from '@angular/common';
import { BuildingUpgradeView } from '../../../../../../shared/models/guilds/buildings/buildingUpgradeView';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';
import { RegularButtonComponent } from '../../../../../../shared/components/buttons/regular-button/regular-button.component';
import { HumanizeEnumPipe } from '../../../../../../shared/pipes/enums/humanize-enum.pipe';

@Component({
  selector: 'app-guild-buildings',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    NumberFormatPipe,
    RegularButtonComponent,
    HumanizeEnumPipe,
  ],
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
    this.state.upgradeGuildBuilding(current);
  }

  getUpgradeProgress(upgrade: BuildingUpgradeView): number {
    const guildResources = this.guild()?.resources ?? [];
    const cost = upgrade.nextCost ?? {};
    const types = Object.keys(cost);
    if (types.length === 0) return 0;

    let totalRatio = 0;
    for (const type of types) {
      const required = cost[type];
      const available =
        guildResources.find((r) => r.resource === type)?.amount ?? 0;
      totalRatio += Math.min(1, available / required);
    }

    return totalRatio / types.length; // average of the ratios
  }

  readonly canUpgrade = computed(() => {
    if (this.state.loading()) return false;

    const upgrade = this.selected();
    const guildResources = this.guild()?.resources ?? [];
    const cost = upgrade?.nextCost ?? {};

    for (const [type, required] of Object.entries(cost)) {
      const available =
        guildResources.find((r) => r.resource === type)?.amount ?? 0;
      if (available < required) return false;
    }

    return true;
  });

  getGuildResourceAmount(type: string): number {
    return (
      this.guild()?.resources?.find((r) => r.resource === type)?.amount ?? 0
    );
  }

  isSelected(upgrade: BuildingUpgradeView): boolean {
    return this.selected()?.definition.id === upgrade.definition.id;
  }
}
