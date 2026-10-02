import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgOptionComponent, LgOptionParent } from './option.component';

/** A list that records what its options report. */
@Component({
  imports: [LgOptionComponent],
  providers: [{ provide: LgOptionParent, useExisting: ListHost }],
  template: `
    <div role="listbox">
      <lg-option value="maren">Maren</lg-option>
      <lg-option value="kaelen" disabled>Kaelen</lg-option>
    </div>
  `,
})
class ListHost extends LgOptionParent {
  readonly highlighted: string[] = [];
  readonly chosen: string[] = [];

  highlight(option: LgOptionComponent): void {
    this.highlighted.push(option.value());
  }

  choose(option: LgOptionComponent): void {
    this.chosen.push(option.value());
  }
}

describe('LgOptionComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(ListHost);
    fixture.detectChanges();
    const [maren, kaelen] = Array.from(
      fixture.nativeElement.querySelectorAll('lg-option'),
    ) as HTMLElement[];
    return { fixture, host: fixture.componentInstance, maren, kaelen };
  }

  it('is an option, selected while highlighted, and reports the pointer and a press to its list', () => {
    const { fixture, host, maren } = setup();
    const option = fixture.debugElement.children[0].children[0]
      .componentInstance as LgOptionComponent;

    expect(maren.getAttribute('role')).toBe('option');
    expect(maren.getAttribute('aria-selected')).toBe('false');
    option.setActiveStyles();
    fixture.detectChanges();
    expect(maren.getAttribute('aria-selected')).toBe('true');
    expect(maren.classList).toContain('is-active');
    expect(option.getLabel()).toBe('Maren');

    maren.dispatchEvent(new MouseEvent('mouseenter'));
    maren.click();
    expect(host.highlighted).toEqual(['maren']);
    expect(host.chosen).toEqual(['maren']);
  });

  it('a disabled option is aria-disabled, skipped by the keys and never chosen', () => {
    const { fixture, host, kaelen } = setup();
    const option = fixture.debugElement.children[0].children[1]
      .componentInstance as LgOptionComponent;

    expect(kaelen.getAttribute('aria-disabled')).toBe('true');
    expect(option.disabled).toBeTrue();
    kaelen.click();
    expect(host.chosen).toEqual([]);
  });

  it('keeps focus where it is when pressed', () => {
    const { maren } = setup();
    const press = new MouseEvent('mousedown', { cancelable: true });

    maren.dispatchEvent(press);

    expect(press.defaultPrevented).toBeTrue();
  });
});
