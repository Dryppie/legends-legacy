import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgCheckboxComponent } from './checkbox.component';
import { LgCheckboxHarness } from '../../testing/checkbox.harness';

@Component({
  imports: [ReactiveFormsModule, LgCheckboxComponent],
  template: `
    <lg-checkbox [formControl]="nobility">Show my Nobility</lg-checkbox>
    <lg-checkbox [(checked)]="all" [(indeterminate)]="some"
      >All channels</lg-checkbox
    >
  `,
})
class CheckboxHost {
  readonly nobility = new FormControl(true, { nonNullable: true });
  readonly all = signal(false);
  readonly some = signal(true);
}

describe('LgCheckboxComponent', () => {
  async function setup() {
    const fixture = TestBed.createComponent(CheckboxHost);
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const nobility = await loader.getHarness(
      LgCheckboxHarness.with({ label: 'Show my Nobility' }),
    );
    const all = await loader.getHarness(
      LgCheckboxHarness.with({ label: 'All channels' }),
    );
    return { fixture, host: fixture.componentInstance, nobility, all };
  }

  it('is a native checkbox named by its words, bound to its form control', async () => {
    const { host, nobility } = await setup();
    expect(await nobility.isChecked()).toBeTrue();

    await nobility.toggle();
    expect(await nobility.isChecked()).toBeFalse();
    expect(host.nobility.value).toBeFalse();

    host.nobility.setValue(true);
    expect(await nobility.isChecked()).toBeTrue();
    await nobility.blur();
    expect(host.nobility.touched).toBeTrue();
  });

  it('a disabled form control disables it', async () => {
    const { host, nobility } = await setup();
    host.nobility.disable();
    expect(await nobility.isDisabled()).toBeTrue();
  });

  it('mixed shows a dash; a press checks it', async () => {
    const { host, all } = await setup();
    expect(await all.isIndeterminate()).toBeTrue();

    await all.toggle();
    expect(await all.isIndeterminate()).toBeFalse();
    expect(await all.isChecked()).toBeTrue();
    expect(host.all()).toBeTrue();
    expect(host.some()).toBeFalse();
  });
});
