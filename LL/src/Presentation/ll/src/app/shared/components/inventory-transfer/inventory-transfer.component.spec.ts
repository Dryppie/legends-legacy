import { fakeAsync, tick } from '@angular/core/testing';
import { of } from 'rxjs';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { InventoryStateService } from '../../../core/services/api/inventory/inventory-state.service';
import { InventoryService } from '../../../core/services/api/inventory/inventory.service';
import { InventoryTransferComponent } from './inventory-transfer.component';

describe('InventoryTransferComponent', () => {
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
