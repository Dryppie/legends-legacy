import { HttpContextToken } from '@angular/common/http';

export const FORCE_STATE_SYNC_RESPONSE_REFRESH = new HttpContextToken<boolean>(
  () => true,
);

export const STATE_SYNC_SCOPES_HANDLED_BY_RESPONSE = new HttpContextToken<
  readonly string[]
>(() => []);
