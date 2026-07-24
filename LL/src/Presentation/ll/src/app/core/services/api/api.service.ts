import { Injectable } from '@angular/core';
import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
  HttpParams,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';

import { catchError } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

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

  put(path: string, body: Object = {}): Observable<any> {
    return this.http
      .put(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.defaultHeaders,
      })
      .pipe(catchError(this.formatErrors));
  }

  patch(path: string, body: Object = {}): Observable<any> {
    return this.http
      .patch(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.defaultHeaders,
      })
      .pipe(catchError(this.formatErrors));
  }

  post(path: string, body: Object = {}): Observable<any> {
    return this.http
      .post(`${this.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.getHeaders(path),
      })
      .pipe(catchError(this.formatErrors));
  }

  delete(path: string, body: Object = {}): Observable<any> {
    return this.http
      .delete(`${this.apiUrl}${path}`, {
        withCredentials: true,
        headers: this.defaultHeaders,
        body: JSON.stringify(body),
      })
      .pipe(catchError(this.formatErrors));
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
