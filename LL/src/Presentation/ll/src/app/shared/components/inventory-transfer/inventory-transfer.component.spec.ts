import { signal, SimpleChange } from '@angular/core';
import { fakeAsync, tick } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { InventoryStateService } from '../../../core/services/api/inventory/inventory-state.service';
import { InventoryService } from '../../../core/services/api/inventory/inventory.service';
import { InventoryTransferComponent } from './inventory-transfer.component';

describe('InventoryTransferComponent', () => {
  it('does not refresh inventory when opening the transfer form', () => {
    const inventoryState = jasmine.createSpyObj<InventoryStateService>(
      'InventoryStateService',
      ['decrementItem', 'load'],
      { loading: signal(false).asReadonly() },
    );
    const component = new InventoryTransferComponent(
      jasmine.createSpyObj<InventoryService>('InventoryService', [
        'transferItem',
      ]),
      inventoryState,
      jasmine.createSpyObj<CharacterService>('CharacterService', [
        'suggestCharacterNames',
      ]),
    );
    component.inventoryItem = item(83);

    component.openForm();

    expect(component.isFormOpen()).toBeTrue();
    expect(inventoryState.load).not.toHaveBeenCalled();
    component.ngOnDestroy();
  });

  it('refreshes and clamps stale stock after the server rejects a transfer', () => {
    const inventoryService = jasmine.createSpyObj<InventoryService>(
      'InventoryService',
      ['transferItem'],
    );
    inventoryService.transferItem.and.returnValue(
      throwError(() => ({
        errorMessage: 'You do not have enough of this item.',
      })),
    );
    const inventoryState = jasmine.createSpyObj<InventoryStateService>(
      'InventoryStateService',
      ['decrementItem', 'load'],
      { loading: signal(false).asReadonly() },
    );
    const component = new InventoryTransferComponent(
      inventoryService,
      inventoryState,
      jasmine.createSpyObj<CharacterService>('CharacterService', [
        'suggestCharacterNames',
      ]),
    );
    const stale = item(83);
    component.inventoryItem = stale;
    component.isFormOpen.set(true);
    component.recipientName = 'Tinybones';
    component.hasSelectedRecipient.set(true);
    component.quantity = 83;

    component.transfer();

    expect(inventoryState.load).toHaveBeenCalledOnceWith(true);
    expect(component.error()).toBe('You do not have enough of this item.');

    const current = item(56);
    component.inventoryItem = current;
    component.ngOnChanges({
      inventoryItem: new SimpleChange(stale, current, false),
    });

    expect(component.isFormOpen()).toBeTrue();
    expect(component.quantity).toBe(56);
    component.ngOnDestroy();
  });

  it('positions recipient suggestions above the text field by default', () => {
    const component = new InventoryTransferComponent(
      jasmine.createSpyObj<InventoryService>('InventoryService', [
        'transferItem',
      ]),
      jasmine.createSpyObj<InventoryStateService>('InventoryStateService', [
        'decrementItem',
      ]),
      jasmine.createSpyObj<CharacterService>('CharacterService', [
        'suggestCharacterNames',
      ]),
    );

    expect(component.recipientSuggestionPositions[0]).toEqual(
      jasmine.objectContaining({
        originY: 'top',
        overlayY: 'bottom',
        offsetY: -4,
      }),
    );
    component.ngOnDestroy();
  });

  it('loads recipient suggestions after typing two characters', fakeAsync(() => {
    const characterService = jasmine.createSpyObj<CharacterService>(
      'CharacterService',
      ['suggestCharacterNames'],
    );
    characterService.suggestCharacterNames.and.returnValue(
      of(['Ember', 'Ember Knight']),
    );
    const component = new InventoryTransferComponent(
      jasmine.createSpyObj<InventoryService>('InventoryService', [
        'transferItem',
      ]),
      jasmine.createSpyObj<InventoryStateService>('InventoryStateService', [
        'decrementItem',
      ]),
      characterService,
    );

    component.onRecipientNameChange('em');
    tick(200);

    expect(characterService.suggestCharacterNames).toHaveBeenCalledOnceWith(
      'em',
    );
    expect(component.recipientSuggestions()).toEqual(['Ember', 'Ember Knight']);
    component.ngOnDestroy();
  }));

  it('fills the recipient name from a selected suggestion', () => {
    const component = new InventoryTransferComponent(
      jasmine.createSpyObj<InventoryService>('InventoryService', [
        'transferItem',
      ]),
      jasmine.createSpyObj<InventoryStateService>('InventoryStateService', [
        'decrementItem',
      ]),
      jasmine.createSpyObj<CharacterService>('CharacterService', [
        'suggestCharacterNames',
      ]),
    );
    const event = jasmine.createSpyObj<Event>('Event', ['preventDefault']);

    component.selectRecipient(event, 'Ember Knight');

    expect(event.preventDefault).toHaveBeenCalled();
    expect(component.recipientName).toBe('Ember Knight');
    expect(component.hasSelectedRecipient()).toBeTrue();
    expect(component.showRecipientSuggestionPanel()).toBeFalse();
    component.ngOnDestroy();
  });
});

function item(
  quantity: number,
): import('../../models/inventoryItem').InventoryItem {
  return {
    id: 'thick-hide-row',
    quantity,
    itemInstance: {
      id: 'thick-hide-instance',
      itemBase: {
        id: 'thick_hide',
        name: 'Thick Hide',
        description: '',
        rarity: 'Common' as never,
        itemType: 'Resource' as never,
        stackable: true,
      },
    },
  };
}
