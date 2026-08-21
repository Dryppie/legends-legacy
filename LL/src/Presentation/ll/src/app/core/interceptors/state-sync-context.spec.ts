import { HttpHeaders } from '@angular/common/http';
import {
  DOMAIN_VERSIONS_HEADER,
  readDomainVersions,
} from './state-sync-context';

describe('readDomainVersions', () => {
  it('prefers the domain-version header and ignores invalid revisions', () => {
    const headers = new HttpHeaders().set(
      DOMAIN_VERSIONS_HEADER,
      JSON.stringify({ inventory: 7, equipment: -1, quests: '8' }),
    );

    expect(readDomainVersions(headers)).toEqual({ inventory: 7 });
  });
});
