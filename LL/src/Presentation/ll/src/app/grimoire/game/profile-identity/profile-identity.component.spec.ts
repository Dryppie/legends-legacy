import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  LgProfileFactComponent,
  LgProfileIdentityComponent,
} from './profile-identity.component';

@Component({
  imports: [LgProfileIdentityComponent, LgProfileFactComponent],
  template: `
    <lg-profile-identity
      eyebrow="Viewing player"
      name="Kaelen"
      noble
      [presence]="{ online: true }"
      as="h1"
    >
      <div lgProfileFact label="Guild"><a href="#">Ember Oath</a></div>
      <div lgProfileFact label="Level">42</div>
    </lg-profile-identity>
  `,
})
class IdentityHost {}

describe('LgProfileIdentityComponent', () => {
  it('is its own box: the name as its heading, the crown, presence, and its facts as a description list', () => {
    const fixture = TestBed.createComponent(IdentityHost);
    fixture.detectChanges();
    const identity: HTMLElement = fixture.nativeElement.querySelector(
      'lg-profile-identity',
    );
    expect(getComputedStyle(identity).display).toBe('grid');
    expect(identity.querySelector('h1')?.textContent).toBe('Kaelen');
    expect(identity.querySelector('[aria-label=Noble]')).not.toBeNull();
    expect(identity.querySelector('lg-presence')?.textContent).toBe('Online');
    const facts = identity.querySelectorAll('dl > [lgProfileFact]');
    expect(facts.length).toBe(2);
    expect(facts[0].querySelector('dt')?.textContent).toBe('Guild');
    expect(getComputedStyle(facts[0]).display).toBe('flex');
    expect(
      getComputedStyle(facts[0].querySelector('a')!).textDecorationLine,
    ).toBe('underline');
  });
});
