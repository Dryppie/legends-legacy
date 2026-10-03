import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgIconComponent } from './icon.component';
import { LgIconName } from '../../core/grimoire-icons';

@Component({
  imports: [LgIconComponent],
  template: `<lg-icon [name]="name()" [size]="16" [label]="label()" />`,
})
class IconHost {
  readonly name = signal<LgIconName>('inventory');
  readonly label = signal<string | undefined>(undefined);
}

describe('LgIconComponent', () => {
  function setup() {
    spyOn(console, 'warn');
    const fixture = TestBed.createComponent(IconHost);
    fixture.detectChanges();
    const host: HTMLElement = fixture.nativeElement.querySelector('lg-icon');
    return { fixture, host };
  }

  it('is its own box, its size square, and hidden from screen readers beside words', () => {
    const { host } = setup();
    expect(host.classList).toContain('lg-icon');
    expect(getComputedStyle(host).display).toBe('inline-block');
    expect(getComputedStyle(host).width).toBe('16px');
    expect(getComputedStyle(host).height).toBe('16px');
    expect(host.getAttribute('aria-hidden')).toBe('true');
    expect(host.querySelector('svg')).not.toBeNull();
  });

  it('with a label it is an image named by it', () => {
    const { fixture, host } = setup();
    fixture.componentInstance.label.set('Inventory');
    fixture.detectChanges();
    expect(host.getAttribute('role')).toBe('img');
    expect(host.getAttribute('aria-label')).toBe('Inventory');
    expect(host.hasAttribute('aria-hidden')).toBeFalse();
    expect(host.hasAttribute('title')).toBeFalse();
  });

  it('an unknown name draws nothing and takes no room', () => {
    const { fixture, host } = setup();
    fixture.componentInstance.name.set('no-such-icon' as LgIconName);
    fixture.detectChanges();
    expect(host.querySelector('svg')).toBeNull();
    expect(getComputedStyle(host).display).toBe('none');
  });
});
