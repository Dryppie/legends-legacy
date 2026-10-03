import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LG_RADIO_GROUP } from './radio.component';
import { LgRadioGroupHarness } from '../../testing/radio-group.harness';

@Component({
  imports: [ReactiveFormsModule, ...LG_RADIO_GROUP],
  template: `
    <lg-radio-group label="Chat layout" [formControl]="layout">
      <lg-radio value="docked">Docked</lg-radio>
      <lg-radio value="floating">Floating drawer</lg-radio>
      <lg-radio value="hidden" disabled>Hidden</lg-radio>
    </lg-radio-group>
  `,
})
class RadioHost {
  readonly layout = new FormControl('docked', { nonNullable: true });
}

describe('LgRadioGroupComponent', () => {
  async function setup() {
    const fixture = TestBed.createComponent(RadioHost);
    const group = await TestbedHarnessEnvironment.loader(fixture).getHarness(
      LgRadioGroupHarness.with({ label: 'Chat layout' }),
    );
    return { fixture, host: fixture.componentInstance, group };
  }

  it('is a fieldset of native radios, one name, the chosen one checked', async () => {
    const { fixture, group } = await setup();
    expect(await group.getOptions()).toEqual([
      'Docked',
      'Floating drawer',
      'Hidden',
    ]);
    expect(await group.getSelected()).toBe('Docked');
    const names = Array.from(
      fixture.nativeElement.querySelectorAll('input[type=radio]'),
      (i: HTMLInputElement) => i.name,
    );
    expect(new Set(names).size).toBe(1);
    expect(
      fixture.nativeElement
        .querySelector('fieldset > legend')
        .textContent.trim(),
    ).toBe('Chat layout');
  });

  it('a press chooses and tells the form; the form’s value chooses too', async () => {
    const { host, group } = await setup();
    await group.select('Floating drawer');
    expect(host.layout.value).toBe('floating');
    expect(await group.getSelected()).toBe('Floating drawer');

    host.layout.setValue('docked');
    expect(await group.getSelected()).toBe('Docked');
  });

  it('a disabled choice can’t be chosen; a disabled control disables the group', async () => {
    const { host, group } = await setup();
    await group.select('Hidden');
    expect(host.layout.value).toBe('docked');

    host.layout.disable();
    expect(await group.isDisabled()).toBeTrue();
  });
});
