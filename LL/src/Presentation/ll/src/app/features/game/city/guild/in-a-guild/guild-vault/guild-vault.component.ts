import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, Input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { CharacterService } from '../../../../../../core/services/api/character/character.service';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { InventoryStateService } from '../../../../../../core/services/api/inventory/inventory-state.service';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { ItemComponent } from '../../../../../../shared/components/item/item.component';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../../../shared/components/custom-components/dropdown/dropdown.component';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';
import { GuildRole } from '../../../../../../shared/models/Dtos/guild/guildRole';
import { GuildRolePermission } from '../../../../../../shared/models/Dtos/guild/guildRolePermission';
import { GuildVaultItem } from '../../../../../../shared/models/Dtos/guild/guildVaultItem';
import { EquipmentInstance } from '../../../../../../shared/models/item';
import { EquipmentType } from '../../../../../../shared/models/enums/equipmentType';
import { ItemQuality } from '../../../../../../shared/models/enums/itemQuality';
import { EquipmentTypePipe } from '../../../../../../shared/pipes/equipment/equipment-type-format/equipment-type.pipe';

type VaultFilter = 'all' | 'available' | 'borrowed';
type VaultSort = 'slot' | 'name' | 'quality' | 'status';
type VaultSlotFilter = EquipmentType | 'all';

@Component({
  selector: 'app-guild-vault',
  imports: [
    NgClass,
    NgFor,
    NgIf,
    FormsModule,
    RegularButtonComponent,
    ItemComponent,
    DropdownComponent,
    EquipmentTypePipe,
  ],
  templateUrl: './guild-vault.component.html',
})
export class GuildVaultComponent {
  @Input({ required: true }) guild!: Guild;
  pendingDonationId: string | null = null;
  busy = false;
  searchTerm = '';
  activeFilter: VaultFilter = 'all';
  selectedSlot: VaultSlotFilter = 'all';
  sortBy: VaultSort = 'slot';
  pendingWithdrawId: string | null = null;
  readonly filters: ReadonlyArray<{ value: VaultFilter; label: string }> = [
    { value: 'all', label: 'All' },
    { value: 'available', label: 'Available' },
    { value: 'borrowed', label: 'Borrowed' },
  ];
  readonly sortOptions: DropdownOption<VaultSort>[] = [
    { value: 'slot', label: 'Slot' },
    { value: 'name', label: 'Name' },
    { value: 'quality', label: 'Quality' },
    { value: 'status', label: 'Status' },
  ];
  readonly slotOptions: DropdownOption<VaultSlotFilter>[] = [
    { value: 'all', label: 'All slots' },
    ...Object.values(EquipmentType).map((slot) => ({
      value: slot,
      label: this.formatEquipmentType(slot),
    })),
  ];

  readonly myCharacterId = computed(
    () => this.characterService.currentCharacterId() ?? '',
  );
  readonly donateOptions = computed(() =>
    this.inventoryState
      .equipment()
      .filter(
        (item) => {
          const equipment = item.itemInstance as EquipmentInstance;
          return !equipment.isGuildBorrowed && (!equipment.progression ||
            (equipment.progression.ownership === 'UnboundPersonal' && !item.isFavorite && !equipment.isFavorite));
        },
      ),
  );

  constructor(
    private readonly characterService: CharacterService,
    private readonly guildState: GuildStateService,
    private readonly inventoryState: InventoryStateService,
  ) {
    this.inventoryState.load();
  }

  get canBorrow(): boolean {
    const me = this.guild.members.find(
      (member) => member.characterId === this.myCharacterId(),
    );
    if (!me) return false;
    return this.permissionFor(me.role)?.canBorrowVault ?? false;
  }

  get canWithdraw(): boolean {
    const me = this.guild.members.find(
      (member) => member.characterId === this.myCharacterId(),
    );
    if (!me) return false;
    if (me.role === GuildRole.Leader) return true;
    return (
      me.role === GuildRole.Officer &&
      (this.permissionFor(me.role)?.canWithdrawVault ?? false)
    );
  }

  get availableCount(): number {
    return this.vaultItems.filter((item) => !item.borrowedByCharacterId).length;
  }

  get borrowedCount(): number {
    return this.vaultItems.length - this.availableCount;
  }

  get vaultItems(): GuildVaultItem[] {
    return Array.isArray(this.guild.vaultItems) ? this.guild.vaultItems : [];
  }

  get visibleVaultItems(): GuildVaultItem[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    const qualityOrder: Record<ItemQuality, number> = {
      [ItemQuality.Crude]: 0,
      [ItemQuality.Standard]: 1,
      [ItemQuality.Fine]: 2,
      [ItemQuality.Exceptional]: 3,
      [ItemQuality.Masterpiece]: 4,
    };

    return this.vaultItems
      .filter((item) => {
        const matchesFilter =
          this.activeFilter === 'all' ||
          (this.activeFilter === 'available' && !item.borrowedByCharacterId) ||
          (this.activeFilter === 'borrowed' && !!item.borrowedByCharacterId);
        const matchesSlot =
          this.selectedSlot === 'all' ||
          item.equipment.equipmentBase.equipmentType === this.selectedSlot;
        if (!matchesFilter || !matchesSlot) return false;
        if (!query) return true;

        return [
          item.equipment.displayName,
          item.equipment.itemBase.name,
          item.equipment.equipmentBase.equipmentType,
          item.equipment.quality,
          item.donatedByName,
          item.borrowedByName,
        ].some((value) => value?.toLocaleLowerCase().includes(query));
      })
      .sort((left, right) => {
        switch (this.sortBy) {
          case 'name':
            return this.equipmentName(left).localeCompare(
              this.equipmentName(right),
            );
          case 'quality':
            return (
              qualityOrder[right.equipment.quality] -
              qualityOrder[left.equipment.quality]
            );
          case 'status':
            return (
              Number(!!left.borrowedByCharacterId) -
              Number(!!right.borrowedByCharacterId)
            );
          case 'slot':
          default:
            return left.equipment.equipmentBase.equipmentType.localeCompare(
              right.equipment.equipmentBase.equipmentType,
            );
        }
      });
  }

  setFilter(filter: VaultFilter): void {
    this.activeFilter = filter;
  }

  setSort(selection: DropdownSelection<unknown>): void {
    this.sortBy = selection.main as VaultSort;
  }

  setSlotFilter(selection: DropdownSelection<unknown>): void {
    this.selectedSlot = selection.main as VaultSlotFilter;
  }

  equipmentName(item: GuildVaultItem): string {
    return item.equipment.displayName || item.equipment.itemBase.name;
  }

  private formatEquipmentType(equipmentType: EquipmentType): string {
    return equipmentType.replace(/([A-Z])/g, ' $1').trim();
  }

  qualityClass(quality: ItemQuality): string {
    switch (quality) {
      case ItemQuality.Fine:
        return 'text-sky-300';
      case ItemQuality.Exceptional:
        return 'text-violet-300';
      case ItemQuality.Masterpiece:
        return 'text-fuchsia-300';
      case ItemQuality.Crude:
        return 'text-zinc-500';
      default:
        return 'text-primary';
    }
  }

  donate(equipmentInstanceId: string): void {
    if (this.busy) return;
    const item = this.donateOptions().find(x => x.itemInstance.id === equipmentInstanceId);
    if (!item) return;
    if ((item.itemInstance as EquipmentInstance).progression && this.pendingDonationId !== equipmentInstanceId) {
      this.pendingDonationId = equipmentInstanceId;
      return;
    }
    this.busy = true;
    this.guildState
      .donateVaultItem(equipmentInstanceId)
      .pipe(finalize(() => (this.busy = false)))
      .subscribe({ next: () => { this.pendingDonationId = null; } });
  }

  borrow(vaultItemId: string): void {
    if (this.busy) return;
    this.busy = true;
    this.guildState
      .borrowVaultItem(vaultItemId)
      .pipe(finalize(() => (this.busy = false)))
      .subscribe();
  }

  returnItem(vaultItemId: string): void {
    if (this.busy) return;
    this.busy = true;
    this.guildState
      .returnVaultItem(vaultItemId)
      .pipe(finalize(() => (this.busy = false)))
      .subscribe();
  }

  requestWithdraw(vaultItemId: string): void {
    if (this.busy || this.vaultItems.find(x => x.id === vaultItemId)?.equipment.progression) return;
    this.pendingWithdrawId = vaultItemId;
  }

  cancelWithdraw(): void {
    this.pendingWithdrawId = null;
  }

  withdraw(vaultItemId: string): void {
    if (this.busy || this.pendingWithdrawId !== vaultItemId || this.vaultItems.find(x => x.id === vaultItemId)?.equipment.progression) return;
    this.busy = true;
    this.guildState
      .withdrawVaultItem(vaultItemId)
      .pipe(finalize(() => (this.busy = false)))
      .subscribe({
        next: () => {
          this.pendingWithdrawId = null;
        },
      });
  }

  private permissionFor(role: GuildRole): GuildRolePermission | undefined {
    const rolePermissions = Array.isArray(this.guild.rolePermissions)
      ? this.guild.rolePermissions
      : [];
    return rolePermissions.find(
      (permission) => permission.role === role,
    );
  }

}
