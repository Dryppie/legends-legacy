import { fakeAsync, tick } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { ChatMentionLookupService } from './chat-mention-lookup.service';

describe('ChatMentionLookupService', () => {
  let lookup: ChatMentionLookupService;
  let resolve: jasmine.Spy;

  beforeEach(() => {
    resolve = jasmine.createSpy('resolveCharacterIdByName');
    lookup = new ChatMentionLookupService({
      resolveCharacterIdByName: resolve,
    } as unknown as CharacterService);
  });

  it('uses an exact lookup and shares it across repeated case-insensitive mentions', () => {
    const response = new Subject<string>();
    resolve.and.returnValue(response);
    const ids: (string | null)[] = [];
    lookup.resolve('Ember').subscribe((id) => ids.push(id));
    lookup.resolve('EMBER').subscribe((id) => ids.push(id));
    expect(resolve).toHaveBeenCalledOnceWith('Ember');
    response.next('ember-id');
    response.complete();
    lookup.resolve('ember').subscribe((id) => ids.push(id));
    expect(ids).toEqual(['ember-id', 'ember-id', 'ember-id']);
    expect(resolve).toHaveBeenCalledTimes(1);
  });

  it('leaves nonexistent, failed, empty, and invalid ID lookups unresolved', () => {
    resolve.and.returnValue(throwError(() => new Error('not found')));
    lookup.resolve('blablabla').subscribe((id) => expect(id).toBeNull());
    resolve.and.returnValue(of(''));
    lookup.resolve('missing').subscribe((id) => expect(id).toBeNull());
    resolve.and.returnValue(of('00000000-0000-0000-0000-000000000000'));
    lookup.resolve('invalid').subscribe((id) => expect(id).toBeNull());
    lookup.resolve('').subscribe((id) => expect(id).toBeNull());
    lookup.resolve('x'.repeat(27)).subscribe((id) => expect(id).toBeNull());
    expect(resolve).toHaveBeenCalledTimes(3);
  });

  it('rechecks cached results after expiry so failures and renames can recover', fakeAsync(() => {
    resolve.and.returnValue(throwError(() => new Error('offline')));
    lookup.resolve('Ember').subscribe((id) => expect(id).toBeNull());
    tick(60_001);
    resolve.and.returnValue(of('ember-id'));
    lookup.resolve('Ember').subscribe((id) => expect(id).toBe('ember-id'));
    expect(resolve).toHaveBeenCalledTimes(2);
  }));
});
