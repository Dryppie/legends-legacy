import { HttpContextToken, HttpHeaders } from '@angular/common/http';
import {
  isStateSyncScope,
  StateSyncScope,
  StateVersionMap,
} from '../services/real-time/game-realtime/game-realtime-contracts';

export const DOMAIN_VERSIONS_HEADER = 'X-LL-Domain-Versions';

export const FORCE_STATE_SYNC_RESPONSE_REFRESH = new HttpContextToken<boolean>(
  () => true,
);

export const STATE_SYNC_SCOPES_HANDLED_BY_RESPONSE = new HttpContextToken<
  readonly StateSyncScope[]
>(() => []);

export function readDomainVersions(
  headers: HttpHeaders,
): StateVersionMap {
  const encoded = headers.get(DOMAIN_VERSIONS_HEADER);
  if (!encoded) return {};

  try {
    const parsed = JSON.parse(encoded) as Record<string, unknown>;
    return Object.fromEntries(
      Object.entries(parsed).filter(
        (entry): entry is [StateSyncScope, number] =>
          isStateSyncScope(entry[0]) &&
          Number.isSafeInteger(entry[1]) &&
          (entry[1] as number) > 0,
      ),
    ) as StateVersionMap;
  } catch {
    return {};
  }
}
