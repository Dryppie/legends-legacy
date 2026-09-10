import {
  ApplicationRef,
  ComponentRef,
  createComponent,
  Directive,
  ElementRef,
  EnvironmentInjector,
  forwardRef,
  HostListener,
  inject,
  Input,
  OnDestroy,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { ChatEquipmentLinkComponent } from './chat-equipment-link.component';
import {
  equipmentLinkRanges,
  chatMessageLength,
  expandEquipmentSelection,
} from '../../../shared/utils/chat/chat-equipment-links';

interface DraftSnapshot {
  value: string;
  start: number;
  end: number;
}

/** The model uses chat tokens; the editor presents each token as one uneditable name. */
@Directive({
  selector: '[appChatComposer]',
  exportAs: 'chatComposer',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ChatComposerDirective),
      multi: true,
    },
  ],
})
export class ChatComposerDirective implements ControlValueAccessor, OnDestroy {
  readonly nativeElement =
    inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly application = inject(ApplicationRef);
  private readonly environment = inject(EnvironmentInjector);
  private views: ComponentRef<ChatEquipmentLinkComponent>[] = [];
  private model = '';
  private savedStart = 0;
  private savedEnd = 0;
  private composing = false;
  private beforeEdit: DraftSnapshot | null = null;
  private undoStack: DraftSnapshot[] = [];
  private redoStack: DraftSnapshot[] = [];
  private onChange: (value: string) => void = () => {};
  private onTouched = () => {};
  @Input() disabled = false;

  get ownerDocument(): Document {
    return this.nativeElement.ownerDocument;
  }
  get value(): string {
    return this.model;
  }
  get selectionStart(): number {
    this.saveSelection();
    return this.savedStart;
  }
  get selectionEnd(): number {
    this.saveSelection();
    return this.savedEnd;
  }
  focus(): void {
    this.nativeElement.focus();
  }

  writeValue(value: string | null): void {
    if ((value ?? '') === this.model) return;
    this.model = value ?? '';
    this.undoStack = [];
    this.redoStack = [];
    this.render();
    this.savedStart = this.savedEnd = this.model.length;
  }
  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(disabled: boolean): void {
    this.disabled = disabled;
    this.nativeElement.contentEditable = String(!disabled);
    this.nativeElement.setAttribute('aria-disabled', String(disabled));
    this.nativeElement.tabIndex = disabled ? -1 : 0;
  }
  ngOnDestroy(): void {
    this.clearViews();
  }

  private clearViews(): void {
    for (const view of this.views) {
      this.application.detachView(view.hostView);
      view.destroy();
    }
    this.views = [];
  }

  private read(node: Node): string {
    if (node instanceof HTMLElement && node.dataset['chatToken'])
      return node.dataset['chatToken']!;
    if (node.nodeType === Node.TEXT_NODE) return node.textContent ?? '';
    if (node.nodeName === 'BR') return '';
    return Array.from(node.childNodes, (child) => this.read(child)).join('');
  }

  private render(): void {
    this.clearViews();
    this.nativeElement.replaceChildren();
    let cursor = 0;
    for (const link of equipmentLinkRanges(this.model)) {
      this.nativeElement.append(
        this.ownerDocument.createTextNode(this.model.slice(cursor, link.start)),
      );
      const atom = this.ownerDocument.createElement('span');
      atom.contentEditable = 'false';
      atom.dataset['chatToken'] = link.token;
      atom.className = 'inline-block';
      const view = createComponent(ChatEquipmentLinkComponent, {
        environmentInjector: this.environment,
      });
      this.views.push(view);
      view.setInput('equipmentId', link.id);
      view.setInput('name', link.name);
      view.setInput('focusable', false);
      atom.append(view.location.nativeElement);
      this.nativeElement.append(atom);
      this.application.attachView(view.hostView);
      view.changeDetectorRef.detectChanges();
      cursor = link.end;
    }
    this.nativeElement.append(
      this.ownerDocument.createTextNode(this.model.slice(cursor)),
    );
  }

  private offset(node: Node, offset: number): number {
    const range = this.ownerDocument.createRange();
    range.selectNodeContents(this.nativeElement);
    range.setEnd(node, offset);
    return this.read(range.cloneContents()).length;
  }

  @HostListener('document:selectionchange')
  saveSelection(): void {
    const selection = this.ownerDocument.getSelection();
    if (!selection?.rangeCount) return;
    const range = selection.getRangeAt(0);
    if (
      !this.nativeElement.contains(range.startContainer) ||
      !this.nativeElement.contains(range.endContainer)
    )
      return;
    this.savedStart = this.offset(range.startContainer, range.startOffset);
    this.savedEnd = this.offset(range.endContainer, range.endOffset);
  }

  setSelectionRange(start: number, end: number): void {
    const point = (offset: number): [Node, number] => {
      let remaining = Math.min(Math.max(offset, 0), this.model.length);
      const walk = (node: Node): [Node, number] | null => {
        if (node instanceof HTMLElement && node.dataset['chatToken']) {
          const length = node.dataset['chatToken']!.length;
          if (remaining <= length) {
            const index = Array.from(node.parentNode!.childNodes).indexOf(node);
            return [node.parentNode!, index + (remaining ? 1 : 0)];
          }
          remaining -= length;
        } else if (node.nodeType === Node.TEXT_NODE) {
          if (remaining <= (node.textContent?.length ?? 0))
            return [node, remaining];
          remaining -= node.textContent?.length ?? 0;
        } else {
          for (const child of Array.from(node.childNodes)) {
            const found = walk(child);
            if (found) return found;
          }
        }
        return null;
      };
      return (
        walk(this.nativeElement) ?? [
          this.nativeElement,
          this.nativeElement.childNodes.length,
        ]
      );
    };
    const range = this.ownerDocument.createRange();
    range.setStart(...point(start));
    range.setEnd(...point(end));
    const selection = this.ownerDocument.getSelection();
    selection?.removeAllRanges();
    selection?.addRange(range);
    this.savedStart = start;
    this.savedEnd = end;
  }

  private snapshot(): DraftSnapshot {
    return {
      value: this.model,
      start: this.selectionStart,
      end: this.selectionEnd,
    };
  }
  private remember(previous = this.snapshot()): void {
    this.undoStack.push(previous);
    if (this.undoStack.length > 100) this.undoStack.shift();
    this.redoStack = [];
  }

  replaceValue(value: string, caret: number): void {
    this.remember();
    this.model = value;
    this.render();
    this.focus();
    this.setSelectionRange(caret, caret);
    this.onChange(value);
  }

  private replaceSelection(text: string): void {
    const { start, end } = expandEquipmentSelection(
      this.model,
      this.selectionStart,
      this.selectionEnd,
    );
    const next = this.model.slice(0, start) + text + this.model.slice(end);
    if (chatMessageLength(next) <= 200)
      this.replaceValue(next, start + text.length);
  }

  private deleteAtom(backward: boolean): boolean {
    let start = this.selectionStart;
    let end = this.selectionEnd;
    const links = equipmentLinkRanges(this.model);
    if (start === end) {
      const link = links.find((link) =>
        backward
          ? start > link.start && start <= link.end
          : start >= link.start && start < link.end,
      );
      if (!link) return false;
      start = link.start;
      end = link.end;
    } else {
      if (!links.some((link) => start < link.end && end > link.start))
        return false;
      ({ start, end } = expandEquipmentSelection(this.model, start, end));
    }
    this.replaceValue(
      this.model.slice(0, start) + this.model.slice(end),
      start,
    );
    return true;
  }

  private history(redo: boolean): void {
    const from = redo ? this.redoStack : this.undoStack;
    const to = redo ? this.undoStack : this.redoStack;
    const next = from.pop();
    if (!next) return;
    to.push(this.snapshot());
    this.model = next.value;
    this.render();
    this.setSelectionRange(next.start, next.end);
    this.onChange(this.model);
  }

  @HostListener('keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (this.disabled || event.isComposing || event.keyCode === 229) return;
    if (
      (event.ctrlKey || event.metaKey) &&
      ['z', 'y'].includes(event.key.toLowerCase())
    ) {
      event.preventDefault();
      this.history(event.shiftKey || event.key.toLowerCase() === 'y');
    } else if (
      (event.key === 'Backspace' || event.key === 'Delete') &&
      this.deleteAtom(event.key === 'Backspace')
    ) {
      event.preventDefault();
    }
  }

  @HostListener('beforeinput', ['$event'])
  beforeInput(event: InputEvent): void {
    if (this.disabled) {
      event.preventDefault();
      return;
    }
    this.beforeEdit = this.snapshot();
    if (
      event.inputType === 'insertParagraph' ||
      event.inputType === 'insertLineBreak'
    )
      event.preventDefault();
    if (
      event.inputType === 'historyUndo' ||
      event.inputType === 'historyRedo'
    ) {
      event.preventDefault();
      this.history(event.inputType === 'historyRedo');
    }
    if (
      ['deleteContentBackward', 'deleteContentForward'].includes(
        event.inputType,
      ) &&
      this.deleteAtom(event.inputType === 'deleteContentBackward')
    )
      event.preventDefault();
  }

  @HostListener('input')
  onInput(): void {
    if (this.composing) return;
    const next = this.read(this.nativeElement);
    const caret = this.selectionStart;
    if (this.disabled || chatMessageLength(next) > 200) {
      this.render();
      this.setSelectionRange(
        Math.min(caret, this.model.length),
        Math.min(caret, this.model.length),
      );
      return;
    }
    if (next !== this.model) {
      this.remember(this.beforeEdit ?? this.snapshot());
      this.model = next;
      this.onChange(next);
    }
    // Native edits (for example, deleting a word on mobile) can remove an
    // atom without using replaceValue. Release its Angular view as well.
    this.views = this.views.filter((view) => {
      if (this.nativeElement.contains(view.location.nativeElement)) return true;
      this.application.detachView(view.hostView);
      view.destroy();
      return false;
    });
    this.beforeEdit = null;
    if (!next) this.nativeElement.replaceChildren();
  }
  @HostListener('compositionstart') compositionStart(): void {
    this.composing = true;
  }
  @HostListener('compositionend') compositionEnd(): void {
    this.composing = false;
    this.onInput();
  }
  @HostListener('blur') blur(): void {
    this.saveSelection();
    this.onTouched();
  }

  @HostListener('paste', ['$event'])
  paste(event: ClipboardEvent): void {
    event.preventDefault();
    if (this.disabled) return;
    const text =
      event.clipboardData?.getData('application/x-ll-chat') ||
      event.clipboardData?.getData('text/plain') ||
      '';
    this.replaceSelection(text.replace(/[\r\n]+/g, ' '));
  }
  @HostListener('copy', ['$event'])
  copy(event: ClipboardEvent): void {
    event.preventDefault();
    const { start, end } = expandEquipmentSelection(
      this.model,
      this.selectionStart,
      this.selectionEnd,
    );
    const text = this.model.slice(start, end);
    let plain = text;
    for (const link of equipmentLinkRanges(text))
      plain = plain.replace(link.token, `[${link.name}]`);
    event.clipboardData?.setData('text/plain', plain);
    event.clipboardData?.setData('application/x-ll-chat', text);
  }
  @HostListener('cut', ['$event'])
  cut(event: ClipboardEvent): void {
    this.copy(event);
    if (!this.disabled) this.replaceSelection('');
  }
  @HostListener('drop', ['$event']) preventDrop(event: DragEvent): void {
    event.preventDefault();
  }
}
