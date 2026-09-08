import { fakeAsync, tick } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { ChatMentionSuggestionsService } from './chat-mention-suggestions.service';

describe('ChatMentionSuggestionsService', () => {
  let service: ChatMentionSuggestionsService;
  let search: jasmine.Spy;

  beforeEach(() => {
    search = jasmine
      .createSpy('suggestCharacterNames')
      .and.returnValue(of(['Ember']));
    service = new ChatMentionSuggestionsService({
      suggestCharacterNames: search,
    } as unknown as CharacterService);
  });
  afterEach(() => service.ngOnDestroy());

  it('uses the existing search after two characters and a short debounce', fakeAsync(() => {
    service.update({ start: 0, end: 2, query: 'E' });
    tick(200);
    expect(search).not.toHaveBeenCalled();
    service.update({ start: 0, end: 3, query: 'Em' });
    tick(199);
    expect(search).not.toHaveBeenCalled();
    tick(1);
    expect(search).toHaveBeenCalledOnceWith('Em');
    expect(service.names()).toEqual(['Ember']);
  }));

  it('cancels old searches immediately when the query changes or closes', fakeAsync(() => {
    const oldSearch = new Subject<string[]>();
    search.and.returnValue(oldSearch);
    service.update({ start: 0, end: 3, query: 'Em' });
    tick(200);
    service.update({ start: 0, end: 4, query: 'Emb' });
    oldSearch.next(['Old result']);
    expect(service.names()).toEqual([]);
    search.and.returnValue(of(['Ember']));
    tick(200);
    expect(service.names()).toEqual(['Ember']);
    service.close();
    oldSearch.next(['Late result']);
    expect(service.names()).toEqual([]);
    expect(service.active()).toBeNull();
    service.update({ start: 0, end: 4, query: 'Emb' });
    tick(200);
    expect(service.names()).toEqual(['Ember']);
  }));

  it('recovers from search errors and cancels pending work on destruction', fakeAsync(() => {
    search.and.returnValue(throwError(() => new Error('offline')));
    service.update({ start: 0, end: 3, query: 'Em' });
    tick(200);
    expect(service.error()).toBeTrue();
    expect(service.loading()).toBeFalse();
    search.and.returnValue(of(['Ash']));
    service.update({ start: 0, end: 3, query: 'As' });
    tick(200);
    expect(service.error()).toBeFalse();
    expect(service.names()).toEqual(['Ash']);
    service.update({ start: 0, end: 4, query: 'Ash' });
    service.ngOnDestroy();
    tick(200);
    expect(search).toHaveBeenCalledTimes(2);
  }));
});
