import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';

import { catchError } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  constructor(private http: HttpClient) {}

  deafultHeaders = new HttpHeaders({
    'Content-Type': 'application/json',
    Accept: 'application/json',
  });

  private formatErrors(error: any) {
    return throwError(() => new Error(error.error));
  }

  get(path: string, params: HttpParams = new HttpParams()): Observable<any> {
    return this.http
      .get(`${environment.apiUrl}${path}`, {
        params,
        withCredentials: true,
        headers: this.deafultHeaders,
      })
      .pipe(catchError(this.formatErrors));
  }

  put(path: string, body: Object = {}): Observable<any> {
    return this.http
      .put(`${environment.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.deafultHeaders,
      })
      .pipe(catchError(this.formatErrors));
  }

  patch(path: string, body: Object = {}): Observable<any> {
    return this.http
      .patch(`${environment.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.deafultHeaders,
      })
      .pipe(catchError(this.formatErrors));
  }

  post(path: string, body: Object = {}): Observable<any> {
    return this.http
      .post(`${environment.apiUrl}${path}`, JSON.stringify(body), {
        withCredentials: true,
        headers: this.deafultHeaders,
      })
      .pipe(catchError(this.formatErrors));
  }

  delete(path: string, body: Object = {}): Observable<any> {
    return this.http
      .delete(`${environment.apiUrl}${path}`, {
        withCredentials: true,
        headers: this.deafultHeaders,
        body: JSON.stringify(body),
      })
      .pipe(catchError(this.formatErrors));
  }
}
