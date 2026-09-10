import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { OverlayContainer } from '@angular/cdk/overlay';
import { Observable, of, Subject } from 'rxjs';
import { ChatEquipmentLinkComponent } from './chat-equipment-link.component';
import { EquipmentService } from '../../../core/services/api/equipment/equipment.service';
import { EquipmentInstance } from '../../../shared/models/item';
import { ItemType } from '../../../shared/models/enums/itemType';
import { EquipmentType } from '../../../shared/models/enums/equipmentType';
import { Rarity } from '../../../shared/models/enums/rarity';
import { ItemQuality } from '../../../shared/models/enums/itemQuality';

describe('Chat equipment tooltip', () => {
  const id = '2b84eb39-110d-4b01-aacd-72caef024eba';
  const base = {
    id: 'mace',
    name: 'Phoenix Mace',
    itemType: ItemType.Equipment,
    equipmentType: EquipmentType.OneHanded,
    rarity: Rarity.Epic,
    attributeModifiers: [],
    description: '',
    stackable: false,
    itemBudget: 191,
    itemBudgetTier: 1,
  };
  const item: EquipmentInstance = {
    id,
    displayName: 'Phoenix Mace',
    itemBase: base,
    equipmentBase: base,
    rarity: Rarity.Epic,
    quality: ItemQuality.Standard,
    tier: 1,
    baseModifiers: [],
    instanceModifiers: [],
    attributeModifiers: [],
    affinityTags: [],
    itemBudget: 191,
    itemBudgetTier: 1,
    isGuildBorrowed: false,
  };

  function setup(
    result: Observable<EquipmentInstance> = of(item),
    rarity: Rarity | null = Rarity.Epic,
  ) {
    const lookup = jasmine.createSpy().and.returnValue(result);
    TestBed.configureTestingModule({
      imports: [ChatEquipmentLinkComponent],
      providers: [
        { provide: EquipmentService, useValue: { getLinkedEquipment: lookup } },
      ],
    });
    const fixture = TestBed.createComponent(ChatEquipmentLinkComponent);
    fixture.componentRef.setInput('equipmentId', id);
    fixture.componentRef.setInput('name', 'Phoenix Mace');
    fixture.componentRef.setInput('rarity', rarity ?? undefined);
    fixture.detectChanges();
    const origin: HTMLElement =
      fixture.nativeElement.querySelector('[tabindex="0"]');
    const overlay = TestBed.inject(OverlayContainer).getContainerElement();
    return { fixture, lookup, origin, overlay };
  }

  it('colors the name from the message without fetching until hover, then reuses loaded details', fakeAsync(() => {
    const { fixture, lookup, origin, overlay } = setup();
    expect(lookup).not.toHaveBeenCalled();
    expect(origin.querySelector('.ll-rarity-epic')?.textContent).toContain(
      'Phoenix Mace',
    );
    origin.dispatchEvent(
      new PointerEvent('pointerenter', { pointerType: 'mouse' }),
    );
    tick(150);
    fixture.detectChanges();
    expect(lookup).toHaveBeenCalledOnceWith(id);
    expect(overlay.textContent).toContain('Phoenix Mace');
    expect(overlay.querySelector('app-equipment-display')).not.toBeNull();
    fixture.componentInstance.load();
    expect(lookup).toHaveBeenCalledOnceWith(id);
    fixture.destroy();
    tick();
  }));

  it('keeps old links neutral until opened and then uses their resolved rarity', fakeAsync(() => {
    const { fixture, lookup, origin } = setup(of(item), null);
    expect(lookup).not.toHaveBeenCalled();
    expect(origin.querySelector('.ll-text-muted')).not.toBeNull();
    origin.dispatchEvent(new FocusEvent('focus'));
    tick(150);
    fixture.detectChanges();
    expect(lookup).toHaveBeenCalledOnceWith(id);
    expect(origin.querySelector('.ll-rarity-epic')).not.toBeNull();
    fixture.destroy();
    tick();
  }));

  it('shows loading and missing-item feedback on keyboard focus, then retries', fakeAsync(() => {
    const pending = new Subject<EquipmentInstance>();
    const { fixture, lookup, origin, overlay } = setup(pending);
    expect(lookup).not.toHaveBeenCalled();
    origin.dispatchEvent(new FocusEvent('focus'));
    tick(150);
    fixture.detectChanges();
    expect(overlay.textContent).toContain('Loading equipment');
    fixture.componentInstance.load();
    expect(lookup).toHaveBeenCalledOnceWith(id);
    pending.error({ status: 404 });
    fixture.detectChanges();
    expect(overlay.textContent).toContain('no longer available');
    expect(origin.querySelector('.ll-rarity-epic')).not.toBeNull();
    lookup.and.returnValue(of(item));
    fixture.componentInstance.load();
    fixture.detectChanges();
    expect(overlay.textContent).toContain('Phoenix Mace');
    fixture.destroy();
    tick();
  }));

  it('opens by touch and offers a close button', fakeAsync(() => {
    const { fixture, lookup, origin, overlay } = setup();
    expect(lookup).not.toHaveBeenCalled();
    origin.dispatchEvent(
      new PointerEvent('pointerdown', { pointerType: 'touch', bubbles: true }),
    );
    origin.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    tick(150);
    fixture.detectChanges();
    expect(overlay.textContent).toContain('Phoenix Mace');
    expect(lookup).toHaveBeenCalledOnceWith(id);
    expect(
      overlay.querySelector('[aria-label="Close item details"]'),
    ).not.toBeNull();
    fixture.destroy();
    tick();
  }));
});
