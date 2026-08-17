import { HttpContextToken } from '@angular/common/http';

export const FORCE_STATE_SYNC_RESPONSE_REFRESH = new HttpContextToken<boolean>(
  () => true,
);
