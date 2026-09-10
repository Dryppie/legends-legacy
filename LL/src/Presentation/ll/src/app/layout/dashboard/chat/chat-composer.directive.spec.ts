import { Component, ViewChild } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of } from 'rxjs';
import { ChatComposerDirective } from './chat-composer.directive';
import { EquipmentService } from '../../../core/services/api/equipment/equipment.service';
import { Rarity } from '../../../shared/models/enums/rarity';

@Component({
  imports: [FormsModule, ChatComposerDirective],
  template:
    '<div appChatComposer [(ngModel)]="draft" [disabled]="disabled" aria-label="Chat message"></div>',
})
class EditorHost {
  draft = '';
  disabled = false;
  @ViewChild(ChatComposerDirective) editor!: ChatComposerDirective;
}

describe('Inline chat composer', () => {
  const mace = '[Mace](equipment:2b84eb39-110d-4b01-aacd-72caef024eba)';
  const otherMace = '[Mace](equipment:2b84eb39-110d-4b01-aacd-72caef024ebb)';

  function setup(value: string) {
    TestBed.configureTestingModule({
      imports: [EditorHost],
      providers: [
        {
          provide: EquipmentService,
          useValue: {
            getLinkedEquipment: (id: string) => of({ id, rarity: Rarity.Epic }),
          },
        },
      ],
    });
    const fixture = TestBed.createComponent(EditorHost);
    fixture.componentInstance.draft = value;
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
    const host = fixture.componentInstance;
    const editor = host.editor;
    editor.focus();
    return { fixture, host, editor, element: editor.nativeElement };
  }

  it('shows colored names inline while preserving different IDs for identically named pieces', fakeAsync(() => {
    const { fixture, editor, element } = setup(`Buy ${mace} and ${otherMace}`);
    expect(element.textContent).toBe('Buy [Mace] and [Mace]');
    expect(element.querySelectorAll('.ll-rarity-epic').length).toBe(2);
    expect(element.querySelectorAll('[contenteditable="false"]').length).toBe(
      2,
    );
    expect(editor.value).toBe(`Buy ${mace} and ${otherMace}`);
    fixture.destroy();
  }));

  it('removes the entire adjacent piece with Backspace or Delete, including mobile beforeinput', fakeAsync(() => {
    const { fixture, host, editor, element } = setup(
      `A ${mace} B ${otherMace}`,
    );
    editor.setSelectionRange(2 + mace.length, 2 + mace.length);
    const backspace = new KeyboardEvent('keydown', {
      key: 'Backspace',
      bubbles: true,
      cancelable: true,
    });
    element.dispatchEvent(backspace);
    expect(backspace.defaultPrevented).toBeTrue();
    expect(host.draft).toBe(`A  B ${otherMace}`);
    editor.setSelectionRange(5, 5);
    const mobileDelete = new InputEvent('beforeinput', {
      inputType: 'deleteContentForward',
      bubbles: true,
      cancelable: true,
    });
    element.dispatchEvent(mobileDelete);
    expect(mobileDelete.defaultPrevented).toBeTrue();
    expect(host.draft).toBe('A  B ');
    expect(element.querySelector('[data-chat-token]')).toBeNull();
    fixture.destroy();
  }));

  it('replaces a selection spanning text and an item without leaving a partial link', fakeAsync(() => {
    const { fixture, host, editor, element } = setup(`Hello ${mace} today`);
    editor.setSelectionRange(3, 6 + mace.length);
    const clipboard = new DataTransfer();
    clipboard.setData('text/plain', 'p');
    element.dispatchEvent(
      new ClipboardEvent('paste', {
        clipboardData: clipboard,
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(host.draft).toBe('Help today');
    expect(element.textContent).toBe('Help today');
    fixture.destroy();
  }));

  it('copies and pastes multiple item links as atoms, and treats external HTML as plain text', fakeAsync(() => {
    const { fixture, host, editor, element } = setup(`${mace} ${otherMace}`);
    editor.setSelectionRange(0, editor.value.length);
    const clipboard = new DataTransfer();
    element.dispatchEvent(
      new ClipboardEvent('cut', {
        clipboardData: clipboard,
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(host.draft).toBe('');
    expect(clipboard.getData('text/plain')).toBe('[Mace] [Mace]');
    element.dispatchEvent(
      new ClipboardEvent('paste', {
        clipboardData: clipboard,
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(host.draft).toBe(`${mace} ${otherMace}`);
    expect(element.querySelectorAll('[data-chat-token]').length).toBe(2);
    editor.setSelectionRange(0, editor.value.length);
    const external = new DataTransfer();
    external.setData('text/html', '<img src=x onerror=alert(1)>');
    external.setData('text/plain', '<img src=x onerror=alert(1)>\nHello');
    element.dispatchEvent(
      new ClipboardEvent('paste', {
        clipboardData: external,
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(element.querySelector('img')).toBeNull();
    expect(host.draft).toBe('<img src=x onerror=alert(1)> Hello');
    fixture.destroy();
  }));

  it('restores an atom and its cursor with undo and redo', fakeAsync(() => {
    const { fixture, host, editor, element } = setup(mace);
    editor.setSelectionRange(mace.length, mace.length);
    element.dispatchEvent(
      new KeyboardEvent('keydown', {
        key: 'Backspace',
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(host.draft).toBe('');
    element.dispatchEvent(
      new KeyboardEvent('keydown', {
        key: 'z',
        ctrlKey: true,
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(host.draft).toBe(mace);
    expect(element.textContent).toBe('[Mace]');
    expect(editor.selectionStart).toBe(mace.length);
    element.dispatchEvent(
      new InputEvent('beforeinput', {
        inputType: 'historyRedo',
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(host.draft).toBe('');
    fixture.destroy();
  }));

  it('keeps item IDs intact when typing after an atom and rejects edits beyond the message limit', fakeAsync(() => {
    const draft = Array(4).fill(mace).join(' ');
    const { fixture, host, editor, element } = setup(draft);
    editor.setSelectionRange(draft.length, draft.length);
    element.dispatchEvent(
      new InputEvent('beforeinput', {
        inputType: 'insertText',
        data: ' hi',
        bubbles: true,
        cancelable: true,
      }),
    );
    const text = document.createTextNode(' hi');
    element.append(text);
    const range = document.createRange();
    range.setStart(text, 3);
    range.collapse(true);
    document.getSelection()!.removeAllRanges();
    document.getSelection()!.addRange(range);
    element.dispatchEvent(
      new InputEvent('input', { inputType: 'insertText', bubbles: true }),
    );
    expect(host.draft).toBe(`${draft} hi`);
    expect(editor.selectionStart).toBe(draft.length + 3);
    const clipboard = new DataTransfer();
    clipboard.setData('text/plain', 'x'.repeat(200));
    element.dispatchEvent(
      new ClipboardEvent('paste', {
        clipboardData: clipboard,
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(host.draft).toBe(`${draft} hi`);
    expect(element.querySelectorAll('[data-chat-token]').length).toBe(4);
    fixture.destroy();
  }));

  it('commits IME composition once and respects disabled chat access', fakeAsync(() => {
    const { fixture, host, element } = setup('');
    element.dispatchEvent(
      new CompositionEvent('compositionstart', { bubbles: true }),
    );
    element.textContent = 'こんにちは';
    element.dispatchEvent(
      new InputEvent('input', { isComposing: true, bubbles: true }),
    );
    expect(host.draft).toBe('');
    element.dispatchEvent(
      new CompositionEvent('compositionend', { bubbles: true }),
    );
    expect(host.draft).toBe('こんにちは');
    host.disabled = true;
    fixture.detectChanges();
    tick();
    expect(element.contentEditable).toBe('false');
    const clipboard = new DataTransfer();
    clipboard.setData('text/plain', 'changed');
    element.dispatchEvent(
      new ClipboardEvent('paste', {
        clipboardData: clipboard,
        bubbles: true,
        cancelable: true,
      }),
    );
    expect(host.draft).toBe('こんにちは');
    fixture.destroy();
  }));
});
