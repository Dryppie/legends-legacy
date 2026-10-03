import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgPresenceComponent } from './presence.component';

@Component({
  imports: [LgPresenceComponent],
  template: `<lg-presence [online]="online()" lastSeen="3 h ago" />`,
})
class PresenceHost {
  readonly online = signal(false);
}

describe('LgPresenceComponent', () => {
  it('says when the player was last seen, or that they are online, in words on the host', () => {
    const fixture = TestBed.createComponent(PresenceHost);
    fixture.detectChanges();
    const presence: HTMLElement =
      fixture.nativeElement.querySelector('lg-presence');
    expect(presence.classList).toContain('is-offline');
    expect(presence.textContent).toBe('Last seen 3 h ago');
    expect(presence.getAttribute('title')).toBe('Last seen 3 h ago');

    fixture.componentInstance.online.set(true);
    fixture.detectChanges();
    expect(presence.classList).toContain('is-online');
    expect(presence.textContent).toBe('Online');
  });
});
