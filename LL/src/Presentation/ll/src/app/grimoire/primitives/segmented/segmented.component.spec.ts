import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LG_SEGMENTED } from './segmented.component';
import { LgSegmentedHarness } from '../../testing/segmented.harness';

@Component({
  imports: [ReactiveFormsModule, ...LG_SEGMENTED],
  template: `
    <lg-segmented label="Text size" [formControl]="size">
      @for (s of sizes; track s.value) {
        <lg-segment [value]="s.value" [disabled]="s.value === 'huge'">{{
          s.label
        }}</lg-segment>
      }
    </lg-segmented>
  `,
})
class SegmentedHost {
  readonly size = new FormControl('default', { nonNullable: true });
  readonly sizes = [
    { value: 'default', label: 'Default' },
    { value: 'large', label: 'Large' },
    { value: 'extra', label: 'Extra large' },
    { value: 'huge', label: 'Huge' },
  ];
}

describe('LgSegmentedComponent', () => {
  async function setup() {
    const fixture = TestBed.createComponent(SegmentedHost);
    const segmented = await TestbedHarnessEnvironment.loader(
      fixture,
    ).getHarness(LgSegmentedHarness.with({ label: 'Text size' }));
    return { fixture, host: fixture.componentInstance, segmented };
  }

  it('is a radio group named by its label, the chosen segment its one tab stop', async () => {
    const { fixture, segmented } = await setup();
    const group = fixture.nativeElement.querySelector(
      '[role=radiogroup]',
    ) as HTMLElement;
    expect(
      document.getElementById(group.getAttribute('aria-labelledby')!)
        ?.textContent,
    ).toBe('Text size');
    expect(await segmented.getSegments()).toEqual([
      'Default',
      'Large',
      'Extra large',
      'Huge',
    ]);
    expect(await segmented.getSelected()).toBe('Default');
    expect(await segmented.getTabStop()).toBe('Default');
  });

  it('a press chooses and tells the form', async () => {
    const { host, segmented } = await setup();
    await segmented.select('Large');
    expect(host.size.value).toBe('large');
    expect(await segmented.getSelected()).toBe('Large');
    expect(await segmented.getTabStop()).toBe('Large');
  });

  it('the arrow keys move the choice, pass over a disabled segment and wrap', async () => {
    const { host, segmented } = await setup();
    await segmented.select('Default');

    await segmented.pressKey('ArrowRight');
    expect(await segmented.getFocused()).toBe('Large');
    expect(host.size.value).toBe('large');

    await segmented.pressKey('ArrowRight');
    await segmented.pressKey('ArrowRight');
    expect(await segmented.getFocused()).toBe('Default');
    expect(host.size.value).toBe('default');

    await segmented.pressKey('ArrowLeft');
    expect(await segmented.getSelected()).toBe('Extra large');
  });

  it('the form’s value chooses; a disabled control disables it', async () => {
    const { fixture, host, segmented } = await setup();
    host.size.setValue('extra');
    expect(await segmented.getSelected()).toBe('Extra large');

    host.size.disable();
    fixture.detectChanges();
    await segmented.select('Default');
    expect(host.size.value).toBe('extra');
  });
});
