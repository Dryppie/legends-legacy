import { signal } from '@angular/core';
import { of } from 'rxjs';
import { CharacterService } from '../../../../../../core/services/api/character/character.service';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { InventoryStateService } from '../../../../../../core/services/api/inventory/inventory-state.service';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';
import { EquipmentInstance } from '../../../../../../shared/models/item';
import { InventoryItem } from '../../../../../../shared/models/inventoryItem';
import { EquipmentOwnership } from '../../../../../../shared/models/equipment-progression';
import { GuildVaultComponent } from './guild-vault.component';

describe('GuildVaultComponent Equipment progression', () => {
  function equipment(id: string, ownership: EquipmentOwnership): InventoryItem {
    return { id, quantity: 1, itemInstance: {
      id, progression: { ownership }, isGuildBorrowed: ownership === 'GuildOwned',
    } as EquipmentInstance };
  }

  function setup(items: InventoryItem[]) {
    const state = jasmine.createSpyObj<GuildStateService>('GuildStateService', ['donateVaultItem', 'withdrawVaultItem']);
    state.donateVaultItem.and.returnValue(of(void 0));
    state.withdrawVaultItem.and.returnValue(of(void 0));
    const equipmentItems = signal(items);
    const inventory = jasmine.createSpyObj<InventoryStateService>('InventoryStateService', ['load'], { equipment: equipmentItems });
    const component = new GuildVaultComponent(
      { currentCharacterId: signal('me') } as unknown as CharacterService, state, inventory,
    );
    component.guild = { vaultItems: [] } as unknown as Guild;
    return { component, state, equipmentItems };
  }

  it('offers only unbound unfavorited personal Equipment progression discoveries', () => {
    const items = [equipment('free', 'UnboundPersonal'), equipment('bound', 'BoundPersonal'),
      equipment('loan', 'GuildOwned'), { ...equipment('favorite', 'UnboundPersonal'), isFavorite: true }];
    const { component } = setup(items);
    expect(component.donateOptions().map(x => x.id)).toEqual(['free']);
  });

  it('requires confirmation of permanent ownership before donating', () => {
    const { component, state } = setup([equipment('free', 'UnboundPersonal')]);
    component.donate('free');
    expect(state.donateVaultItem).not.toHaveBeenCalled();
    expect(component.pendingDonationId).toBe('free');
    component.donate('free');
    expect(state.donateVaultItem).toHaveBeenCalledOnceWith('free');
    expect(component.pendingDonationId).toBeNull();
  });

  it('rechecks eligibility if the item changes before confirmation', () => {
    const item = equipment('free', 'UnboundPersonal');
    const { component, state, equipmentItems } = setup([item]);
    component.donate('free');
    equipmentItems.set([equipment('free', 'BoundPersonal')]);
    component.donate('free');
    expect(state.donateVaultItem).not.toHaveBeenCalled();
  });

  it('prevents Equipment progression withdrawal even when a stale confirmation is present', () => {
    const { component, state } = setup([]);
    component.guild = { vaultItems: [{ id: 'vault', equipment: equipment('loan', 'GuildOwned').itemInstance }] } as unknown as Guild;
    component.requestWithdraw('vault');
    expect(component.pendingWithdrawId).toBeNull();
    component.pendingWithdrawId = 'vault';
    component.withdraw('vault');
    expect(state.withdrawVaultItem).not.toHaveBeenCalled();
  });
});
