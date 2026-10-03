import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgSwitchComponent } from './switch.component';
import { LgSwitchHarness } from '../../testing/switch.harness';

@Component({
  imports: [ReactiveFormsModule, LgSwitchComponent],
  template: `<lg-switch [formControl]="newLook">New look</lg-switch>`,
})
class SwitchHost {
  readonly newLook = new FormControl(false, { nonNullable: true });
}

describe('LgSwitchComponent', () => {
  it('is a switch named by its words; a press turns it on and off and tells the form', async () => {
    const fixture = TestBed.createComponent(SwitchHost);
    const sw = await TestbedHarnessEnvironment.loader(fixture).getHarness(
      LgSwitchHarness.with({ label: 'New look' }),
    );
    const host = fixture.componentInstance;

    expect(await sw.isChecked()).toBeFalse();
    await sw.toggle();
    expect(await sw.isChecked()).toBeTrue();
    expect(host.newLook.value).toBeTrue();
    await sw.blur();
    expect(host.newLook.touched).toBeTrue();

    host.newLook.setValue(false);
    expect(await sw.isChecked()).toBeFalse();

    host.newLook.disable();
    expect(await sw.isDisabled()).toBeTrue();
  });
});
