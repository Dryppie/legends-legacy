import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HarnessLoader, TestKey } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { LgSelectComponent } from './select.component';
import { LgOptionComponent } from '../option/option.component';
import { LgFieldComponent } from '../field/field.component';
import { LgSelectHarness } from '../../testing/select.harness';

@Component({
  imports: [
    ReactiveFormsModule,
    LgSelectComponent,
    LgOptionComponent,
    LgFieldComponent,
  ],
  template: `
    <lg-field label="Rarity" hint="Filter the list">
      <lg-select [formControl]="rarity" placeholder="Any rarity">
        <lg-option value="Common">Common</lg-option>
        <lg-option value="Rare">Rare</lg-option>
        <lg-option value="Unique" disabled>Unique</lg-option>
        <lg-option value="Epic">Epic</lg-option>
        <lg-option value="Legendary">Legendary</lg-option>
      </lg-select>
    </lg-field>
    <lg-select label="Sort by" [(value)]="sort">
      <lg-option value="name">Name</lg-option>
      <lg-option value="level">Level</lg-option>
    </lg-select>
  `,
})
class SelectHost {
  readonly rarity = new FormControl<string | null>(null, Validators.required);
  sort: string | null = 'level';
}

describe('LgSelectComponent', () => {
  let loader: HarnessLoader;
  let host: SelectHost;
  let rarity: LgSelectHarness;

  beforeEach(async () => {
    const fixture = TestBed.createComponent(SelectHost);
    fixture.detectChanges();
    host = fixture.componentInstance;
    loader = TestbedHarnessEnvironment.loader(fixture);
    rarity = await loader.getHarness(
      LgSelectHarness.with({ value: 'Any rarity' }),
    );
  });

  it('shows its placeholder until a choice, and is named by its Field’s label', async () => {
    expect(await rarity.hasValue()).toBeFalse();
    expect(await rarity.getLabel()).toBe('Rarity *');
    expect(await rarity.isOpen()).toBeFalse();
  });

  it('a press opens its options; choosing one sets the form’s value, closes the list and keeps focus on it', async () => {
    await rarity.press();
    expect(await rarity.isOpen()).toBeTrue();
    expect(await rarity.getOptionTexts()).toEqual([
      'Common',
      'Rare',
      'Unique',
      'Epic',
      'Legendary',
    ]);
    expect(await rarity.getHighlightedText()).toBe('Common');

    await rarity.choose('Epic');

    expect(host.rarity.value).toBe('Epic');
    expect(await rarity.getValueText()).toBe('Epic');
    expect(await rarity.isOpen()).toBeFalse();
    expect(await rarity.isFocused()).toBeTrue();
  });

  it('opens on its choice, marked chosen', async () => {
    host.rarity.setValue('Rare');
    await rarity.press();

    expect(await rarity.getHighlightedText()).toBe('Rare');
    expect(await rarity.getChosenOptionText()).toBe('Rare');
  });

  it('the keys: the down arrow opens it, arrows move past a disabled option, Enter chooses', async () => {
    await rarity.pressKey(TestKey.DOWN_ARROW);
    expect(await rarity.isOpen()).toBeTrue();
    expect(await rarity.getHighlightedText()).toBe('Common');

    await rarity.pressKey(TestKey.DOWN_ARROW);
    await rarity.pressKey(TestKey.DOWN_ARROW);
    expect(await rarity.getHighlightedText()).toBe('Epic');

    await rarity.pressKey(TestKey.ENTER);
    expect(host.rarity.value).toBe('Epic');
    expect(await rarity.isOpen()).toBeFalse();
  });

  it('End goes to the last option; Tab chooses the highlighted one and moves on', async () => {
    await rarity.pressKey(TestKey.END);
    expect(await rarity.getHighlightedText()).toBe('Legendary');

    await rarity.pressKey(TestKey.TAB);
    expect(host.rarity.value).toBe('Legendary');
    expect(await rarity.isOpen()).toBeFalse();
  });

  it('Escape closes it without a choice, and goes no further', async () => {
    let heard = false;
    const listen = (e: KeyboardEvent) => (heard = heard || e.key === 'Escape');
    document.addEventListener('keydown', listen);
    await rarity.pressKey(TestKey.DOWN_ARROW);
    await rarity.pressKey(TestKey.ESCAPE);
    document.removeEventListener('keydown', listen);

    expect(await rarity.isOpen()).toBeFalse();
    expect(host.rarity.value).toBeNull();
    expect(heard).toBeFalse();
  });

  it('leaving it marks the control touched, so the Field shows its error', async () => {
    await rarity.press();
    await rarity.blur();

    expect(host.rarity.touched).toBeTrue();
    expect(await rarity.isOpen()).toBeFalse();
  });

  it('a disabled control disables it', async () => {
    host.rarity.disable();
    expect(await rarity.isDisabled()).toBeTrue();
  });

  it('outside a Field it is named by its label, and [(value)] binds the choice', async () => {
    const sort = await loader.getHarness(
      LgSelectHarness.with({ value: 'Level' }),
    );
    expect(await sort.getLabel()).toBe('Sort by');

    await sort.choose('Name');
    expect(host.sort).toBe('name');
  });
});
