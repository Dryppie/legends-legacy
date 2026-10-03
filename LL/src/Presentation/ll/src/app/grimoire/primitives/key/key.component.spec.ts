import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgKeyComponent } from './key.component';

@Component({
  imports: [LgKeyComponent],
  template: `<p>Back <kbd lgKey>Esc</kbd></p>`,
})
class KeyHost {}

describe('LgKeyComponent', () => {
  it('is the kbd itself: a key cap at least as wide as it is tall', () => {
    const fixture = TestBed.createComponent(KeyHost);
    fixture.detectChanges();
    const key: HTMLElement = fixture.nativeElement.querySelector('kbd');
    expect(key.classList).toContain('lg-key');
    expect(key.textContent?.trim()).toBe('Esc');
    expect(key.children.length).toBe(0);
    const box = key.getBoundingClientRect();
    expect(box.width).toBeGreaterThanOrEqual(box.height);
  });
});
