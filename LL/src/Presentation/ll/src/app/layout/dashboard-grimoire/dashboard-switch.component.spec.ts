import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { GrimoirePreviewPreferenceService } from '../../core/services/client-side/grimoire-preview/grimoire-preview-preference.service';
import { DashboardSwitchComponent, isNewLookShortcut } from './dashboard-switch.component';

describe('DashboardSwitchComponent', () => {
  const key = (over: Partial<KeyboardEventInit> = {}) =>
    new KeyboardEvent('keydown', { code: 'Space', key: ' ', ctrlKey: true, cancelable: true, ...over });

  it('knows Ctrl + Space and nothing else', () => {
    expect(isNewLookShortcut(key())).toBeTrue();
    expect(isNewLookShortcut(key({ ctrlKey: false }))).toBeFalse();
    expect(isNewLookShortcut(key({ shiftKey: true }))).toBeFalse();
    expect(isNewLookShortcut(key({ altKey: true }))).toBeFalse();
    expect(isNewLookShortcut(key({ metaKey: true }))).toBeFalse();
    expect(isNewLookShortcut(key({ repeat: true }))).toBeFalse();
    expect(isNewLookShortcut(key({ code: 'KeyK', key: 'k' }))).toBeFalse();
  });

  it('flips the New look setting on Ctrl + Space', () => {
    const newLook = signal(false);
    const pref = { newLook, setNewLook: jasmine.createSpy('setNewLook').and.callFake((v: boolean) => newLook.set(v)) };
    TestBed.configureTestingModule({ providers: [{ provide: GrimoirePreviewPreferenceService, useValue: pref }] });
    TestBed.overrideComponent(DashboardSwitchComponent, { set: { imports: [], template: '' } });
    const fixture = TestBed.createComponent(DashboardSwitchComponent);
    fixture.detectChanges();

    const e = key();
    window.dispatchEvent(e);
    expect(pref.setNewLook).toHaveBeenCalledWith(true);
    expect(e.defaultPrevented).toBeTrue();

    window.dispatchEvent(key());
    expect(pref.setNewLook).toHaveBeenCalledWith(false);

    window.dispatchEvent(key({ ctrlKey: false }));
    expect(pref.setNewLook).toHaveBeenCalledTimes(2);
  });
});
