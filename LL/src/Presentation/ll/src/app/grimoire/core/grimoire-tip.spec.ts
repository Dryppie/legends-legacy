import { Component } from '@angular/core';
import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  flush,
  tick,
} from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { Overlay, OverlayContainer } from '@angular/cdk/overlay';
import { DomPortal } from '@angular/cdk/portal';
import { LgTip } from './grimoire-tip';
import { LgTipHarness } from '../testing/tip.harness';

@Component({
  // Kept in view: the tip closes when its element is off screen.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <button type="button" class="a">A</button>
    <button type="button" class="b">B</button>
  `,
})
class TipCase {}

describe('LgTip', () => {
  let fixture: ComponentFixture<TipCase>;
  let page: HarnessLoader;
  let tip: LgTip;

  beforeEach(() => {
    fixture = TestBed.createComponent(TipCase);
    fixture.detectChanges();
    page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    tip = TestBed.inject(LgTip);
  });

  const el = (cls: string) =>
    (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>(
      'button.' + cls,
    )!;
  const shown = () => page.getHarnessOrNull(LgTipHarness.with({ shown: true }));
  const panes = () =>
    Array.from(
      TestBed.inject(OverlayContainer)
        .getContainerElement()
        .querySelectorAll('.cdk-overlay-pane'),
    );
  /** Opens an overlay the way a popover will, and returns it with its pane. */
  function openOverlay() {
    const content = document.createElement('div');
    content.textContent = 'Popover';
    document.body.appendChild(content);
    const ref = TestBed.inject(Overlay).create();
    ref.attach(new DomPortal(content));
    return { ref, content };
  }

  it('is one tip for the page: showing it for another element moves it there', async () => {
    tip.show(el('a'), { text: 'About A' });
    expect(await (await shown())!.getReason()).toBe('About A');
    expect(tip.isShownFor(el('a'))).toBeTrue();

    tip.show(el('b'), { title: 'B', text: 'About B' });
    const t = await shown();
    expect(await t!.getTitle()).toBe('B');
    expect(await t!.getReason()).toBe('About B');
    expect(tip.isShownFor(el('a'))).toBeFalse();
    expect(document.querySelectorAll('.lg-tip').length).toBe(1);
    tip.hide();
  });

  it('sets a word, a warning and a footnote', async () => {
    tip.show(el('a'), {
      word: 'Insufficient',
      text: 'Short by 250 Cinders',
      meta: 'Cinders come from dungeons',
      tone: 'warning',
    });
    const t = (await shown())!;

    expect(await t.getWord()).toBe('Insufficient');
    expect(await t.isWarning()).toBeTrue();
    expect(await t.getMeta()).toBe('Cinders come from dungeons');
    tip.hide();
  });

  it('sits above an overlay that opened before it, and moves above one that opened since when shown again', () => {
    const first = openOverlay();
    try {
      tip.show(el('a'), { text: 'About A' });
      const tipPane = () =>
        panes().find((p) => p.classList.contains('lg-tip-pane'));
      expect(panes().at(-1)).toBe(tipPane()!);

      tip.hide();
      const second = openOverlay();
      try {
        tip.show(el('b'), { text: 'About B' });
        expect(panes().at(-1)).toBe(tipPane()!);
      } finally {
        second.ref.dispose();
        second.content.remove();
      }
    } finally {
      tip.hide();
      first.ref.dispose();
      first.content.remove();
    }
  });

  it('hears Escape before anything else on the page', async () => {
    const heard = jasmine.createSpy('document keydown');
    document.addEventListener('keydown', heard);
    try {
      tip.show(el('a'), { text: 'About A' });
      el('a').dispatchEvent(
        new KeyboardEvent('keydown', {
          key: 'Escape',
          bubbles: true,
          cancelable: true,
        }),
      );

      expect(await shown()).toBeNull();
      expect(heard).not.toHaveBeenCalled();

      el('a').dispatchEvent(
        new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
      );
      expect(heard).toHaveBeenCalledTimes(1);
    } finally {
      document.removeEventListener('keydown', heard);
    }
  });

  it('closes on a press elsewhere, not on a press on its element or on itself', async () => {
    tip.show(el('a'), { text: 'About A' }, { pinned: true });
    el('a').dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
    document
      .querySelector('.lg-tip')!
      .dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
    expect(await shown()).not.toBeNull();

    el('b').dispatchEvent(new PointerEvent('pointerdown', { bubbles: true }));
    expect(await shown()).toBeNull();
  });

  it('waits a moment after the pointer leaves, and stays while the pointer is on it', fakeAsync(() => {
    tip.show(el('a'), { text: 'About A' });
    const t = document.querySelector('.lg-tip')!;

    tip.hideSoon();
    tick(100);
    t.dispatchEvent(new MouseEvent('mouseenter'));
    tick(500);
    expect(t.classList.contains('is-shown')).toBeTrue();

    t.dispatchEvent(new MouseEvent('mouseleave'));
    tick(150);
    expect(t.classList.contains('is-shown')).toBeFalse();
    flush();
  }));

  it('leaves the overlay once it has faded, so it never covers the page', fakeAsync(() => {
    tip.show(el('a'), { text: 'About A' });
    expect(panes().some((p) => p.classList.contains('lg-tip-pane'))).toBeTrue();

    tip.hide();
    tick(200);
    fixture.detectChanges();
    flush();
    expect(
      panes().some(
        (p) => p.classList.contains('lg-tip-pane') && p.childElementCount > 0,
      ),
    ).toBeFalse();
  }));
});
