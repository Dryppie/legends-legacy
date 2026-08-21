import { Injectable } from '@angular/core';
import {
  HttpClient,
  HttpContext,
  HttpErrorResponse,
  HttpHeaders,
  HttpParams,
} from '@angular/common/http';
import { map, Observable, throwError } from 'rxjs';

import { catchError } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import {
  FORCE_STATE_SYNC_RESPONSE_REFRESH,
  readDomainVersions,
  STATE_SYNC_SCOPES_HANDLED_BY_RESPONSE,
} from '../../interceptors/state-sync-context';

export interface ApiMutationOptions {
  forceStateSyncRefresh?: boolean;
  /** Scopes patched from the response or known to be unchanged by this mutation. */
  stateSyncScopesHandledByResponse?: readonly string[];
}

export interface VersionedMutationResult<T> {
  data: T;
  domainVersions: Readonly<Record<string, number>>;
}

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  constructor(private http: HttpClient) {}

  public apiUrl = `${environment.apiBaseUrl}/api/v1/`;

  defaultHeaders = new HttpHeaders({
    'Content-Type': 'application/json',
    Accept: 'application/json',
  });

  private formatErrors = (error: HttpErrorResponse) => {
    (error as HttpErrorResponse & { errorMessage: string }).errorMessage =
      this.getErrorMessage(error);
    return throwError(() => error);
  };

  private getErrorMessage(error: HttpErrorResponse): string {
    if (typeof error.error === 'string' && error.error.trim()) {
      return error.error;
    }

    if (error.error?.errorMessage) {
      return error.error.errorMessage;
    }

    if (Array.isArray(error.error?.errors) && error.error.errors.length) {
      return error.error.errors.join('\n');
    }

    if (typeof error.error?.detail === 'string' && error.error.detail.trim()) {
      return error.error.detail;
    }

    return error.message || 'Request failed';
  }

  get(path: string, params: HttpParams = new HttpParams()): Observable<any> {
    return this.http
      .get(`${this.apiUrl}${path}`, {
        params,
        withCredentials: true,
        headers: this.defaultHeaders,
      })
      .pipe(catchError(this.formatErrors));
  }

  put(
    path: string,
    body: Object = {},
    options: ApiMutationOptions = {},
  ): Observable<any> {
    return this.http
      .put(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.defaultHeaders,
        context: this.getMutationContext(options),
      })
      .pipe(catchError(this.formatErrors));
  }

  putVersioned<T>(
    path: string,
    body: Object = {},
    options: ApiMutationOptions = {},
  ): Observable<VersionedMutationResult<T>> {
    return this.http
      .put<T>(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.defaultHeaders,
        context: this.getMutationContext(options),
        observe: 'response',
      })
      .pipe(
        map((response) =>
          this.toVersionedResult(response.body as T, response.headers),
        ),
        catchError(this.formatErrors),
      );
  }

  patch(
    path: string,
    body: Object = {},
    options: ApiMutationOptions = {},
  ): Observable<any> {
    return this.http
      .patch(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.defaultHeaders,
        context: this.getMutationContext(options),
      })
      .pipe(catchError(this.formatErrors));
  }

  patchVersioned<T>(
    path: string,
    body: Object = {},
    options: ApiMutationOptions = {},
  ): Observable<VersionedMutationResult<T>> {
    return this.http
      .patch<T>(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.defaultHeaders,
        context: this.getMutationContext(options),
        observe: 'response',
      })
      .pipe(
        map((response) =>
          this.toVersionedResult(response.body as T, response.headers),
        ),
        catchError(this.formatErrors),
      );
  }

  post(
    path: string,
    body: Object = {},
    options: ApiMutationOptions = {},
  ): Observable<any> {
    return this.http
      .post(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.getHeaders(path),
        context: this.getMutationContext(options),
      })
      .pipe(catchError(this.formatErrors));
  }

  postVersioned<T>(
    path: string,
    body: Object = {},
    options: ApiMutationOptions = {},
  ): Observable<VersionedMutationResult<T>> {
    return this.http
      .post<T>(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.getHeaders(path),
        context: this.getMutationContext(options),
        observe: 'response',
      })
      .pipe(
        map((response) =>
          this.toVersionedResult(response.body as T, response.headers),
        ),
        catchError(this.formatErrors),
      );
  }

  delete(
    path: string,
    body: Object = {},
    options: ApiMutationOptions = {},
  ): Observable<any> {
    return this.http
      .delete(`${this.apiUrl}${path}`, {
        withCredentials: true,
        headers: this.defaultHeaders,
        body: JSON.stringify(body),
        context: this.getMutationContext(options),
      })
      .pipe(catchError(this.formatErrors));
  }

  deleteVersioned<T>(
    path: string,
    body: Object = {},
    options: ApiMutationOptions = {},
  ): Observable<VersionedMutationResult<T>> {
    return this.http
      .delete<T>(`${this.apiUrl}${path}`, {
        withCredentials: true,
        headers: this.defaultHeaders,
        body: JSON.stringify(body),
        context: this.getMutationContext(options),
        observe: 'response',
      })
      .pipe(
        map((response) =>
          this.toVersionedResult(response.body as T, response.headers),
        ),
        catchError(this.formatErrors),
      );
  }

  private toVersionedResult<T>(
    data: T,
    headers: HttpHeaders,
  ): VersionedMutationResult<T> {
    return {
      data,
      domainVersions: readDomainVersions(headers),
    };
  }

  private getMutationContext(options: ApiMutationOptions): HttpContext {
    return new HttpContext()
      .set(
        FORCE_STATE_SYNC_RESPONSE_REFRESH,
        options.forceStateSyncRefresh ?? true,
      )
      .set(
        STATE_SYNC_SCOPES_HANDLED_BY_RESPONSE,
        options.stateSyncScopesHandledByResponse ?? [],
      );
  }

  private getHeaders(path: string): HttpHeaders {
    const normalizedPath = path.toLowerCase();

    if (
      normalizedPath === 'auth/createnewtokens' ||
      normalizedPath === 'auth/logout'
    ) {
      return this.defaultHeaders.set('X-LL-Refresh-Request', '1');
    }

    return this.defaultHeaders;
  }
}
