import { PlayerWorkspaceComponent } from './player-workspace.component';
import { LiveOpsApiService } from '../../liveops-api.service';
import { ItemCatalogEntry, PlayerDetails, CompensationEquipmentOptions } from '../../liveops.models';

describe('PlayerWorkspaceComponent Equipment progression compensation', () => {
  const sword = { id: 'shortsword', name: 'Shortsword', itemType: 'Equipment' } as ItemCatalogEntry;
  const choices: CompensationEquipmentOptions = {
    usesEquipmentProgression: true, maximumQuantity: 100,
    options: [{ definitionId: 'plain.shortsword', name: 'Shortsword', itemBaseId: 'shortsword', archetypeId: 'plain.shortsword', minimumTier: 1, maximumTier: 1, nativeStyleId: null, compatibleStyleIds: ['blueprint_fury'] }],
  };

  function fixture() {
    const api = jasmine.createSpyObj<LiveOpsApiService>('LiveOpsApiService', ['compensationEquipmentOptions', 'previewGrantItems', 'grantItems']);
    api.compensationEquipmentOptions.and.resolveTo({ isSuccess: true, data: choices, errorMessage: '' });
    const component = new PlayerWorkspaceComponent(api, {} as never, {} as never, { permissions: ['liveops.economy.compensate'] } as never);
    component.selectedPlayer = { player: { characterId: 'owner-1' } } as PlayerDetails;
    component.grantReason = 'CASE-100';
    return { component, api };
  }

  it('previews the explicit definition, rank and style selected for the current recipient', async () => {
    const { component, api } = fixture();
    await component.chooseItem(sword);
    expect(api.compensationEquipmentOptions).toHaveBeenCalledOnceWith('owner-1', 'shortsword');
    expect(component.selectedEquipmentDefinition?.maximumTier).toBe(1);
    component.equipmentRank = 3;
    component.equipmentStyleId = 'blueprint_fury';
    api.previewGrantItems.and.resolveTo({ isSuccess: false, data: null, errorMessage: 'Preview fixture' });
    await component.grantItems();
    expect(api.previewGrantItems).toHaveBeenCalledOnceWith('owner-1', jasmine.objectContaining({
      itemBaseId: 'shortsword', quantity: 1, reason: 'CASE-100',
      equipment: { definitionId: 'plain.shortsword', tier: 1, rank: 3, activeStyleId: 'blueprint_fury' },
    }));
    expect(api.grantItems).not.toHaveBeenCalled();
  });

  it('blocks equipment when options fail and discards late options after switching items', async () => {
    const { component, api } = fixture();
    api.compensationEquipmentOptions.and.rejectWith(new Error('Unavailable'));
    await component.chooseItem(sword);
    await component.grantItems();
    expect(api.previewGrantItems).not.toHaveBeenCalled();

    let resolve!: (value: Awaited<ReturnType<LiveOpsApiService['compensationEquipmentOptions']>>) => void;
    api.compensationEquipmentOptions.and.returnValue(new Promise(done => { resolve = done; }));
    const pending = component.chooseItem(sword);
    await component.chooseItem({ id: 'item.monster_core.lesser', name: 'Lesser Monster Core', itemType: 'Resource' } as ItemCatalogEntry);
    resolve({ isSuccess: true, data: choices, errorMessage: '' });
    await pending;
    expect(component.equipmentOptions).toBeNull();
    expect(component.loadingEquipmentOptions).toBeFalse();
  });

  it('preserves legacy grants and rejects quantities above the canonical equipment limit', async () => {
    const { component, api } = fixture();
    await component.chooseItem(sword);
    component.grantQuantity = 101;
    await component.grantItems();
    expect(api.previewGrantItems).not.toHaveBeenCalled();
    api.compensationEquipmentOptions.and.resolveTo({ isSuccess: true, data: { usesEquipmentProgression: false, maximumQuantity: 100, options: [] }, errorMessage: '' });
    await component.chooseItem(sword);
    api.previewGrantItems.and.resolveTo({ isSuccess: false, data: null, errorMessage: 'Preview fixture' });
    await component.grantItems();
    expect(api.previewGrantItems).toHaveBeenCalledWith('owner-1', jasmine.objectContaining({ equipment: null }));
  });
});
