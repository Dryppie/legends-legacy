import { ApplicationRef, Component } from '@angular/core';
import { TestBed, fakeAsync, flush, tick } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import {
  LgToastEnd,
  LgToaster,
  LgToastOutletComponent,
} from './toaster.service';
import { LgToastHarness } from '../../testing/toast.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';
import { lgCloseTip } from '../../testing/tip.harness';

@Component({
  imports: [LgToastOutletComponent],
  host: { style: 'position: fixed; top: 0; left: 0; width: 40rem' },
  template: `<button type="button" class="opener">Sell</button
    ><lg-toast-outlet />`,
})
class StageHost {}

/*
 * Harnesses flush every timer in fakeAsync, so the tests that count time read the stack on the overlay directly.
 */
describe('LgToaster', () => {
  let toaster: LgToaster;

  beforeEach(lgAnnouncerIdle);
  afterEach(() => {
    lgCloseTip();
  });

  function setup() {
    const fixture = TestBed.createComponent(StageHost);
    fixture.detectChanges();
    toaster = TestBed.inject(LgToaster);
    return fixture;
  }
  /** The toasts in the stack, newest first, after a render. */
  function shown(): HTMLElement[] {
    TestBed.inject(ApplicationRef).tick();
    return Array.from(
      document.querySelectorAll<HTMLElement>('.lg-toast-stack lg-toast'),
    );
  }
  const heading = (el: HTMLElement) =>
    el.querySelector('.lg-toast__heading')?.textContent?.trim();
  const leaving = (el: HTMLElement) => el.classList.contains('is-leaving');
  const headings = () => shown().map(heading);

  it('shows what happened, never takes focus, and is announced', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    setup();
    const opener = document.querySelector<HTMLElement>('.opener')!;
    opener.focus();

    toaster.show({
      heading: 'Listed for 1,200 Cinders',
      text: 'It sells while you play.',
      tone: 'success',
    });
    tick(100);

    const [toast] = shown();
    expect(heading(toast)).toBe('Listed for 1,200 Cinders');
    expect(toast.querySelector('.lg-toast__text')?.textContent?.trim()).toBe(
      'It sells while you play.',
    );
    expect(toast.classList).toContain('lg-toast--success');
    expect(document.activeElement).toBe(opener);
    expect(watch.said).toEqual([
      'polite: Listed for 1,200 Cinders. It sells while you play.',
    ]);

    watch.stop();
    toaster.dismissAll();
    flush();
  }));

  it('a danger toast is said at once', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    setup();

    toaster.show({
      heading: "Couldn't save. Your change was undone.",
      tone: 'danger',
    });
    tick(100);

    expect(watch.said).toEqual([
      "assertive: Couldn't save. Your change was undone.",
    ]);
    watch.stop();
    toaster.dismissAll();
    flush();
  }));

  it('stays at least six seconds, then fades and goes', fakeAsync(() => {
    setup();
    const ended: LgToastEnd[] = [];
    toaster
      .show({ heading: 'Saved', duration: 2000 })
      .closed.subscribe((w) => ended.push(w));

    tick(5999);
    expect(leaving(shown()[0])).toBeFalse();

    tick(1);
    expect(leaving(shown()[0])).toBeTrue();
    expect(ended).toEqual(['timeout']);

    tick(200);
    expect(shown()).toEqual([]);
    flush();
  }));

  it('waits while the pointer is over it', fakeAsync(() => {
    setup();
    toaster.show({ heading: 'Saved' });
    tick(4000);
    const [toast] = shown();

    toast.dispatchEvent(new MouseEvent('mouseenter'));
    tick(20000);
    expect(leaving(shown()[0])).toBeFalse();

    toast.dispatchEvent(new MouseEvent('mouseleave'));
    tick(1999);
    expect(leaving(shown()[0])).toBeFalse();
    tick(1);
    expect(leaving(shown()[0])).toBeTrue();
    flush();
  }));

  it('shows three at most, newest on top; the fourth waits for one to go', fakeAsync(() => {
    setup();
    toaster.show({ heading: 'One' });
    tick(1000);
    toaster.show({ heading: 'Two' });
    toaster.show({ heading: 'Three' });
    const fourth: LgToastEnd[] = [];
    toaster.show({ heading: 'Four' }).closed.subscribe((w) => fourth.push(w));

    expect(headings()).toEqual(['Three', 'Two', 'One']);

    tick(5000);
    tick(200);
    expect(headings()).toEqual(['Four', 'Three', 'Two']);
    expect(fourth).toEqual([]);

    toaster.dismissAll();
    flush();
  }));

  it('Escape while focus is in it dismisses it, and focus goes back to where it came from', fakeAsync(() => {
    setup();
    const opener = document.querySelector<HTMLElement>('.opener')!;
    toaster.show({ heading: 'Saved' });
    const [toast] = shown();

    opener.focus();
    toast.querySelector<HTMLElement>('.lg-toast__dismiss')!.focus();
    lgCloseTip();
    document.activeElement!.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
    );

    expect(leaving(shown()[0])).toBeTrue();
    expect(document.activeElement).toBe(opener);
    flush();
  }));

  it('its action and Dismiss each end it, and say which', async () => {
    const fixture = setup();
    const page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    const ended: LgToastEnd[] = [];
    toaster
      .show({ heading: 'Sold Ashen Blade', action: 'Undo' })
      .closed.subscribe((w) => ended.push(w));
    toaster.show({ heading: 'Saved' }).closed.subscribe((w) => ended.push(w));

    const undo = await page.getHarness(
      LgToastHarness.with({ heading: 'Sold Ashen Blade' }),
    );
    expect(await undo.getActionLabel()).toBe('Undo');
    await undo.pressAction();
    await (
      await page.getHarness(LgToastHarness.with({ heading: 'Saved' }))
    ).dismiss();

    expect(ended).toEqual(['action', 'dismissed']);
    expect(await undo.isLeaving()).toBeTrue();
    toaster.dismissAll();
  });
});
