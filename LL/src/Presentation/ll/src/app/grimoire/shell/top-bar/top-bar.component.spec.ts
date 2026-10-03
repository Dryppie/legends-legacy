import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import {
  LgTopBarCenterComponent,
  LgTopBarComponent,
} from './top-bar.component';
import { LgTopBarHarness } from '../../testing/top-bar.harness';

@Component({
  imports: [LgTopBarComponent, LgTopBarCenterComponent],
  template: `
    <lg-top-bar
      heading="Aldric Vane"
      eyebrow="Lv. 42"
      showMenu
      (menu)="menus = menus + 1"
    >
      @if (run()) {
        <lg-top-bar-center
          ><span class="sc-run">Floor 3</span></lg-top-bar-center
        >
      }
      <span class="sc-currency">12,480</span>
    </lg-top-bar>
  `,
})
class TopBarHost {
  readonly run = signal(true);
  menus = 0;
}

describe('LgTopBarComponent', () => {
  async function setup() {
    const fixture = TestBed.createComponent(TopBarHost);
    const bar =
      await TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgTopBarHarness,
      );
    return { fixture, bar, el: fixture.nativeElement as HTMLElement };
  }

  it('shows the heading and the eyebrow, and puts the rest at its end', async () => {
    const { bar, el } = await setup();

    expect(await bar.getHeading()).toBe('Aldric Vane');
    expect(el.querySelector('.lg-topbar__eyebrow')?.textContent?.trim()).toBe(
      'Lv. 42',
    );
    expect(el.querySelector('.lg-topbar__end .sc-currency')).not.toBeNull();
    expect(el.querySelector('lg-top-bar')?.hasAttribute('title')).toBeFalse();
  });

  it('holds the centre region between the title and the end, and knows when it has one', async () => {
    const { fixture, el } = await setup();
    const host = el.querySelector('lg-top-bar')!;

    expect(
      host.querySelector(
        ':scope > .lg-topbar__title + lg-top-bar-center + .lg-topbar__end',
      ),
    ).not.toBeNull();
    expect(host.classList).toContain('has-center');

    fixture.componentInstance.run.set(false);
    fixture.detectChanges();
    expect(host.querySelector('lg-top-bar-center')).toBeNull();
    expect(host.classList).not.toContain('has-center');
  });

  it('the menu button emits menu', async () => {
    const { fixture, bar } = await setup();

    expect(await bar.hasMenu()).toBeTrue();
    await bar.pressMenu();
    expect(fixture.componentInstance.menus).toBe(1);
  });
});
