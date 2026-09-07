import { computed, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  EquipmentLoadout,
  EquipmentLoadoutService,
} from '../../../core/services/api/equipment/equipment-loadout.service';
import { EquipmentLoadoutsComponent } from './equipment-loadouts.component';

describe('EquipmentLoadoutsComponent', () => {
  let fixture: ComponentFixture<EquipmentLoadoutsComponent>;
  let component: EquipmentLoadoutsComponent;
  let state: ReturnType<typeof makeState>;

  function makeState() {
    const loadouts = signal<EquipmentLoadout[]>([
      {
        id: 'starter',
        name: 'Starter',
        autoUseActivities: ['IdleCombat'],
        slots: [],
      },
      { id: 'dungeon', name: 'Dungeons', autoUseActivities: [], slots: [] },
    ]);
    const selectedId = signal<string | null>('starter');
    const pending = signal(false);
    return {
      loadouts,
      selectedId,
      selected: computed(() =>
        loadouts().find((entry) => entry.id === selectedId()),
      ),
      pending,
      busy: computed(() => pending()),
      error: signal<string | null>(null),
      unsavedChanges: signal(false),
      load: jasmine.createSpy('load'),
      select: jasmine.createSpy('select'),
      save: jasmine.createSpy('save'),
      newLoadout: jasmine
        .createSpy('newLoadout')
        .and.callFake(() => selectedId.set(null)),
      remove: jasmine.createSpy('remove'),
      setActivities: jasmine.createSpy('setActivities'),
      retrySave: jasmine.createSpy('retrySave'),
    };
  }

  beforeEach(async () => {
    state = makeState();
    await TestBed.configureTestingModule({
      imports: [EquipmentLoadoutsComponent],
      providers: [{ provide: EquipmentLoadoutService, useValue: state }],
    }).compileComponents();
    fixture = TestBed.createComponent(EquipmentLoadoutsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  function button(label: string): HTMLButtonElement {
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button'),
    ) as HTMLButtonElement[];
    const result = buttons.find((entry) => entry.textContent?.trim() === label);
    if (!result) throw new Error('Button not found: ' + label);
    return result;
  }

  it('selects and equips through the tab without duplicate buttons or slot previews', () => {
    button('Dungeons').click();
    expect(state.select).toHaveBeenCalledOnceWith('dungeon');
    const labels = Array.from(
      fixture.nativeElement.querySelectorAll('button'),
    ).map((entry) => (entry as HTMLElement).textContent?.trim());
    expect(labels).not.toContain('Equip');
    expect(labels).not.toContain('Replace with equipped');
    expect(fixture.nativeElement.querySelector('.loadout-slots')).toBeNull();

    state.selectedId.set('dungeon');
    fixture.detectChanges();
    expect(button('Dungeons').getAttribute('aria-pressed')).toBe('true');
    expect(component.name).toBe('Dungeons');
  });

  it('creates a named loadout from the currently equipped items', () => {
    button('+ New').click();
    fixture.detectChanges();
    expect(button('Create loadout').disabled).toBeTrue();
    component.name = 'New set';
    fixture.detectChanges();
    button('Create loadout').click();
    expect(state.save).toHaveBeenCalledOnceWith(null, 'New set');
  });

  it('only enables saving a changed name and preserves it during automatic refreshes', () => {
    expect(button('Save name').disabled).toBeTrue();
    component.name = 'Updated name';
    state.loadouts.update((loadouts) =>
      loadouts.map((loadout) => ({ ...loadout })),
    );
    fixture.detectChanges();
    expect(component.name).toBe('Updated name');
    button('Save name').click();
    expect(state.save).toHaveBeenCalledOnceWith('starter', 'Updated name');
  });

  it('requires confirmation before deleting the selected loadout', () => {
    button('Delete').click();
    fixture.detectChanges();
    expect(state.remove).not.toHaveBeenCalled();
    button('Delete loadout').click();
    expect(state.remove).toHaveBeenCalledOnceWith('starter');
  });

  it('keeps unsaved equipment changes visible and offers a retry', () => {
    state.error.set('Save failed');
    state.unsavedChanges.set(true);
    fixture.detectChanges();
    expect(button('Dungeons').disabled).toBeTrue();
    expect(button('+ New').disabled).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain(
      'Retry before switching loadouts',
    );
    button('Retry saving').click();
    expect(state.retrySave).toHaveBeenCalledOnceWith();
  });

  it('disables controls while changing equipment or saving a loadout', async () => {
    state.pending.set(true);
    fixture.detectChanges();
    await fixture.whenStable();
    const controls = Array.from(
      fixture.nativeElement.querySelectorAll('button, input'),
    ) as (HTMLButtonElement | HTMLInputElement)[];
    expect(controls.every((control) => control.disabled)).toBeTrue();
  });
});
