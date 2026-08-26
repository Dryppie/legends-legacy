import { DOCUMENT } from '@angular/common';
import { FocusTrap, FocusTrapFactory } from '@angular/cdk/a11y';
import {
  AfterViewInit,
  Directive,
  ElementRef,
  EventEmitter,
  inject,
  Input,
  OnDestroy,
  Output,
} from '@angular/core';

@Directive({
  selector: '[appDialogFocus]',
  standalone: true,
  host: {
    '[attr.role]': 'dialogRole',
    'aria-modal': 'true',
    tabindex: '-1',
    '(keydown)': 'onKeydown($event)',
  },
})
export class DialogFocusDirective implements AfterViewInit, OnDestroy {
  @Input() dialogRole: 'dialog' | 'alertdialog' = 'dialog';
  @Input() dialogEscapeDisabled = false;
  @Output() dialogEscape = new EventEmitter<void>();

  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly focusTrapFactory = inject(FocusTrapFactory);
  private readonly document = inject(DOCUMENT);
  private readonly restoreFocusTarget =
    this.document.activeElement instanceof HTMLElement
      ? this.document.activeElement
      : null;
  private focusTrap?: FocusTrap;

  ngAfterViewInit(): void {
    this.focusTrap = this.focusTrapFactory.create(this.element.nativeElement);
    void this.focusTrap.focusInitialElementWhenReady().then((focused) => {
      if (!focused) this.element.nativeElement.focus();
    });
  }

  ngOnDestroy(): void {
    this.focusTrap?.destroy();
    const target = this.restoreFocusTarget;
    if (!target) return;

    queueMicrotask(() => {
      if (target.isConnected) target.focus();
    });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Escape' || this.dialogEscapeDisabled) return;

    event.preventDefault();
    event.stopPropagation();
    this.dialogEscape.emit();
  }
}
