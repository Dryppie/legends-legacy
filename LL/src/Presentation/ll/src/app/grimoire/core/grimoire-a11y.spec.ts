import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LgLayerHandle, lgOpenLayer, lgTopLayer } from './grimoire-a11y';
import { lgCloseReasonTip } from '../testing/reason-tip.harness';

type LayerKind = 'modal' | 'popover';

/**
 * The parity case i-layers: a dialog opened from a button, and a popover opened from inside the dialog. Each layer is
 * registered while its flag is set, and closing one clears its flag and returns focus to its opener.
 */
@Component({
  template: `
    <button #opener class="open" (click)="openModal(opener)">open</button>
    @if (modal()) {
      <div class="dlg">
        <button class="pop" (click)="openPopover()">pop</button>
        @if (popover()) {
          <span class="popover">popover</span>
        }
      </div>
    }
  `,
})
class LayersCase {
  readonly modal = signal(false);
  readonly popover = signal(false);
  private readonly layers: Partial<Record<LayerKind, LgLayerHandle>> = {};

  openModal(opener: HTMLElement): void {
    this.modal.set(true);
    this.layers.modal = lgOpenLayer({
      kind: 'modal',
      opener,
      onClose: () => this.closeLayer('modal'),
    });
  }

  openPopover(): void {
    this.popover.set(true);
    this.layers.popover = lgOpenLayer({
      kind: 'popover',
      onClose: () => this.closeLayer('popover'),
    });
  }

  /** Closes whatever a failed test left open: the layer stack is shared by every test. */
  closeAll(): void {
    Object.values(this.layers).forEach((layer) => layer?.close(false));
  }

  private closeLayer(kind: LayerKind): void {
    (kind === 'modal' ? this.modal : this.popover).set(false);
    this.layers[kind]?.close();
    delete this.layers[kind];
  }
}

describe('Grimoire layer stack', () => {
  let fixture: ComponentFixture<LayersCase>;
  let page: LayersCase;

  beforeEach(() => {
    // The reason tip hears Escape before any layer; make sure none is open.
    lgCloseReasonTip();
    fixture = TestBed.createComponent(LayersCase);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => page.closeAll());

  /** A pointer press on a button focuses it, then clicks it. */
  function press(selector: string): HTMLElement {
    const button = (
      fixture.nativeElement as HTMLElement
    ).querySelector<HTMLElement>(selector)!;
    button.focus();
    button.click();
    fixture.detectChanges();
    return button;
  }

  /** Escape, pressed wherever focus is. */
  function escape(): void {
    (document.activeElement ?? document.body).dispatchEvent(
      new KeyboardEvent('keydown', {
        key: 'Escape',
        bubbles: true,
        cancelable: true,
      }),
    );
    fixture.detectChanges();
  }

  it('closes the topmost layer on each Escape, highest first, and returns focus to its opener (i-layers)', () => {
    expect(lgTopLayer())
      .withContext('no layer is open before the test')
      .toBeNull();

    const opener = press('button.open');
    expect(page.modal()).toBeTrue();
    expect(lgTopLayer()).toBe('modal');

    press('button.pop');
    expect(page.popover()).toBeTrue();
    // The popover sits on z-popover, below the modal, so the modal stays the top layer.
    expect(lgTopLayer()).toBe('modal');

    escape();
    expect(page.modal()).toBeFalse();
    expect(document.activeElement).toBe(opener);
    expect(lgTopLayer()).toBe('popover');

    escape();
    expect(page.popover()).toBeFalse();
    expect(lgTopLayer()).toBeNull();
    // The popover's opener left with the dialog, so focus stays where the first Escape put it.
    expect(document.activeElement).toBe(opener);
  });
});
