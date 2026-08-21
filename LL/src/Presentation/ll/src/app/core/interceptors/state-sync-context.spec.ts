import { HttpHeaders } from '@angular/common/http';
import {
  DOMAIN_VERSIONS_HEADER,
  readDomainVersions,
  STATE_REVISIONS_HEADER,
} from './state-sync-context';

describe('readDomainVersions', () => {
  it('prefers the domain-version header and ignores invalid revisions', () => {
    const headers = new HttpHeaders()
      .set(
        DOMAIN_VERSIONS_HEADER,
        JSON.stringify({ inventory: 7, equipment: -1, quests: '8' }),
      )
      .set(STATE_REVISIONS_HEADER, JSON.stringify({ inventory: 2 }));

    expect(readDomainVersions(headers)).toEqual({ inventory: 7 });
  });

  it('falls back to the compatibility state-revision header', () => {
    const headers = new HttpHeaders().set(
      STATE_REVISIONS_HEADER,
      JSON.stringify({ equipment: 3 }),
    );

    expect(readDomainVersions(headers)).toEqual({ equipment: 3 });
  });
});
