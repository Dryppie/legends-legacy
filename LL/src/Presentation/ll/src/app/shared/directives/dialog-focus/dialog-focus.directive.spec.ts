import { NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DialogFocusDirective } from './dialog-focus.directive';

@Component({
  imports: [NgIf, DialogFocusDirective],
  template: `
    <button id="dialog-trigger" type="button" (click)="open = true">
      Open
    </button>
    <section
      *ngIf="open"
      appDialogFocus
      aria-label="Example dialog"
      (dialogEscape)="open = false"
    >
      <button id="dialog-action" type="button">Action</button>
    </section>
  `,
})
class DialogFocusHostComponent {
  open = false;
}

describe('DialogFocusDirective', () => {
  let fixture: ComponentFixture<DialogFocusHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DialogFocusHostComponent],
    }).compileComponents();
    fixture = TestBed.createComponent(DialogFocusHostComponent);
    fixture.detectChanges();
  });

  it('captures focus, closes on Escape, and restores the trigger', async () => {
    const trigger: HTMLButtonElement =
      fixture.nativeElement.querySelector('#dialog-trigger');
    trigger.focus();
    trigger.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect((document.activeElement as HTMLElement)?.id).toBe('dialog-action');

    const dialog: HTMLElement =
      fixture.nativeElement.querySelector('[appDialogFocus]');
    dialog.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
    );
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[appDialogFocus]')).toBeNull();
    expect(document.activeElement).toBe(trigger);
  });
});
