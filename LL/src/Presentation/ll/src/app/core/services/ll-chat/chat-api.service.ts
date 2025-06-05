import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ChatApiService {
  constructor(private http: HttpClient) {}

  public apiUrl = `${environment.chatApiRoot}/api/v1/`;

  deafultHeaders = new HttpHeaders({
    'Content-Type': 'application/json',
    Accept: 'application/json',
  });

  private formatErrors(error: any) {
    return throwError(() => new Error(error.error));
  }

  get(path: string, params: HttpParams = new HttpParams()): Observable<any> {
    return this.http
      .get(`${this.apiUrl}${path}`, {
        params,
        withCredentials: true,
        headers: this.deafultHeaders,
      })
      .pipe(catchError(this.formatErrors));
  }
}
