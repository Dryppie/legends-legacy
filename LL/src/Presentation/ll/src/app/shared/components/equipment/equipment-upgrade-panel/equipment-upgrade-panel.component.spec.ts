import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { EquipmentUpgradePanelComponent } from './equipment-upgrade-panel.component';
import {
  EquipmentService,
  EquipmentUpgradeQuote,
} from '../../../../core/services/api/equipment/equipment.service';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { EquipmentInstance } from '../../../models/item';

describe('Equipment blueprint panel', () => {
  let api: jasmine.SpyObj<EquipmentService>;
  let panel: EquipmentUpgradePanelComponent;

  beforeEach(() => {
    api = jasmine.createSpyObj<EquipmentService>('EquipmentService', [
      'previewUpgrade',
      'getBlueprints',
      'applyVariant',
    ]);
    TestBed.configureTestingModule({
      imports: [EquipmentUpgradePanelComponent],
      providers: [
        { provide: EquipmentService, useValue: api },
        { provide: InventoryStateService, useValue: { load: () => {} } },
        { provide: EquipmentStateService, useValue: { load: () => {} } },
        {
          provide: CharacterStateService,
          useValue: { refreshCurrentCharacter: () => {} },
        },
      ],
    });
    panel = TestBed.createComponent(
      EquipmentUpgradePanelComponent,
    ).componentInstance;
    panel.equipmentInstance = { id: 'sword' } as EquipmentInstance;
    panel.blueprints = ['fury', 'arcane'].map((name) => ({
      styleId: name,
      name,
      itemId: name,
      held: 1,
      isCurrent: false,
      sources: [],
    }));
  });

  it('waits for confirmation and submits the displayed quote only once while pending', () => {
    const quote = {
      canExecute: true,
      request: { kind: 'ApplyVariant', blueprintStyleId: 'fury' },
    } as EquipmentUpgradeQuote;
    api.previewUpgrade.and.returnValue(of(quote));
    api.applyVariant.and.returnValue(new Subject());
    panel.selectBlueprint('fury');
    expect(api.applyVariant).not.toHaveBeenCalled();
    panel.applyVariant();
    panel.applyVariant();
    expect(api.applyVariant).toHaveBeenCalledOnceWith(quote);
  });

  it('ignores a late preview for the previously selected variant', () => {
    const oldPreview = new Subject<EquipmentUpgradeQuote>();
    const currentPreview = new Subject<EquipmentUpgradeQuote>();
    api.previewUpgrade.and.returnValues(oldPreview, currentPreview);
    panel.selectBlueprint('fury');
    panel.selectBlueprint('arcane');
    oldPreview.next({ token: 'old' } as EquipmentUpgradeQuote);
    expect(panel.variantQuote).toBeNull();
    currentPreview.next({ token: 'current' } as EquipmentUpgradeQuote);
    expect(panel.variantQuote?.token).toBe('current');
    panel.selectBlueprint('');
    expect(panel.variantQuote).toBeNull();
    expect(panel.variantLoading).toBeFalse();
  });

  it('maps blueprint choices into the shared dropdown options', () => {
    panel.blueprints[0].isCurrent = true;
    panel.blueprints[1].held = 3;

    expect(panel.blueprintDropdownOptions).toEqual([
      { label: 'fury — 1 held (current)', value: 'fury' },
      { label: 'arcane — 3 held', value: 'arcane' },
    ]);
  });

  it('shows removed bonus attributes when replacing a variant', () => {
    panel.variantQuote = {
      before: { stats: { Power: 100, CritChance: 8 } },
      after: { stats: { Power: 100, MagicPenetration: 9 } },
    } as unknown as EquipmentUpgradeQuote;
    const crit = panel.variantStatChanges.find(
      (x) => x.attributeType === 'CritChance',
    );
    expect(crit?.after).toBe(0);
    expect(crit?.difference).toBe(-8);
    expect(
      panel.variantStatChanges.find((x) => x.attributeType === 'Power')
        ?.difference,
    ).toBe(0);
  });

  it('shows reinforcement changes using equipment-stat precision', () => {
    api.previewUpgrade.and.returnValues(
      of({
        before: { rank: 0, stats: { Power: 8.28 } },
        after: { rank: 1, stats: { Power: 8.61 } },
      } as unknown as EquipmentUpgradeQuote),
      of({} as EquipmentUpgradeQuote),
    );
    api.getBlueprints.and.returnValue(of([]));
    const fixture = TestBed.createComponent(EquipmentUpgradePanelComponent);
    fixture.componentInstance.equipmentInstance = {
      id: 'spear',
    } as EquipmentInstance;

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent.replace(/\s+/g, ' ');
    expect(text).toContain('8.28 → 8.61');
    expect(text).toContain('+0.33');
  });

  it('uses tabs to separate management sections in the wide layout', () => {
    api.previewUpgrade.and.returnValues(
      of({} as EquipmentUpgradeQuote),
      of({} as EquipmentUpgradeQuote),
    );
    api.getBlueprints.and.returnValue(of([]));
    const fixture = TestBed.createComponent(EquipmentUpgradePanelComponent);
    fixture.componentInstance.equipmentInstance = {
      id: 'spear',
    } as EquipmentInstance;
    fixture.componentInstance.tabbed = true;
    fixture.detectChanges();

    const tabs = fixture.nativeElement.querySelectorAll('[role="tab"]');
    expect(tabs.length).toBe(2);
    expect(fixture.nativeElement.textContent).toContain('Reinforcement');
    expect(fixture.nativeElement.textContent).not.toContain(
      'Blueprints & variants',
    );

    tabs[1].click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Reinforcement');
    expect(fixture.nativeElement.textContent).toContain(
      'Blueprints & variants',
    );
  });
});
