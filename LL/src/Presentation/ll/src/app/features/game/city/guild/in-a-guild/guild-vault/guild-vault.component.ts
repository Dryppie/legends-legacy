import { Component } from '@angular/core';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { GuildResourceType } from '../../../../../../shared/models/Dtos/guild/guildResourceType';
import { HumanizeEnumPipe } from '../../../../../../shared/pipes/enums/humanize-enum.pipe';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { InventoryStateService } from '../../../../../../core/services/api/inventory/inventory-state.service';
import { CharacterStateService } from '../../../../../../core/services/api/character/character-state.service';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-guild-vault',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    NgClass,
    HumanizeEnumPipe,
    RegularButtonComponent,
    NumberFormatPipe,
  ],
  templateUrl: './guild-vault.component.html',
})
export class GuildVaultComponent {
  readonly guild;
  readonly items;
  readonly character;

  readonly allResourceTypes = Object.values(GuildResourceType);

  donationForm: Record<GuildResourceType, number> = Object.fromEntries(
    this.allResourceTypes.map((type) => [type, 0]),
  ) as Record<GuildResourceType, number>;

  constructor(
    private readonly guildState: GuildStateService,
    private readonly inventoryState: InventoryStateService,
    private readonly characterState: CharacterStateService,
  ) {
    this.guild = this.guildState.guild;
    this.items = this.inventoryState.items;
    this.character = this.characterState.currentCharacter;
  }

  get allResources(): { type: GuildResourceType; amount: number }[] {
    const actualResources = this.guild()?.resources ?? [];

    const resourceMap = new Map<GuildResourceType, number>(
      actualResources.map((r) => [r.resource, r.amount]),
    );

    return Object.values(GuildResourceType).map((type) => ({
      type,
      amount: resourceMap.get(type) ?? 0,
    }));
  }

  disabled(): boolean {
    const available = this.availableAmounts;

    for (const [type, amount] of Object.entries(this.donationForm)) {
      const typed = type as GuildResourceType;
      if (amount > 0 && amount > available[typed]) {
        return true;
      }
    }

    return Object.values(this.donationForm).every((amount) => amount <= 0);
  }

  donationTotal(): number {
    return Object.values(this.donationForm).reduce(
      (total, amount) => total + amount,
      0,
    );
  }

  donationExceedsAvailable(type: GuildResourceType): boolean {
    return this.donationForm[type] > this.availableAmounts[type];
  }

  donate(): void {
    if (this.disabled()) return;

    const donations = Object.entries(this.donationForm)
      .filter(([_, amount]) => amount > 0)
      .map(([type, amount]) => ({
        type: type as GuildResourceType,
        amount,
      }));

    if (donations.length === 0) {
      console.warn('No donations entered');
      return;
    }

    this.donationForm = Object.fromEntries(
      this.allResourceTypes.map((type) => [type, 0]),
    ) as Record<GuildResourceType, number>;

    this.guildState.donate(donations);
  }

  get availableAmounts(): Record<GuildResourceType, number> {
    const result: Partial<Record<GuildResourceType, number>> = {};

    const items = this.items() ?? [];
    const character = this.character();

    if (items) {
      result[GuildResourceType.TemperedScrap] =
        items.find((i) => i.itemInstance.itemBase.name === 'Tempered Scrap')
          ?.quantity ?? 0;
      result[GuildResourceType.SoulDust] =
        items.find((i) => i.itemInstance.itemBase.name === 'Soul Dust')
          ?.quantity ?? 0;
    }

    // Character-based resources
    if (character) {
      result[GuildResourceType.Cinders] = character.cinders ?? 0;
      result[GuildResourceType.Soulstones] = character.soulstones ?? 0;
    }

    // Fill in any missing resource types with 0
    for (const type of this.allResourceTypes) {
      result[type] = result[type] ?? 0;
    }

    return result as Record<GuildResourceType, number>;
  }

  formatNumber(value: number): string {
    return value.toLocaleString(); // default 'en-US', can customize
  }

  onFormattedInput(event: Event, type: GuildResourceType): void {
    const input = (event.target as HTMLInputElement).value;

    // Remove commas or non-digit characters
    const raw = input.replace(/[^0-9]/g, '');
    const parsed = parseInt(raw, 10);

    this.donationForm[type] = isNaN(parsed) ? 0 : parsed;
  }
}
