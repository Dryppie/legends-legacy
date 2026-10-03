import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LG_POPOVER } from './popover.component';
import { LgPopoverTriggerHarness } from '../../testing/popover.harness';

@Component({
  imports: [...LG_POPOVER],
  template: `
    <button
      type="button"
      class="trigger"
      [lgPopoverTrigger]="filters"
      [(lgPopoverOpen)]="open"
    >
      Filters
    </button>
    <lg-popover #filters label="Filters">
      <p>Show only:</p>
      <button type="button" class="first">Equipped</button>
      <button type="button" class="last">Favourites</button>
    </lg-popover>
    <button type="button" class="away">Away</button>
  `,
})
class PopoverHost {
  readonly open = signal(false);
}

describe('LgPopoverTriggerDirective', () => {
  let fixture: ComponentFixture<PopoverHost>;
  let trigger: LgPopoverTriggerHarness;

  beforeEach(async () => {
    fixture = TestBed.createComponent(PopoverHost);
    fixture.detectChanges();
    const loader: HarnessLoader = TestbedHarnessEnvironment.loader(fixture);
    trigger = await loader.getHarness(
      LgPopoverTriggerHarness.with({ text: 'Filters' }),
    );
  });

  afterEach(() => fixture.destroy());

  const inside = (cls: string) =>
    document.querySelector(`.lg-popover .${cls}`) as HTMLElement;
  const key = (el: Element, k: string, shiftKey = false) =>
    el.dispatchEvent(
      new KeyboardEvent('keydown', {
        key: k,
        shiftKey,
        bubbles: true,
        cancelable: true,
      }),
    );

  it('a press opens it on the overlay, named, with focus inside; its element says it is expanded', async () => {
    await trigger.press();

    expect(await trigger.isOpen()).toBeTrue();
    expect(fixture.componentInstance.open()).toBeTrue();
    expect(await trigger.getPopoverLabel()).toBe('Filters');
    expect(await trigger.hasFocusInside()).toBeTrue();
    expect(fixture.nativeElement.querySelector('.lg-popover')).toBeNull();
  });

  it('Escape closes it and focus goes back to its element', async () => {
    await trigger.press();
    await trigger.pressEscape();

    expect(await trigger.isOpen()).toBeFalse();
    expect(await trigger.isFocused()).toBeTrue();
  });

  it('a second press closes it', async () => {
    await trigger.press();
    await trigger.press();

    expect(await trigger.isOpen()).toBeFalse();
    expect(await trigger.isFocused()).toBeTrue();
  });

  it('Tab past its last control, or back past its first, closes it and goes back to its element', async () => {
    await trigger.press();
    inside('last').focus();
    key(inside('last'), 'Tab');
    await fixture.whenStable();
    expect(await trigger.isOpen()).toBeFalse();
    expect(await trigger.isFocused()).toBeTrue();

    await trigger.press();
    inside('first').focus();
    key(inside('first'), 'Tab', true);
    await fixture.whenStable();
    expect(await trigger.isOpen()).toBeFalse();
    expect(await trigger.isFocused()).toBeTrue();
  });

  it('a press outside closes it and leaves focus where the player pressed', async () => {
    await trigger.press();
    const away = fixture.nativeElement.querySelector('.away') as HTMLElement;
    away.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
    away.focus();
    away.click();
    await fixture.whenStable();

    expect(await trigger.isOpen()).toBeFalse();
    expect(document.activeElement).toBe(away);
  });

  it('[(lgPopoverOpen)] opens and closes it', async () => {
    fixture.componentInstance.open.set(true);
    expect(await trigger.isOpen()).toBeTrue();
    expect(await trigger.getPopoverText()).toContain('Show only:');
    fixture.componentInstance.open.set(false);
    expect(await trigger.isOpen()).toBeFalse();
  });
});
